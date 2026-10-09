// Validation integration: bridges game state to ValidationDecision.Decide().
//
// At the GameStartCheckSkillPoint prefix, this:
// 1. Reads game totals (level, K, mission points, assigned, unspent) — read-only.
// 2. Reads identity (OcPl.GUID, _selectedCharacter.GUID) — read-only, never GetCharacterGUID().
// 3. Reads entitlement from the store for (guid, epoch).
// 4. Calls ValidationDecision.Decide() with explicit inputs.
// 5. Returns the outcome: Pass (skip validator), RunVanilla (run original),
//    Defer (skip validator, preserve, block grants).
//
// All reads are non-throwing. Unknown inputs -> Defer.

using System;
using System.Reflection;

namespace CraftopiaArchipelago
{
    public class Validator
    {
        private readonly EntitlementStore store;
        private readonly Action<string> logInfo;
        private readonly Action<string> logError;

        private FieldInfo isLoadedField;
        private FieldInfo selectedCharField;

        // Current slot name (from Plugin config). Set by Plugin.
        public static string SlotName { get; set; }
        // Atomic enrollment state (from Plugin). Set by Plugin.
        public static EnrollmentState EnrollmentState { get; set; }
        // Current epoch (from bridge slot_data or config). Set by Plugin.
        private long currentEpoch;
        private readonly ValidationGate gate;
        public Func<long> LoadEvent { get; set; }
        public Func<bool> LoadCompleted { get; set; }
        public long CurrentEpoch {
            get => currentEpoch;
            set { if (value != currentEpoch) Invalidate(); currentEpoch = value; }
        }

        // Whether the current character is enrolled (from init file). Set by Plugin.
        public Func<int, bool> IsEnrolled { get; set; }

        // Whether history indicators exist (applied/quarantined non-empty, legacy).
        // Set by Plugin from grant state.
        public Func<bool> HasHistory { get; set; }
        public Func<ValidationSnapshot, bool> AwaitingEnrollment { get; set; }

        // Callback for eligibility: invoked after validation with outcome and
        // the bound identity (playerRef, guid, epoch, loadEventId).
        // Set by Plugin.
        public Action<ValidationOutcome, object, int, long, long> OnValidationCallback { get; set; }

