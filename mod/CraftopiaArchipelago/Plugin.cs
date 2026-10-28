using System;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.craftopia.archipelago";
        public const string NAME = "CraftopiaArchipelago";
        public const string VERSION = "1.18.2";

        internal static ManualLogSource Log;
        internal static Bridge Bridge;
        internal static Validator Validator;
        internal static PermissionManager Permissions;
        private static UnlockMirror unlockMirror;
        internal static PlayerStatusDisplay StatusDisplay;
        internal static ConfigEntry<string> SlotName;
        internal static ConfigEntry<int> LocalPort;
        private ConfigEntry<float> hudX, hudY;
        private ConfigFile userConfig;
        private readonly HudDragState hudDrag = new HudDragState();
        private static EnrollmentCoordinator enrollmentCoordinator;
        private static string enrollmentStatus;

        /// <summary>
        /// Skill info: category and tier for a skill ID.
        /// Populated at runtime from OcSkillManager.skillList.all.
        /// </summary>
        internal class SkillInfo
        {
            public string Category;
            public int Tier;
        }
        private static System.Collections.Generic.Dictionary<int, SkillInfo> skillInfoCache =
            new System.Collections.Generic.Dictionary<int, SkillInfo>();

        /// <summary>
        /// Check if a session is bound (AP connection active with valid slot/epoch).
        /// When not bound, enforcement hooks fail-open to vanilla behavior.
        /// </summary>
        // Persistent session binding, separate from socket connectivity.
        // Set when a validated session is established; cleared only on
        // explicit session change or recovery hold. Survives disconnects.
        private static EquipmentSession equipmentSession;
        internal static string EquipmentCatalogIdentity => equipmentSession?.Catalog;
        internal static bool EquipmentRequired => equipmentSession != null && equipmentSession.Required;
        internal static PermissionManager EquipmentPermissions()
        {
            try {
                if (!EquipmentRequired || Validator == null || eligibility == null) return null;
                var s = Validator.Capture();
                bool valid = s.Ready && s.Input.Identity == IdentityStatus.Verified &&
                    Validator.LastOutcome.Outcome == ValidationOutcome.Pass && !Validator.IsHeld(s.Input.Guid,s.Input.Epoch) &&
                    eligibility.IsEligible(s.Player,s.Input.Guid,s.Input.Epoch,s.LoadEvent);
                return equipmentSession.Ready(s.Input.Guid,s.Input.Epoch,SlotName.Value,valid) ? Permissions : null;
            } catch { return null; }
        }
        private static bool sessionBound = false;
        private static long boundEpoch = 0;
        private static string boundSlot = null;

        internal static bool IsSessionBound()
        {
            // Session binding persists across disconnects and recovery holds.
            // A recovery hold blocks grants/acks (checked separately via
            // recoveryHold) but must NEVER turn a bound player into vanilla
            // mode for enforcement. Enforcement uses existing permissions
            // while bound; grants remain held until recovery completes.
            // Only cleared on explicit session change.
            return sessionBound;
        }

        /// <summary>
        /// Check if grants/acks may proceed. Requires a bound session AND
        /// no active recovery hold. Enforcement hooks use IsSessionBound;
        /// delivery paths use this.
        /// </summary>
        internal static bool IsDeliveryEligible()
        {
            return sessionBound && !recoveryHold;
        }

        internal static void SetSessionBound(long epoch, string slot)
        {
            sessionBound = true;
            boundEpoch = epoch;
            boundSlot = slot;
            Log.LogInfo($"[AP] Session bound: epoch={epoch}, slot={slot}");
        }

        internal static void ClearSessionBound(string reason)
        {
            if (sessionBound)
            {
                Log.LogInfo($"[AP] Session unbound: {reason}");
                sessionBound = false;
                boundEpoch = 0;
                boundSlot = null;
            }
        }

        /// <summary>
        /// Resolve a building type name to its production group.
        /// Returns "opening" for free stations, group ID for gated stations,
        /// or null if unknown (bound production attempts are denied).
        /// Concrete station types are matched exactly.
        /// </summary>
        internal static string ResolveProductionGroup(string typeName)
            => ProductionEnforcement.ResolveStation(typeName);

        /// <summary>
        /// Resolve a skill ID to its category and tier.
        /// Returns null if unknown (bound learning attempts are denied).
        /// </summary>
        private static bool skillCatalogWarningLogged = false;

        /// <summary>
        /// Get the current character's GUID, or 0 if no character is loaded.
        /// </summary>
        internal static int GetCharacterGuid()
        {
            try
            {
                // The player instance GUID is captured during load validation.
                // See OnLoadStart which receives instance.GUID.
                return lastPlayerGuid;
            }
            catch
            {
                return 0;
            }
        }
        private static int lastPlayerGuid = 0;

        internal static void SetLastPlayerGuid(int guid)
        {
            lastPlayerGuid = guid;
        }

        internal static SkillInfo ResolveSkillInfo(int skillId)
        {
            SkillInfo cached;
            if (skillInfoCache.TryGetValue(skillId, out cached))
                return cached;
            // Skill catalog not yet populated from runtime.
            // Enforcement is UNAVAILABLE until the diagnostic collector provides
            // the ID -> (category, tier) mapping. Fail-open with a one-time warning.
            if (!skillCatalogWarningLogged)
            {
                skillCatalogWarningLogged = true;
                // Note: while session-bound, unknown skill IDs are DENIED by
                // SkillEnforcement (fail-closed). This warning is for diagnostics.
                Log.LogWarning("[AP] Skill ID not in catalog 20261008_r1. " +
                    "While session-bound, unknown IDs are blocked. " +
                    "If this is a valid skill, the catalog may need updating.");
            }
            return null;
        }

        /// <summary>
        /// Populate the skill catalog from runtime data.
        /// Called by the diagnostic collector or on first skill manager access.
        /// </summary>
        internal static void PopulateSkillCatalog(System.Collections.Generic.Dictionary<int, SkillInfo> catalog)
        {
            skillInfoCache = catalog ?? new System.Collections.Generic.Dictionary<int, SkillInfo>();
            Log.LogInfo($"[AP] Skill catalog populated: {skillInfoCache.Count} skills");
        }

        /// <summary>
        /// Load the versioned skill catalog from the embedded JSON resource.
        /// Built from the 2026-10-08 runtime capture (241 player skill candidates).
        /// Called during plugin initialization; fails safe (empty catalog) on error.
        /// </summary>
        internal static void LoadSkillCatalogFromEmbedded()
        {
            const string resourceName = "skill_catalog_20261008_r1.json";
            const string expectedCatalogId = "20261008_r1";
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Log.LogWarning("[AP] Skill catalog resource not found: " + resourceName);
                        return;
                    }
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        // Use the tested parser from SkillEnforcement.
                        var parsed = SkillEnforcement.ParseSkillCatalog(
                            json, expectedCatalogId, m => Log.LogError(m));
                        if (parsed == null)
                        {
                            Log.LogError("[AP] Skill catalog: parse failed");
                            return;
                        }
                        var catalog = new System.Collections.Generic.Dictionary<int, SkillInfo>();
                        foreach (var kv in parsed)
                            catalog[kv.Key] = new SkillInfo
                            {
                                Category = kv.Value.Category,
                                Tier = kv.Value.Tier
                            };
                        skillInfoCache = catalog;
                        Log.LogInfo($"[AP] Skill catalog loaded: {catalog.Count} skills " +
                            $"(catalog {expectedCatalogId})");
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.LogError("[AP] Skill catalog load failed: " + e.GetType().Name);
            }
        }

        private static EntitlementStore entitlementStore;
        private static EnrollmentState enrollmentStateField;
        internal static EnrollmentState EnrollmentState => enrollmentStateField;
        private static string EntitlementStorePath;
        private static GrantTransaction grantTransaction;
        private static bool recoveryHold = false;
        private static Eligibility eligibility;
        private static long loadEventCounter = 0;
        private static bool loadCompletedFlag = false; // Set by OnLoadEnd(true).

        /// <summary>
        /// Item-grant state machine (pure logic, unit-tested). Owns dedup,
        /// retry-with-backoff, epoch rotation, and the rule that nothing is
        /// acked until it was applied AND durably persisted.
        /// </summary>
        internal static GrantPipeline Pipeline = new GrantPipeline();

        /// <summary>
        /// Skill-point grant applicator with player-readiness gating. The
        /// gateway re-evaluates OcPlMng.LocalPl on every call and never caches
        /// the player, so a stale player object can never be granted to.
        /// </summary>
        private static readonly PlayerGrantApplicator grantApplicator =
            new PlayerGrantApplicator(new OcPlayerSkillGateway(),
                m => Log.LogInfo(m), m => Log.LogError(m));

        private static string GrantStatePath;

        // Lifecycle ownership: the bridge thread and Harmony patches must
        // exist exactly once. If a duplicate plugin component ever appears,
        // only the first owns the bridge; the duplicate destroys itself
        // without touching it.
        private static Plugin instance;
        private static bool harmonyApplied;

        private void Awake()
        {
            Log = Logger;
            string now = System.DateTime.UtcNow.ToString("o");
            int id = GetInstanceID();
            string detail = DescribeGameObject();
            bool onManager = false;
            try { onManager = ReferenceEquals(gameObject, BepInEx.Bootstrap.Chainloader.ManagerObject); }
            catch (Exception e) { detail += " managerCheckFailed=" + e.GetType().Name; }
            Log.LogInfo("[AP] Awake t=" + now + " instance=" + id + " " + detail
                + " onManagerObject=" + onManager);

            if (instance != null && !ReferenceEquals(instance, this))
            {
                Log.LogWarning("[AP] duplicate plugin instance " + id
                    + " detected; destroying the duplicate without touching the bridge");
                Destroy(this);
                return;
            }
            instance = this;
            try
            {
                string pluginDirectory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                UserStorage.Migrate(UserStorage.Root, UserStorage.LegacyDirectories(BepInEx.Paths.ConfigPath, pluginDirectory, BepInEx.Paths.PluginPath));
                userConfig = new ConfigFile(System.IO.Path.Combine(UserStorage.Root, "com.craftopia.archipelago.cfg"), true);
            }
            catch (Exception e)
            {
                Log.LogError("[AP] User-data migration stopped startup to protect progress: " + e.Message);
                enabled = false;
                return;
            }

            // Load the versioned skill catalog from embedded resource.
            // This enables skill-tree enforcement with the 2026-10-08 runtime IDs.
            LoadSkillCatalogFromEmbedded();

            hudX = userConfig.Bind("Display", "HudX", 220f, "Field Guide badge horizontal position in scaled UI pixels; drag with the cursor visible.");
            hudY = userConfig.Bind("Display", "HudY", 20f, "Field Guide badge vertical position in scaled UI pixels.");
            SlotName = userConfig.Bind("General", "SlotName", "Player1",
                "Archipelago slot name. Must match the name in your YAML.");
            LocalPort = userConfig.Bind("General", "LocalPort", 11341,
                "Port the Python bridge client listens on (localhost).");

            managedMode = userConfig.Bind("Connection", "Managed", true, "Use the in-game Connection page and managed local bridge.").Value;
            InitializeSession(null);
            Connection = new ConnectionController(userConfig, System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            if (!harmonyApplied)
            {
                if (!InstallAndVerifyHooks())
                {
                    // Verification failed: the mission hook is not registered.
                    // Stop initialization here — do not start the bridge, do
                    // not mark Harmony as initialized. The error above says why.
                    return;
                }
                harmonyApplied = true;
            }

            if (!managedMode && Bridge == null)
            {
                Bridge = new Bridge();
                // Initialize durable observation outbox.
                // Path is in the BepInEx config folder.
                try
                {
                    string outboxPath = System.IO.Path.Combine(
                        UserStorage.Root,
                        "craftopia_observations.log");
                    Bridge.InitDurableOutbox(outboxPath);
                }
                catch (Exception e)
                {
                    Log.LogError("[AP] outbox init failed: " + e.GetType().Name);
                }
                Bridge.Start();
            }

            try
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            }
            catch (Exception e)
            {
                Log.LogWarning("[AP] could not subscribe to scene events: " + e.Message);
            }

            Log.LogInfo("[AP] CraftopiaArchipelago PoC " + VERSION + " loaded");
        }

        private static string sessionFileKey, enrollmentStatePath, sessionSeed;
        private static float nextCharacterRegistration;
        private static bool managedMode;
        internal static ConnectionController Connection;
        internal static string ManagedProfileKey { get; private set; }
        internal static bool ManagedSessionInitialized => ManagedProfileKey != null;
        private void InitializeSession(string profileKey)
        {
            fieldGuide.SetOpen(false); orbMap.Dispose(); OrbMapTracker.State.Clear();
            enrollmentStatus = null; sessionSeed = null; nextCharacterRegistration = 0;
            bool managedProfile = profileKey != null;
            sessionFileKey = managedProfile ? "profile-" + profileKey : SanitizeSlot(SlotName.Value);
            // The applied-grant file is bound to the slot identity: a different
            // slot name gets a different file, so applied indices from one slot
            // can never suppress or duplicate another slot's grants.
            GrantStatePath = System.IO.Path.Combine(UserStorage.Root,
                "craftopia-archipelago-grants-" + sessionFileKey + ".json");
            LoadGrantState();

            // Entitlement store: durable AP skill-point entitlement per (guid, epoch).
            EntitlementStorePath = System.IO.Path.Combine(UserStorage.Root,
                "craftopia-archipelago-entitlement-" + sessionFileKey + ".json");
            entitlementStore = new EntitlementStore(EntitlementStorePath,
                m => Log.LogInfo(m), m => Log.LogError(m));
            // Atomic enrollment state: single-file durable enrollment authority.
            // Replaces the two-file attestation+entitlement design.
            string enrollmentPath = System.IO.Path.Combine(UserStorage.Root,
                "craftopia-archipelago-enrollment-" + sessionFileKey + ".json");
            var enrollmentState = new EnrollmentState(enrollmentPath,
                m => Log.LogInfo(m), m => Log.LogError(m));
            enrollmentState.Load();
            // Store for enrollment coordinator access.
            enrollmentStateField = enrollmentState;
            entitlementStore.Load();
            // Validation needs slot name and enrollment state.

            // Grant transaction: durable intent → mutation → atomic commit → ack.
            string intentPath = System.IO.Path.Combine(UserStorage.Root,
                "craftopia-archipelago-intent-" + sessionFileKey + ".json");
            grantTransaction = new GrantTransaction(
                intentPath,
                entitlementStore,
                indices => SaveGrantStateFor(indices), // persist applied
                m => Log.LogInfo(m), m => Log.LogError(m),
                enrollmentStateField, SlotName.Value);

            // Eligibility: initially blocked; invalidated on load/session.
            eligibility = new Eligibility(m => Log.LogInfo(m), m => Log.LogWarning(m));

            // Permission manager for equipment/production/skill unlocks.
            Permissions = new PermissionManager(m => Log.LogInfo(m), m => Log.LogWarning(m));

            unlockMirror = new UnlockMirror(Permissions);
            equipmentSession = managedProfile ? EquipmentSession.Managed(SlotName.Value, enrollmentPath + ".equipment.json") : new EquipmentSession(
                userConfig.Bind("EquipmentTest", "Enabled", false, "Explicit experimental equipment test approval; existing seeds leave false.").Value,
                userConfig.Bind("EquipmentTest", "CharacterGuid", 0, "Approved test character GUID.").Value,
                userConfig.Bind("EquipmentTest", "Epoch", 0L, "Approved test epoch.").Value, SlotName.Value);

            // Player status display.
            StatusDisplay = new PlayerStatusDisplay(Permissions, m => Log.LogInfo(m));

            // Check for incomplete intent at startup (recovery hold).
            // Block first, handle null intent (corrupt) without dereference.
            GrantIntent incomplete;
            if (grantTransaction.CheckForIncompleteIntent(out incomplete))
            {
                recoveryHold = true;
                if (eligibility != null)
                    eligibility.Block("incomplete intent at startup");
                string idxStr = "unknown";
                try
                {
                    if (incomplete != null && incomplete.Indices != null)
                        idxStr = string.Join(",", incomplete.Indices.ConvertAll(
                            x => x.ToString()).ToArray());
                }
                catch { }
                Log.LogError("[AP] RECOVERY HOLD: incomplete grant intent found. " +
                    "Do NOT replay. Requires explicit reviewed recovery. " +
                    "Indices: [" + idxStr + "]");
            }

            // Validator: gates GameStartCheckSkillPoint via ValidationDecision.
            Validator = new Validator(entitlementStore,
                m => Log.LogInfo(m), m => Log.LogError(m), new RecoveryHoldStore(EntitlementStorePath + ".holds.json"));
            Validator.SlotName = SlotName.Value;
            Validator.EnrollmentState = enrollmentState;
            Validator.LoadEvent = () => loadEventCounter;
            Validator.LoadCompleted = () => loadCompletedFlag;
            // Epoch and enrollment are set from bridge slot_data on connect.
            // Defaults: epoch 0 (unknown), no enrollment -> defer.
            Validator.CurrentEpoch = 0;
            Validator.IsEnrolled = guid => false;
            // Wire validation -> eligibility.
            Validator.OnValidationCallback = (outcome, playerRef, guid, epoch, loadEvent) =>
            {
                try
                {
                    if (eligibility != null)
                    {
                        // Use the captured event, never substitute a newer global event.
                        // Stale callbacks must remain bound to their original invocation.
                        eligibility.OnValidation(outcome, playerRef, guid, epoch, loadEvent);
                    }
                }
                catch (Exception e)
                {
                    Log.LogError("[AP] eligibility OnValidation failed: " + e.GetType().Name);
                }
            };
            Validator.HasHistory = () => Pipeline.Snapshot().Applied.Count > 0 || Pipeline.Snapshot().Quarantined.Count > 0;
            // Deliberately opt-in, exact character/session only. These values are captured
            // at startup; editing the config during play never silently enrolls anyone.
            bool approveFresh = userConfig.Bind("Enrollment", "ApproveFreshCharacter", false,
                "Explicitly attest that the selected GUID is a NEW disposable level-1 character with zero points and no AP history. One-time approval; restart required.").Value;
            int approvedGuid = userConfig.Bind("Enrollment", "CharacterGuid", 0,
                "Exact signed GUID observed for the new character. Zero disables enrollment.").Value;
            long approvedEpoch = userConfig.Bind("Enrollment", "Epoch", 0L,
                "Exact epoch of the new test seed. Zero disables enrollment.").Value;
            enrollmentCoordinator = new EnrollmentCoordinator(enrollmentState,
                enrollmentPath + ".approval-used", approvedGuid, approvedEpoch, SlotName.Value, approveFresh);
            enrollmentStatePath = enrollmentPath;
            Validator.AwaitingEnrollment = s => managedMode ? ManagedAwaitingEnrollment(s) : enrollmentCoordinator.CanWait(s,
                EnrollmentHasHistory(), recoveryHold || Validator.IsHeld(s.Input.Guid,s.Input.Epoch));

        }

        internal static bool CanChangeConnection
        {
            get { try { return Oc.OcPlMng.LocalPl == null && !MilestoneSession.HasUnpersistedObservations; } catch { return false; } }
        }
        internal static bool PrepareManagedSession(string slot, string profileKey, int localPort)
        {
            if (instance == null || !harmonyApplied || !managedMode || string.IsNullOrWhiteSpace(slot) ||
                !ConnectionController.IsValidProfileKey(profileKey) || localPort < 1 || localPort > 65535) return false;
            bool same = ManagedProfileKey == profileKey && SlotName.Value == slot;
            if (!same && !CanChangeConnection) return false;
            Bridge?.Stop();
            Bridge = null;
            CancelEnrollment();
            if (!same) {
                CurrencyRuntime.ResetSession(); StarterKitRuntime.ResetSession();
                MilestoneSession.Bind(null);
                lock (pendingSessions) pendingSessions.Clear();
                lock (pendingUnlocks) pendingUnlocks.Clear();
                Pipeline = new GrantPipeline();
                ClearSessionBound("explicit main-menu profile selection");
                recoveryHold = false; loadCompletedFlag = false; lastPlayerGuid = 0;
                SlotName.Value = slot;
                instance.InitializeSession(profileKey);
                ManagedProfileKey = profileKey;
            }
            LocalPort.Value = localPort;
            Bridge = new Bridge(Pipeline.Epoch, true);
            Bridge.InitDurableOutbox(System.IO.Path.Combine(UserStorage.Root,
                "craftopia-observations-" + sessionFileKey + ".log"));
            Bridge.Start();
            return true;
        }

        private void OnDestroy()
        {
            string now = System.DateTime.UtcNow.ToString("o");
            int id;
            try { id = GetInstanceID(); }
            catch { id = -1; }
            bool owner = ReferenceEquals(instance, this);
            try
            {
                Log.LogInfo("[AP] OnDestroy t=" + now + " instance=" + id + " "
                    + DescribeGameObject() + " owner=" + owner);
                Log.LogInfo("[AP] OnDestroy stack: " + System.Environment.StackTrace);
            }
            catch { /* logging must never break teardown */ }

            if (!owner) return; // duplicate instance; the owner keeps the bridge
            Connection?.Dispose();
            fieldGuide.Dispose();
            orbMap.Dispose();
            OrbMapTracker.State.Clear();
            CurrencyRuntime.Suspend(); StarterKitRuntime.Suspend();
            instance = null;
            try
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
            catch { }
            if (Bridge != null)
            {
                Bridge.Stop();
                Bridge = null;
            }
        }

        private void OnApplicationQuit()
        {
            try
            {
                Log.LogInfo("[AP] OnApplicationQuit t=" + System.DateTime.UtcNow.ToString("o")
                    + " instance=" + GetInstanceID() + " " + DescribeGameObject());
            }
            catch { }
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            try
            {
                Log.LogInfo("[AP] scene loaded: '" + scene.name + "' mode=" + mode
                    + " t=" + System.DateTime.UtcNow.ToString("o"));
            }
            catch { }
        }

        private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            try
            {
                Log.LogInfo("[AP] scene unloaded: '" + scene.name
                    + "' t=" + System.DateTime.UtcNow.ToString("o"));
            }
            catch { }
        }

        /// <summary>
        /// Installs Harmony patches, then verifies via Harmony's own patch
        /// metadata that all three hooks are registered under our owner ID:
        /// Mission.TryGetReward postfix, OcSkillManager.GameStartCheckSkillPoint
        /// prefix, and OcPlMaster.LoadSaveData prefix/postfix/finalizer.
        /// Returns true only when all verify.
        /// </summary>
        private static bool InstallAndVerifyHooks()
        {
            Harmony harmony = null;
            try
            {
                harmony = new Harmony(GUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                ProductionHooks.Install(harmony);
                EquipmentHooks.Install(harmony);
                CurrencySaveObserver.Install(harmony);
                StarterKitSaveObserver.Install(harmony);
                SoulOrbHooks.Install(harmony);
                MilestoneHooks.Install(harmony);
            }
            catch (Exception e)
            {
                try { harmony?.UnpatchSelf(); } catch { }
                Log.LogError("[AP] FATAL: Harmony PatchAll threw: " + e.GetType().Name
                    + ": " + e.Message + ". Hooks not registered; bridge not started.");
                return false;
            }

            bool ok = true;
            ok &= VerifyHook(harmony, typeof(Oc.Missions.Mission),
                nameof(Oc.Missions.Mission.TryGetReward),
                "MissionRewardPostfix", "postfix");
            ok &= VerifyHook(harmony, typeof(Oc.Skills.OcSkillManager),
                nameof(Oc.Skills.OcSkillManager.GameStartCheckSkillPoint),
                "ValidatorPrefix", "prefix");
            ok &= VerifyHook(harmony, typeof(Oc.Skills.OcSkillManager),
                nameof(Oc.Skills.OcSkillManager.GameStartCheckSkillPoint), "ValidatorPostfix", "postfix");
            ok &= VerifyHook(harmony, typeof(Oc.Skills.OcSkillManager),
                nameof(Oc.Skills.OcSkillManager.GameStartCheckSkillPoint), "ValidatorFinalizer", "finalizer");
            ok &= VerifyHook(harmony, typeof(Oc.OcPlMaster),
                nameof(Oc.OcPlMaster.LoadSaveData),
                "LoadPrefix", "prefix");
            ok &= VerifyHook(harmony, typeof(Oc.OcPlMaster),
                nameof(Oc.OcPlMaster.LoadSaveData),
                "LoadPostfix", "postfix");
            ok &= VerifyHook(harmony, typeof(Oc.OcPlMaster),
                nameof(Oc.OcPlMaster.LoadSaveData),
                "LoadFinalizer", "finalizer");
            // Skill-tree enforcement: the TryLearnSkill prefix is a production
            // enforcement hook. A bound session must not start without it.
            ok &= VerifyHook(harmony, typeof(Oc.Skills.OcSkillManager),
                nameof(Oc.Skills.OcSkillManager.TryLearnSkill),
                "TryLearnSkillPrefix", "prefix");
            ok &= VerifyHook(harmony, typeof(Oc.Skills.OcSkillManager),
                nameof(Oc.Skills.OcSkillManager.TryAssignSkillPoint),
                "TryAssignSkillPointPrefix", "prefix");

            if (!ok)
            {
                try { harmony.UnpatchSelf(); } catch { }
                Log.LogError("[AP] FATAL: hook verification failed; bridge not started.");
                return false;
            }
            Log.LogInfo("[AP] all hooks registered and verified");
            return true;
        }

        private static bool VerifyHook(Harmony harmony, Type declaringType,
            string methodName, string patchMethodName, string kind)
        {
            try
            {
                foreach (var m in harmony.GetPatchedMethods())
                {
                    if (m.DeclaringType != declaringType || m.Name != methodName)
                        continue;
                    var info = Harmony.GetPatchInfo(m);
                    if (info == null) continue;
                    System.Collections.Generic.IEnumerable<HarmonyLib.Patch> patches =
                        kind == "prefix" ? info.Prefixes :
                        kind == "postfix" ? info.Postfixes :
                        (System.Collections.Generic.IEnumerable<HarmonyLib.Patch>)info.Finalizers;
                    foreach (var p in patches)
                    {
                        if (p.owner == GUID
                            && p.PatchMethod != null
                            && p.PatchMethod.Name == patchMethodName
                            && p.PatchMethod.DeclaringType == typeof(Patches))
                        {
                            Log.LogInfo("[AP] hook registered: " + declaringType.Name + "." +
                                methodName + " " + kind + " (" + patchMethodName + ")");
                            return true;
                        }
                    }
                }
                Log.LogError("[AP] FATAL: " + declaringType.Name + "." + methodName +
                    " " + kind + " (" + patchMethodName + ") not registered.");
                return false;
            }
            catch (Exception e)
            {
                Log.LogError("[AP] FATAL: hook verification threw for " + methodName +
                    ": " + e.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Called on LoadSaveData entry (prefix). Captures identity for
        /// validation context. Read-only; never calls GetCharacterGUID().
        /// </summary>
        internal sealed class LoadInvocation
        {
            internal Oc.OcPlMaster Player;
            internal long Event;
        }
        internal static void SafeError(string message) { try { Log?.LogError(message); } catch { } }
        internal static LoadInvocation OnLoadStart(Oc.OcPlMaster instance)
        {
            CancelEnrollment();
            CurrencyRuntime.Suspend(); StarterKitRuntime.Suspend();
            unlockMirror?.Suspend();
            ++loadEventCounter;
            loadCompletedFlag = false;
            eligibility?.Block("load entry");
            Validator?.Invalidate();
            var call = new LoadInvocation { Player = instance, Event = loadEventCounter };
            try {
                int guid = instance == null ? 0 : instance.GUID;
                SetLastPlayerGuid(guid);
                eligibility?.OnLoadStart(instance, guid, call.Event);
            }
            catch { eligibility?.Block("load entry read failed"); }
            return call;
        }
        internal static void OnLoadEnd(LoadInvocation call, Exception ex)
        {
            if (ex != null) {
                loadCompletedFlag = false; eligibility?.Block("load exception"); Validator?.Block();
                SafeError("[AP] load exception: " + ex.GetType().Name); return;
            }
            try {
                if (call == null || call.Event != loadEventCounter ||
                    !ReferenceEquals(call.Player, Oc.OcPlMng.LocalPl)) return;
                var field = typeof(Oc.OcPlMaster).GetField("_isLoaded",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (call.Player == null || field == null || field.FieldType != typeof(bool) ||
                    !(bool)field.GetValue(call.Player)) return;
                loadCompletedFlag = true;
                eligibility?.OnLoadComplete(call.Player, call.Player.GUID, call.Event);
                // Read-only baseline for explicit enrollment review, including before a bridge connects.
                try {
                    var baseline=Validator.Capture();
                    Log.LogInfo($"[AP] character baseline: guid={baseline.Input.Guid} identity={baseline.Input.Identity} ready={baseline.Ready} level={baseline.Input.Level} assigned={baseline.Input.AssignedPoints} unspent={baseline.Input.UnspentPoints} mission={baseline.Input.MissionPoints} totalsKnown={baseline.Input.TotalsKnown}");
                } catch { }
                // Identity update: menu-first connect or character switch.
                // Send the loaded character GUID to the bridge.
                try
                {
                    int guid = call.Player.GUID;
                    SetLastPlayerGuid(guid);
                    PublishLoadedIdentity();
                }
                catch (Exception ie)
                {
                    Log.LogError("[AP] identity update on load failed: " + ie.GetType().Name);
                }
            } catch {
                loadCompletedFlag = false; eligibility?.Block("load completion read failed"); Validator?.Block();
            }
        }
        /// <summary>
        /// Called by the bridge (network thread) when slot_data arrives.
        /// Enqueues for main-thread processing; does NOT touch validation state
        /// directly (thread safety). The main thread drains the queue in Update.
        /// </summary>
        internal static void OnSlotData(long epoch, System.Collections.Generic.HashSet<int> enrolledGuids, string token = null, int connection = 0, string equipmentCatalog = null, int equipmentProtocol = 0, int multiplayer = 0, System.Collections.Generic.Dictionary<string,object> currencySession = null)
        {
            try
            {
                lock (pendingSessions)
                {
                    pendingSessions.Enqueue(new SessionData(epoch, enrolledGuids, token, connection, equipmentCatalog, equipmentProtocol, multiplayer, currencySession));
                }
                Log.LogInfo("[AP] session queued: epoch=" + epoch);
            }
            catch (Exception e)
            {
                Log.LogError("[AP] OnSlotData enqueue error: " + e.GetType().Name);
            }
        }

        private struct SessionData
        {
            public long Epoch;
            public string Token;
            public string EquipmentCatalog;
            public int EquipmentProtocol, Multiplayer;
            public int Connection;
            public System.Collections.Generic.Dictionary<string,object> CurrencySession;
            public System.Collections.Generic.HashSet<int> EnrolledGuids;
            public SessionData(long epoch, System.Collections.Generic.HashSet<int> guids, string token, int connection, string equipmentCatalog, int equipmentProtocol, int multiplayer, System.Collections.Generic.Dictionary<string,object> currencySession)
            {
                Epoch = epoch;
                EnrolledGuids = guids; Token = token; Connection = connection;
                EquipmentCatalog=equipmentCatalog; EquipmentProtocol=equipmentProtocol; Multiplayer=multiplayer;
                CurrencySession=currencySession;
            }
        }

        private static readonly System.Collections.Generic.Queue<SessionData> pendingSessions =
            new System.Collections.Generic.Queue<SessionData>();

        // Pending unlocks from bridge (queued on worker thread, processed on main thread).
        private static readonly System.Collections.Generic.Queue<UnlockData> pendingUnlocks =
            new System.Collections.Generic.Queue<UnlockData>();

        internal struct UnlockData
        {
            public System.Collections.Generic.Dictionary<string, object> Message;
            public int Connection;
        }
        internal static void QueueUnlock(System.Collections.Generic.Dictionary<string,object> message, int connection)
        {
            lock (pendingUnlocks)
            {
                // Rejected overflow is not acked; durable bridge retries.
                if (pendingUnlocks.Count < 4096)
                    pendingUnlocks.Enqueue(new UnlockData { Message=message, Connection=connection });
            }
        }
        private static void ProcessPendingUnlocks(bool allowed)
        {
            if (unlockMirror == null) return;
            try
            {
                lock (pendingUnlocks)
                {
                    while (pendingUnlocks.Count > 0)
                    {
                        var item=pendingUnlocks.Dequeue();
                        unlockMirror.Accept(item.Message,item.Connection);
                    }
                }
                unlockMirror.Flush(allowed && Bridge != null && Bridge.IsConnected,
                    Bridge == null ? 0 : Bridge.ConnectionId, message => Bridge.SendUnlockAck(message));
            }
            catch { unlockMirror.Suspend(); }
        }

        /// <summary>
        /// Main-thread: drain pending sessions, invalidate eligibility, apply.
        /// Called from Update before the grant pipeline.
        /// </summary>
        private static void ProcessPendingSessions()
        {
            try
            {
                SessionData? next = null;
                lock (pendingSessions)
                {
                    // Take the latest; discard older (session is idempotent).
                    while (pendingSessions.Count > 0)
                        next = pendingSessions.Dequeue();
                }
                if (next.HasValue && Validator != null)
                {
                    var s = next.Value;
                    if (Bridge == null || s.Connection != Bridge.ConnectionId ||
                        !unlockMirror.BeginSession(SlotName.Value,s.Epoch,s.Token,s.Connection)) return;
                    if (!equipmentSession.Bind(s.Epoch,SlotName.Value,s.EquipmentCatalog,s.EquipmentProtocol,s.Multiplayer))
                    {
                        eligibility?.Block("equipment contract rejected");
                        EquipmentHooks.Diagnostic("Equipment contract rejected; delivery held. Review explicit test approval.");
                        return;
                    }
                    // Invalidate eligibility BEFORE using the new session.
                    if (eligibility != null)
                        eligibility.OnSessionChange(s.Epoch);
                    // Clear transaction dedup for new session.
                    if (grantTransaction != null && Validator.CurrentEpoch != s.Epoch)
                        grantTransaction.ClearCommittedForNewSession();
                    // Update validator.
                    if (Validator.CurrentEpoch != s.Epoch) StatusDisplay?.ClearActivity();
                    Validator.CurrentEpoch = s.Epoch;
                    var guids = s.EnrolledGuids; // capture for lambda
                    Validator.IsEnrolled = g => guids != null && guids.Contains(g);
                    Log.LogInfo("[AP] session applied: epoch=" + s.Epoch +
                        ", enrolled=" + (guids != null ? guids.Count : 0));
                    // Persist session binding separately from socket connectivity.
                    SetSessionBound(s.Epoch, SlotName?.Value);
                    CurrencyRuntime.ApplySession(s.CurrencySession,s.Connection);
                    StarterKitRuntime.ApplySession(s.CurrencySession,s.Connection);
                    MilestoneSession.Bind(s.CurrencySession);
                    sessionSeed = s.CurrencySession == null ? null : MiniJson.GetString(s.CurrencySession,"seed_name");
                    PublishLoadedIdentity();
                }
            }
            catch (Exception e)
            {
                Log.LogError("[AP] session apply error: " + e.GetType().Name);
                // Fail closed: block eligibility.
                if (eligibility != null)
                    eligibility.Block("session apply error");
            }
        }

        /// <summary>
        /// Late-session revalidation: if the character loaded before a valid
        /// session arrived (epoch 0 → Defer), and now a session is available,
        /// trigger a safe read-only revalidation. This does NOT clear recovery
        /// holds and does NOT call the game's validator.
        /// </summary>
        private static void TryLateRevalidation()
        {
            if (!loadCompletedFlag || recoveryHold || Validator == null) return;
            try { Validator.TrySoftValidation(); }
            catch { eligibility?.Block("late validation error"); Validator.Block(); }
        }
        private static void PublishLoadedIdentity()
        {
            if (!loadCompletedFlag || Validator == null || Bridge == null) return;
            var snapshot = Validator.Capture();
            if (snapshot.Ready && snapshot.Input.Identity == IdentityStatus.Verified)
                Bridge.SendIdentityUpdate(snapshot.Input.Guid, snapshot.Input.Epoch, SlotName.Value);
        }
        private static void TryRegisterEnrolledCharacter(bool allowed)
        {
            if (!allowed || Bridge == null || !Bridge.IsConnected || string.IsNullOrWhiteSpace(sessionSeed)) return;
            try {
                var snapshot = Validator.Capture();
                if (!snapshot.Ready || snapshot.Input.Identity != IdentityStatus.Verified ||
                    Validator.IsEnrolled == null || Validator.IsEnrolled(snapshot.Input.Guid)) return;
                var authority = enrollmentStateField.Read(snapshot.Input.Guid,snapshot.Input.Epoch,SlotName.Value,
                    Validator.HasHistory == null || Validator.HasHistory());
                if (authority.Status != EntitlementStatus.Ok && authority.Status != EntitlementStatus.NoRecordConfirmed) return;
                if (UnityEngine.Time.realtimeSinceStartup < nextCharacterRegistration) return;
                nextCharacterRegistration = UnityEngine.Time.realtimeSinceStartup + 5f;
                PublishLoadedIdentity();
                Bridge.SendCharacterEnrollment(snapshot.Input.Guid,snapshot.Input.Epoch,sessionSeed,SlotName.Value);
            } catch { eligibility?.Block("character registration unavailable"); }
        }
        private static bool EnrollmentEvidenceExists(string path)
        {
            try { System.IO.File.GetAttributes(path); return true; }
            catch (System.IO.FileNotFoundException) { return false; }
            catch { return true; }
        }
        private static bool EnrollmentHasHistory()
        {
            try {
                if (Validator.HasHistory == null || Validator.HasHistory()) return true;
                if (managedMode && ManagedSessionInitialized && lastPlayerGuid != 0 &&
                    (SoulOrbHooks.HasCharacterBinding(lastPlayerGuid) || CurrencyRuntime.HasEnrollmentHistory(lastPlayerGuid))) return true;
                // Legacy state, unresolved intents, and partial writes require review.
                if (EnrollmentEvidenceExists(EntitlementStorePath) || EnrollmentEvidenceExists(EntitlementStorePath + ".tmp")) return true;
                string enrollment=System.IO.Path.Combine(UserStorage.Root,
                    "craftopia-archipelago-enrollment-"+sessionFileKey+".json");
                if (EnrollmentEvidenceExists(enrollment+".tmp")) return true;
                string intent=System.IO.Path.Combine(UserStorage.Root,
                    "craftopia-archipelago-intent-"+sessionFileKey+".json");
                if (EnrollmentEvidenceExists(intent) || EnrollmentEvidenceExists(intent+".tmp")) return true;
                if (EnrollmentEvidenceExists(GrantStatePath+".tmp")) return true;
                if (EnrollmentEvidenceExists(GrantStatePath)) {
                    var saved=PersistedGrantState.Read(GrantStatePath);
                    if (saved.Epoch != Validator.CurrentEpoch || saved.Applied.Count != 0 || saved.Quarantined.Count != 0) return true;
                }
                return false;
            } catch { return true; }
        }
        private static ValidationSnapshot pendingEnrollment;
        private static string pendingEnrollmentProfile;
        private static object pendingEnrollmentWorld;
        internal static bool HasPendingEnrollment => pendingEnrollment != null && pendingEnrollmentWorld != null;
        private static EnrollmentCoordinator NewEnrollmentApproval(ValidationSnapshot s)
            => new EnrollmentCoordinator(enrollmentStateField, enrollmentStatePath + ".approval-used",
                s.Input.Guid, s.Input.Epoch, SlotName.Value, true);
        private static bool ManagedAwaitingEnrollment(ValidationSnapshot s)
        {
            if (!ManagedSessionInitialized) return true;
            return NewEnrollmentApproval(s).CanWait(s, EnrollmentHasHistory(),
                recoveryHold || Validator.IsHeld(s.Input.Guid, s.Input.Epoch));
        }
        internal static bool CanEnroll
        {
            get {
                try {
                    if (!managedMode || !ManagedSessionInitialized || !loadCompletedFlag ||
                        Bridge == null || !Bridge.IsConnected || Connection == null || !Connection.IsConnected ||
                        Validator == null || !IsSessionBound()) return false;
                    var s = Validator.Capture();
                    return EnrollmentCoordinator.IsFreshBaseline(s) && ManagedAwaitingEnrollment(s) &&
                        (!EquipmentRequired || equipmentSession.CanApprove);
                } catch { return false; }
            }
        }
        internal static string EnrollmentSummary
        {
            get {
                try {
                    if (!ManagedSessionInitialized) return "Connect to your Archipelago slot from the main menu first.";
                    if (Validator == null || Validator.CurrentEpoch <= 0) return "Waiting for the server seed and slot details.";
                    if (!loadCompletedFlag || Oc.OcPlMng.LocalPl == null) return "Load your new level-1 character, then enroll it here.";
                    var s = Validator.Capture();
                    string baseline = "Character " + s.Input.Guid + ": level " + s.Input.Level +
                        ", assigned points " + s.Input.AssignedPoints + ", unspent points " + s.Input.UnspentPoints + ". ";
                    if (recoveryHold || Validator.IsHeld(s.Input.Guid,s.Input.Epoch)) return baseline + "Delivery held for review; enrollment cannot clear a recovery hold.";
                    if (enrollmentStatus != null) return baseline + enrollmentStatus;
                    if (CanEnroll) return baseline + "Fresh baseline available. Confirm that this is a new character for this seed.";
                    var state = enrollmentStateField.Read(s.Input.Guid,s.Input.Epoch,SlotName.Value,false);
                    if (state.Status == EntitlementStatus.Ok || state.Status == EntitlementStatus.NoRecordConfirmed)
                        return baseline + "Character enrolled. World and reward checks remain active.";
                    return baseline + "Fresh enrollment unavailable: baseline, saved history, or session approval needs review.";
                } catch { return "Character details are not ready yet."; }
            }
        }
        internal static string BeginEnrollment()
        {
            CancelEnrollment();
            if (!CanEnroll) return enrollmentStatus = "Enrollment unavailable. A fresh level-1 zero-point character and a ready seed are required.";
            pendingEnrollmentWorld = MilestoneSession.CaptureFreshEnrollmentWorld();
            if (pendingEnrollmentWorld == null)
                return enrollmentStatus = "A fresh world could not be verified. Use a new world near the first tower with no completed milestones.";
            pendingEnrollment = Validator.Capture();
            pendingEnrollmentProfile = ManagedProfileKey;
            return enrollmentStatus = "Confirm fresh character " + pendingEnrollment.Input.Guid +
                " for slot " + SlotName.Value + ". This approval is used once.";
        }
        internal static void CancelEnrollment()
        {
            pendingEnrollment = null; pendingEnrollmentProfile = null; pendingEnrollmentWorld = null;
            enrollmentStatus = null;
        }
        internal static string ConfirmEnrollment()
        {
            var observed = pendingEnrollment;
            string profile = pendingEnrollmentProfile;
            object world = pendingEnrollmentWorld;
            pendingEnrollment = null; pendingEnrollmentProfile = null; pendingEnrollmentWorld = null;
            try {
                if (observed == null || profile != ManagedProfileKey || !CanEnroll ||
                    !observed.SameBinding(Validator.Capture()) || !MilestoneSession.SameFreshEnrollmentWorld(world))
                    return enrollmentStatus = "Confirmation expired or the character or world changed. Review both and begin again.";
                enrollmentCoordinator = NewEnrollmentApproval(observed);
                bool committed = enrollmentCoordinator.TryEnroll(observed, () => {
                    if (profile != ManagedProfileKey || !MilestoneSession.SameFreshEnrollmentWorld(world) || EnrollmentHasHistory() || recoveryHold ||
                        Validator.IsHeld(observed.Input.Guid,observed.Input.Epoch))
                        throw new InvalidOperationException("Enrollment evidence changed");
                    return Validator.Capture();
                }, EnrollmentHasHistory(), recoveryHold || Validator.IsHeld(observed.Input.Guid,observed.Input.Epoch));
                if (!committed) return enrollmentStatus = enrollmentCoordinator.Status;
                if (EquipmentRequired && !equipmentSession.Approve(observed.Input.Guid,observed.Input.Epoch,SlotName.Value)) {
                    eligibility?.Block("equipment approval write failed"); Validator.Block();
                    return enrollmentStatus = "Enrollment recorded, but equipment approval failed. Delivery held for review.";
                }
                CurrencyRuntime.AttestNew(observed.Input.Guid);
                    StarterKitRuntime.AttestNew(observed.Input.Guid);
                Validator.Invalidate();
                return enrollmentStatus = "Fresh character enrolled. World baseline and reward validation will run before delivery.";
            } catch {
                eligibility?.Block("enrollment error"); Validator?.Block();
                return enrollmentStatus = "Enrollment could not finish. Delivery held for review.";
            }
        }
        private static void TryFreshEnrollment()
        {
            if (!loadCompletedFlag || enrollmentCoordinator == null || Validator == null) return;
            try {
                var observed=Validator.Capture();
                if (enrollmentCoordinator.TryEnroll(observed, () => {
                    if (EnrollmentHasHistory() || recoveryHold || Validator.IsHeld(observed.Input.Guid,observed.Input.Epoch))
                        throw new InvalidOperationException("Enrollment evidence changed");
                    return Validator.Capture();
                },
                    EnrollmentHasHistory(), recoveryHold || Validator.IsHeld(observed.Input.Guid,observed.Input.Epoch)))
                {
                    CurrencyRuntime.AttestNew(observed.Input.Guid);
                    StarterKitRuntime.AttestNew(observed.Input.Guid);
                    Validator.Invalidate(); // Eligibility still requires fresh read-only validation below.
                }
                if (enrollmentStatus != enrollmentCoordinator.Status) {
                    enrollmentStatus=enrollmentCoordinator.Status;
                    Log.LogInfo("[AP] enrollment: "+enrollmentStatus);
                }
            } catch { eligibility?.Block("enrollment error"); Validator.Block(); }
        }
        /// <summary>
        /// Called by the grant pipeline after a successful application.
        /// Commits the entitlement (points) for (guid, epoch).
        /// </summary>
        internal static bool CommitEntitlement(int guid, long epoch, int points)
        {
            try
            {
                if (entitlementStore != null)
                    return entitlementStore.Commit(guid, epoch, points);
                return false;
            }
            catch (Exception e)
            {
                Log.LogError("[AP] CommitEntitlement error: " + e.GetType().Name);
                return false;
            }
        }

        private string DescribeGameObject()
        {
            try
            {
                string parent;
                try { parent = transform.parent == null ? "null(root)" : "'" + transform.parent.name + "'"; }
                catch { parent = "?"; }
                string activeScene;
                try { activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; }
                catch { activeScene = "?"; }
                return "go='" + gameObject.name + "' scene='" + gameObject.scene.name
                    + "' activeScene='" + activeScene + "' parent=" + parent
                    + " hideFlags=" + gameObject.hideFlags;
            }
            catch (Exception e)
            {
                return "unavailable(" + e.GetType().Name + ")";
            }
        }

        private static string SanitizeSlot(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "")
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            return sb.Length > 0 ? sb.ToString() : "Player1";
        }

        private void Update()
        {
            // Only the owning instance drives the pipeline; a duplicate
            // awaiting destruction must not touch it.
            if (!ReferenceEquals(instance, this)) return;

            // 0. Process pending sessions (from network thread) BEFORE validation.
            // This invalidates eligibility and applies the new epoch/enrollment.
            Connection?.Pump();
            ProcessPendingSessions();
            try { PollStatusKey(); Bridge?.PumpNotifications(); } catch { }
            try { PollHudPointer(); } catch (Exception ex) {
                hudDrag.Cancel();
                try { StatusDisplay?.Transition("hud-input-error", "[AP] HUD input unavailable: " + ex.GetBaseException().Message); } catch { }
            }
            if (!managedMode) TryFreshEnrollment();


            // 0b. Late-session revalidation: if load is complete, session is
            // available (epoch != 0), but eligibility is blocked due to an
            // earlier Defer (e.g., character loaded before session arrived),
            // trigger a safe read-only revalidation. This does NOT clear
            // recovery holds.
            TryLateRevalidation();

            // Poll readiness every frame so the NOT READY / READY transition
            // lines are guaranteed even with no queued grants (e.g. at the
            // main menu, where the applier would otherwise never run).
            try { grantApplicator.PollReadiness(); }
            catch (Exception e) { Log.LogError("[AP] readiness poll error: " + e); }

            long frame = UnityEngine.Time.frameCount;

            // 1. Drain inbound grants. The main thread owns the pipeline; the
            // network thread only ever enqueued into the inbound queue.
            System.Collections.Generic.List<GrantPipeline.InboundResult> inboundResults;
            try
            {
                inboundResults = Pipeline.DrainInbound();
            }
            catch (Exception e)
            {
                Log.LogError("[AP] grant pipeline drain error: " + e);
                inboundResults = new System.Collections.Generic.List<GrantPipeline.InboundResult>();
            }
            foreach (var r in inboundResults)
            {
                // Note: Reack is NOT sent here. It is sent after the validation
                // gate below, because a Defer (uncertain session/validation)
                // must block even duplicate acks. The reack indices are preserved
                // in the result for later.
                if (r.FreshQueued > 0)
                    Log.LogInfo("[AP] received grant for " + r.FreshQueued + " new item indice(s)");
            }

            // 2. Advance queued jobs, gated by validation outcome.
            // If the validator deferred (uncertain entitlement, discrepancy,
            // unknown inputs), grants are blocked: they stay queued, unacked.
            // Only a confirmed Pass allows delivery.
            bool validationAllows = false;
            try
            {
                if (Validator != null)
                {
                    var outcome = Validator.LastOutcome.Outcome;
                    // LastOutcome defaults to Defer (zero-init) until first validation.
                    validationAllows = (outcome == ValidationOutcome.Pass);
                    StatusDisplay?.Transition("validation", validationAllows ? null : "[AP] grants blocked: validation outcome=" + outcome);
                }
            }
            catch (Exception e)
            {
                Log.LogError("[AP] validation gate error: " + e.GetType().Name);
                validationAllows = false; // fail closed
            }

            // Eligibility check: bound to player/guid/epoch/load.
            bool identityEligible = false;
            bool eligible = false;
            try
            {
                if (eligibility != null && Validator != null)
                {
                    object playerRef = null;
                    int guid = 0;
                    try
                    {
                        var pl = Oc.OcPlMng.LocalPl;
                        if (pl != null)
                        {
                            playerRef = pl;
                            guid = pl.GUID;
                        }
                    }
                    catch { }
                    long epoch = Validator.CurrentEpoch;
                    identityEligible = !Validator.IsHeld(guid, epoch) &&
                        eligibility.IsEligible(playerRef, guid, epoch, loadEventCounter);
                    eligible = Pipeline.Epoch == epoch && identityEligible;
                    StatusDisplay?.Transition("eligibility", eligible ? null : "[AP] grants blocked: not eligible (player/guid/epoch/load mismatch)");
                }
            }
            catch (Exception e)
            {
                Log.LogError("[AP] eligibility check error: " + e.GetType().Name);
                identityEligible = false;
                eligible = false; // fail closed
            }

            bool equipmentContractReady = equipmentSession != null && equipmentSession.ContractReady;
            bool equipmentIdentityReady = !EquipmentRequired || EquipmentPermissions() != null;
            ProcessPendingUnlocks(validationAllows && identityEligible && !recoveryHold && equipmentContractReady && equipmentIdentityReady);
            // Restore ownership before admission: missing unlocks must not deadlock their own delivery.
            bool equipmentAdmitted = equipmentSession != null && equipmentSession.Admit(equipmentIdentityReady,EquipmentHooks.CurrentEquipmentAllowed);
            TryRegisterEnrolledCharacter(validationAllows && identityEligible && !recoveryHold && equipmentAdmitted);
            CurrencyRuntime.Pump(validationAllows && identityEligible && !recoveryHold && equipmentAdmitted);
            StarterKitRuntime.Pump(validationAllows && identityEligible && !recoveryHold && equipmentAdmitted);
            MilestoneSession.Pump(validationAllows && identityEligible && !recoveryHold && equipmentAdmitted);

            System.Collections.Generic.List<long> acks =
                new System.Collections.Generic.List<long>();
            if (validationAllows && eligible && !recoveryHold && equipmentAdmitted)
            {
                try
                {
                    // Transactional grant: intent → mutation → atomic commit → ack.
                    // The transaction handles entitlement (cumulative), applied
                    // persistence, and intent clearance atomically.
                    acks = ExecuteTransactionalGrants(frame);
                }
                catch (Exception e)
                {
                    Log.LogError("[AP] grant pipeline error: " + e);
                    return;
                }
            }
            else if (recoveryHold)
            {
                StatusDisplay?.Transition("recovery", "[AP] grants blocked: recovery hold (incomplete intent)");
            }
            // else: grants remain queued; no ack; retry next frame after validation.

            // Send duplicate re-acks (from inbound drain) only if validation allows.
            // If blocked, they stay queued for next frame.
            // Reacks gated on validation AND eligibility AND no recovery hold.
            // All ack paths use the same gate.
            if (validationAllows && eligible && !recoveryHold && equipmentAdmitted)
            {
                foreach (var r in inboundResults)
                {
                    if (r.Reack.Count > 0 && r.AckEpoch == Validator.CurrentEpoch)
                    {
                        Bridge.SendAck(r.Reack, r.AckEpoch);
                        Log.LogInfo("[AP] duplicate grant ignored, re-acked indices ["
                            + string.Join(",", r.Reack.ConvertAll(x => x.ToString()).ToArray()) + "]");
                    }
                }
            }

            if (acks.Count > 0)
            {
                Log.LogInfo("[AP] granted skill point(s) for item indices ["
                    + string.Join(",", acks.ConvertAll(x => x.ToString()).ToArray()) + "]");
                // The transaction already performed the atomic commit
                // (entitlement + applied + intent clearance) before returning
                // these acks. Send the ack now.
                Bridge.SendAck(acks, Pipeline.Epoch);
            }
        }

        /// <summary>
        /// In-game status overlay (Unity IMGUI). Toggle with F8.
        /// Shows connection, slot/session, permissions, goal progress, recovery holds.
        /// </summary>
        private readonly OrbMapView orbMap = new OrbMapView();
        private readonly FieldGuideView fieldGuide = new FieldGuideView();
        private void PollStatusKey()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F8)) {
                fieldGuide.Toggle(); hudDrag.Cancel();
            } else if (fieldGuide.IsOpen && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape)) {
                fieldGuide.SetOpen(false); hudDrag.Cancel();
            }
        }
        private void LateUpdate()
        {
            if (ReferenceEquals(instance, this)) {
                fieldGuide.KeepCursor();
                try { orbMap.Refresh(OrbMapTracker.State); } catch(Exception e) { StatusDisplay?.Transition("orb-map-error", "AP map unavailable: " + e.GetType().Name); }
                StatusDisplay?.ItemNotices.Advance(UnityEngine.Time.unscaledDeltaTime,
                    !UnityEngine.Application.isFocused || fieldGuide.IsOpen);
            }
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { fieldGuide.SetOpen(false); hudDrag.Cancel(); }
        }
        private void PollHudPointer()
        {
            if (!UnityEngine.Application.isFocused || !UnityEngine.Cursor.visible
                || UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.Locked) { hudDrag.Cancel(); return; }
            var mouse = UnityEngine.Input.mousePosition;
            float scale = GuideScale();
            float width = 244 * scale;
            float x = PlayerStatusDisplay.ClampHudPosition(hudDrag.Active ? hudDrag.X : hudX.Value * scale, UnityEngine.Screen.width, width);
            float y = PlayerStatusDisplay.ClampHudPosition(hudDrag.Active ? hudDrag.Y : hudY.Value * scale, UnityEngine.Screen.height, 52 * scale);
            bool save = hudDrag.Step(UnityEngine.Input.GetMouseButtonDown(0),
                UnityEngine.Input.GetMouseButton(0), UnityEngine.Input.GetMouseButtonUp(0),
                mouse.x, UnityEngine.Screen.height - mouse.y, x, y, width - 36 * scale, 52 * scale,
                UnityEngine.Screen.width - 36 * scale, UnityEngine.Screen.height, 52 * scale);
            if (save) { hudX.Value = hudDrag.X / scale; hudY.Value = hudDrag.Y / scale; }
        }
        private static float GuideScale()
        {
            return Math.Max(.4f, Math.Min(1.5f, Math.Min(UnityEngine.Screen.height / 900f, UnityEngine.Screen.width / 1050f)));
        }
        private void OnGUI()
        {
            if (!ReferenceEquals(instance, this)) return;
            try {
                if(!fieldGuide.IsOpen) orbMap.DrawToggle();
                float scale = GuideScale();
                fieldGuide.Draw(StatusDisplay, Permissions, SlotName?.Value ?? "Unknown",
                    Bridge != null && Bridge.IsConnected && (!managedMode || Connection?.IsConnected == true), recoveryHold,
                    enrollmentStatus ?? (Validator != null && Validator.CurrentEpoch > 0 ? "Validation: " + Validator.LastOutcome.Outcome : null),
                    hudDrag.Active ? hudDrag.X / scale : hudX?.Value ?? 220,
                    hudDrag.Active ? hudDrag.Y / scale : hudY?.Value ?? 20, scale);
            } catch (Exception ex) {
                StatusDisplay?.Transition("field-guide-error", "[AP] Field Guide unavailable: " + ex.GetBaseException().Message);
            }
        }
        /// <summary>
        /// Execute grants transactionally: for each pending batch, run the
        /// full intent → mutation → atomic commit transaction.
        /// Returns indices safe to ack (transaction succeeded).
        /// </summary>
        private System.Collections.Generic.List<long> ExecuteTransactionalGrants(long frame)
        {
            var allAcks = new System.Collections.Generic.List<long>();

            // Get current guid for entitlement.
            int guid = 0;
            bool gotGuid = false;
            try
            {
                var pl = Oc.OcPlMng.LocalPl;
                if (pl != null) { guid = pl.GUID; gotGuid = true; }
            }
            catch { }
            if (!gotGuid)
            {
                Log.LogWarning("[AP] cannot get player GUID; grants blocked");
                return allAcks;
            }

            long epoch = Pipeline.Epoch;
            int pointsPerIndex = 1; // Bridge expands to one-point virtual indices.

            // Use Pipeline to drain inbound and manage job queue, but the
            // actual apply is transactional.
            // Inbound messages are drained exactly once, at the start of Update.
            // A second drain here could rotate the epoch after eligibility was checked.
            // Note: DrainInbound already handled reacks via OnGrantItems.
            // We need to get the pending jobs to apply. For simplicity, we'll
            // use a custom drain that returns indices ready to apply.

            // Actually: Pipeline.OnFrame handles the job state machine.
            // We'll call it with a transactional tryApply that does the full
            // transaction (intent→mutate→commit). The tryPersist is a no-op
            // because the transaction already persisted atomically.
            var acks = Pipeline.OnFrame(
                frame,
                ids => TransactionalApply(ids, guid, epoch, pointsPerIndex),
                () => true // no-op; transaction handled persistence
            );
            allAcks.AddRange(acks);
            return allAcks;
        }

        /// <summary>
        /// Transactional applicator: wraps the game mutation with intent,
        /// cumulative entitlement commit, and atomic persistence.
        /// Returns GrantApplyResult for the Pipeline state machine.
        /// </summary>
        private GrantApplyResult TransactionalApply(
            System.Collections.Generic.List<long> ids,
            int guid,
            long epoch,
            int pointsPerIndex)
        {
            if (grantTransaction == null)
                return GrantApplyResult.Retryable;

            // The transaction does: intent → mutate → commit → return acks.
            var acked = grantTransaction.Execute(
                ids, guid, epoch, pointsPerIndex,
                innerIds => grantApplicator.TryApply(innerIds));

            if (acked.Count == 0)
            {
                // Transaction did not complete.
                return GrantApplyResult.Retryable;
            }

            // Transaction succeeded (mutation + atomic commit done).
            return GrantApplyResult.Confirmed;
        }
        /// </summary>
        private static bool SaveGrantStateFor(System.Collections.Generic.List<long> indices)
        {
            try { PersistedGrantState.MergeApplied(GrantStatePath, Pipeline.Epoch, indices); return true; }
            catch (Exception e) { SafeError("[AP] grant state write refused: " + e.GetType().Name); return false; }
        }
        private void LoadGrantState()
        {
            try {
                if (!System.IO.File.Exists(GrantStatePath)) return;
                var state = PersistedGrantState.Read(GrantStatePath);
                Pipeline.Restore(state.Epoch, new System.Collections.Generic.List<long>(state.Applied),
                    new System.Collections.Generic.List<long>(state.Quarantined));
            } catch (Exception e) {
                recoveryHold = true;
                SafeError("[AP] RECOVERY HOLD: invalid grant state: " + e.GetType().Name);
            }
        }
    }
}