        public Validator(EntitlementStore store, Action<string> logInfo, Action<string> logError, RecoveryHoldStore holds)
        {
            this.store = store; gate = new ValidationGate(holds);
            this.logInfo = logInfo;
            this.logError = logError;
            try
            {
                isLoadedField = typeof(Oc.OcPlMaster).GetField("_isLoaded",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                selectedCharField = typeof(Oc.OcPlCharacterManager).GetField("_selectedCharacter",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception e)
            {
                Error("[AP] validator reflection init failed: " + e.GetType().Name);
            }
        }

        private void Info(string s) { try { logInfo?.Invoke(s); } catch { } }
        private void Error(string s) { try { logError?.Invoke(s); } catch { } }
        public ValidationResult LastOutcome => gate.Last;
        public void Invalidate()
        {
            gate.Invalidate();
            try { OnValidationCallback?.Invoke(ValidationOutcome.Defer, null, 0, 0, 0); } catch { }
        }
        public void Block()
        {
            gate.Invalidate(false);
            try { OnValidationCallback?.Invoke(ValidationOutcome.Defer, null, 0, 0, 0); } catch { }
        }
        private void Publish(ValidationSnapshot s)
        {
            try { OnValidationCallback?.Invoke(gate.Last.Outcome, s?.Player, s?.Input.Guid ?? 0,
                s?.Input.Epoch ?? 0, s?.LoadEvent ?? 0); }
            catch { Block(); }
            Info("[AP] validation: " + gate.Last.Outcome + " " + gate.Last.Reason);
        }
        public bool ShouldRunValidator(out ValidationInvocation call)
        {
            Invalidate(); call = null;
            try {
                var snapshot = Capture();
                call = gate.Begin(snapshot, false);
                Publish(snapshot);
                return call.RunOriginal;
            } catch (Exception e) { Block(); Error("[AP] validation failed: " + e.GetType().Name); return false; }
        }
        public void OriginalReturned(ValidationInvocation call)
        {
            if (call == null || !call.RunOriginal) return;
            try { var s = Capture(); gate.Complete(call, s); Publish(s); }
            catch { OriginalFailed(call); }
        }
        public void OriginalFailed(ValidationInvocation call)
        {
            Block();
            try { gate.Failed(call); } catch { }
            Error("[AP] validator exception; grants blocked; original exception preserved");
        }
        public void TrySoftValidation()
        {
            gate.RetryHoldWrite();
            if (!gate.CanSoftRetry) return;
            try {
                if (LoadCompleted == null || !LoadCompleted()) return;
                var s = Capture();
                if (!s.Ready || currentEpoch <= 0) return;
                gate.Begin(s, true); Publish(s);
            } catch { Block(); Error("[AP] soft validation failed"); }
        }
        public bool IsHeld(int guid, long epoch) => gate.IsHeld(guid, epoch);
        internal ValidationSnapshot Capture()
        {
            var pl = Oc.OcPlMng.LocalPl;
            long epoch = CurrentEpoch;
            long load = LoadEvent == null ? 0 : LoadEvent();
            var input = BuildInput(pl, epoch);
            bool ready = pl != null && pl.isLocalPlayer && isLoadedField != null && isLoadedField.FieldType == typeof(bool) &&
                (bool)isLoadedField.GetValue(pl) && LoadCompleted != null && LoadCompleted();
            if (!ReferenceEquals(pl, Oc.OcPlMng.LocalPl) || epoch != CurrentEpoch ||
                load != (LoadEvent == null ? 0 : LoadEvent()))
                input.Identity = IdentityStatus.Unknown;
            var snapshot = new ValidationSnapshot { Player = pl, LoadEvent = load, Input = input, Ready = ready };
            snapshot.AwaitingEnrollment = AwaitingEnrollment != null && AwaitingEnrollment(snapshot);
            return snapshot;
        }
        private ValidationInput BuildInput(Oc.OcPlMaster pl, long epoch)
        {
            var input = new ValidationInput();
            input.TotalsKnown = false; // must prove all reads succeeded

            // Game totals (read-only). Any failure -> TotalsKnown=false -> Defer.
            // Do NOT substitute defaults; unknown inputs must defer.
            bool totalsOk = true;
            try
            {

                if (pl == null)
                {
                    totalsOk = false;
                }
                else
                {
                    try
                    {
                        var lvlCtrl = pl.PlLevelCtrl;
                        var levelProp = lvlCtrl.GetType().GetProperty("Level");
                        var br = levelProp.GetValue(lvlCtrl, null);
                        var valProp = br.GetType().GetProperty("Value");
                        input.Level = Convert.ToInt32(valProp.GetValue(br, null));
                    }
                    catch { totalsOk = false; }
                    try { input.K = Oc.OcDefine.INCREASE_SKILLPOINT_BY_LEVEL_UP; }
                    catch { totalsOk = false; }
                    try { input.MissionPoints = Oc.Missions.OcMissionManager.Inst.GetTotalSkillPointTaken(); }
                    catch { totalsOk = false; }
                    try { input.AssignedPoints = Oc.Skills.OcSkillManager.Inst.CurrentAssignedSP; }
                    catch { totalsOk = false; }
                    try { input.UnspentPoints = pl.SkillCtrl.SkillPoint; }
                    catch { totalsOk = false; }
                }
            }
            catch { totalsOk = false; }
            input.TotalsKnown = totalsOk;

            // Identity (read-only; never GetCharacterGUID).
            // Requires BOTH OcPl.GUID and _selectedCharacter.GUID to be readable
            // and agree. If either is missing or they differ, identity is Unknown.
            try
            {

                int plGuid = 0;
                bool gotPlGuid = false;
                if (pl != null)
                {
                    try { plGuid = pl.GUID; gotPlGuid = true; }
                    catch { }
                }

                int scGuid = 0;
                bool gotScGuid = false;
                try
                {
                    var cm = Oc.OcPlCharacterManager.Inst;
                    if (cm != null && selectedCharField != null)
                    {
                        var sc = selectedCharField.GetValue(cm) as Oc.OcPlCharaSaveData;
                        if (sc != null)
                        {
                            scGuid = sc.GUID;
                            gotScGuid = true;
                        }
                    }
                }
                catch { }

                if (gotPlGuid && gotScGuid && plGuid != 0 && plGuid == scGuid)
                {
                    input.Identity = IdentityStatus.Verified;
                    input.Guid = plGuid;
                    input.Epoch = epoch;
                }
                else
                {
                    // Missing or disagreeing GUIDs -> Unknown -> Defer.
                    // Do not assert an identity from partial evidence.
                    input.Identity = IdentityStatus.Unknown;
                }
            }
            catch
            {
                input.Identity = IdentityStatus.Unknown;
            }

            // Entitlement (from store).
            try
            {
                if (input.Identity == IdentityStatus.Verified && epoch > 0)
                {
                    bool enrolled = false;
                    try { enrolled = IsEnrolled != null && IsEnrolled(input.Guid); }
                    catch { throw; }
                    bool hasHistory = false;
                    try { hasHistory = HasHistory == null || HasHistory(); }
                    catch { throw; }

                    // A configured enrollment authority is exclusive, including failures.
                    var read = CraftopiaArchipelago.EnrollmentState.Resolve(EnrollmentState, store,
                        input.Guid, input.Epoch, SlotName, enrolled, hasHistory);
                    input.Entitlement = read.Status;
                    if (read.Status == EntitlementStatus.Ok)
                    {
                        input.EntitlementPoints = read.Points;
                        input.BoundGuid = read.BoundGuid;
                        input.BoundEpoch = read.BoundEpoch;
                    }
                }
                else
                {
                    input.Entitlement = EntitlementStatus.Unknown;
                }
            }
            catch
            {
                input.Entitlement = EntitlementStatus.Unknown;
            }

            return input;
        }
    }
}


