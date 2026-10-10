using System;
using HarmonyLib;
using Oc.Missions;
using Oc.Skills;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// v1.0 patches: check detection, validation gating, load observation.
    /// All patches are non-throwing and never mutate game state.
    /// </summary>
    /// <remarks>
    /// The class-level [HarmonyPatch] is required: the assembly-scanning
    /// PatchAll path skips classes without Harmony annotations, so the
    /// method-level targets below would otherwise never be registered.
    /// </remarks>
    [HarmonyPatch]
    internal static class Patches
    {
        // ---- Check detection: mission reward claim ----

        [HarmonyPatch(typeof(Mission), nameof(Mission.TryGetReward))]
        [HarmonyPostfix]
        private static void MissionRewardPostfix(Mission __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!__instance.IsRewardTaken) return;

                int id = __instance.ID;
                string title;
                try { title = __instance.Title ?? ""; }
                catch { title = ""; }
                string className;
                try { className = __instance.GetType().FullName ?? ""; }
                catch { className = ""; }

                Plugin.Log.LogInfo("[AP] check: mission " + id + " '" + title +
                    "' class=" + className);
                Plugin.Bridge.SendCheck(id, title, className);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[AP] patch error (mission): " + e.GetType().Name);
            }
        }

        [HarmonyPatch(typeof(OcSkillManager), nameof(OcSkillManager.GameStartCheckSkillPoint))]
        [HarmonyPrefix]
        private static bool ValidatorPrefix(out ValidationInvocation __state)
        {
            __state = null;
            try { return Plugin.Validator != null && Plugin.Validator.ShouldRunValidator(out __state); }
            catch { Plugin.Validator?.Block(); Plugin.SafeError("[AP] validator prefix failed"); return false; }
        }
        [HarmonyPatch(typeof(OcSkillManager), nameof(OcSkillManager.GameStartCheckSkillPoint))]
        [HarmonyPostfix]
        private static void ValidatorPostfix(ValidationInvocation __state)
        {
            try { Plugin.Validator?.OriginalReturned(__state); }
            catch { Plugin.Validator?.Block(); Plugin.SafeError("[AP] validator readback failed"); }
        }
        [HarmonyPatch(typeof(OcSkillManager), nameof(OcSkillManager.GameStartCheckSkillPoint))]
        [HarmonyFinalizer]
        private static void ValidatorFinalizer(Exception __exception, ValidationInvocation __state)
        {
            if (__exception == null) return;
            try { Plugin.Validator?.OriginalFailed(__state); } catch { }
            // Void finalizer: preserves the original exception.
        }
        [HarmonyPatch(typeof(Oc.OcPlMaster), nameof(Oc.OcPlMaster.LoadSaveData))]
        [HarmonyPrefix]
        private static void LoadPrefix(Oc.OcPlMaster __instance, out Plugin.LoadInvocation __state)
        {
            __state = null;
            try { __state = Plugin.OnLoadStart(__instance); }
            catch { Plugin.Validator?.Block(); Plugin.SafeError("[AP] load entry observation failed"); }
        }
        [HarmonyPatch(typeof(Oc.OcPlMaster), nameof(Oc.OcPlMaster.LoadSaveData))]
        [HarmonyPostfix]
        private static void LoadPostfix(Plugin.LoadInvocation __state)
        {
            try { Plugin.OnLoadEnd(__state, null); }
            catch { Plugin.Validator?.Block(); Plugin.SafeError("[AP] load return observation failed"); }
        }
        [HarmonyPatch(typeof(Oc.OcPlMaster), nameof(Oc.OcPlMaster.LoadSaveData))]
        [HarmonyFinalizer]
        private static void LoadFinalizer(Exception __exception, Plugin.LoadInvocation __state)
        {
            if (__exception == null) return;
            try { Plugin.OnLoadEnd(__state, __exception); } catch { }
        }

        // Production checks are installed at pre-cost boundaries by ProductionHooks.

        // ---- Skill tree enforcement ----

        // The normal skill menu calls this boundary directly, bypassing
        // TryLearnSkill. Deny before ApplySp, stored ranks or SP cost change.
        [HarmonyPatch(typeof(OcSkillManager), nameof(OcSkillManager.TryAssignSkillPoint),
            new Type[] { typeof(OcSkill), typeof(int), typeof(bool) })]
        [HarmonyPrefix]
        private static bool TryAssignSkillPointPrefix(OcSkill __0, ref bool __result)
        {
            if (!Plugin.IsSessionBound()) return true;
            try
            {
                if (SkillEnforcement.DecideSkillLearn(true, Plugin.Permissions,
                    sid =>
                    {
                        var info = Plugin.ResolveSkillInfo(sid);
                        return info == null ? null : new SkillEnforcement.SkillInfo
                            { Category = info.Category, Tier = info.Tier };
                    }, __0.ID,
                    m => Plugin.Log.LogInfo(m),
                    m => Plugin.StatusDisplay?.ShowBlockedAction(m))) return true;
            }
            catch
            {
                SkillEnforcement.SafeDiagnostic(m => Plugin.Log.LogError(m),
                    m => Plugin.StatusDisplay?.ShowBlockedAction(m),
                    "[AP] Skill blocked: assignment identity could not be resolved.");
            }
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(OcSkillManager), nameof(OcSkillManager.TryLearnSkill))]
        [HarmonyPrefix]
        private static bool TryLearnSkillPrefix(OcSkillManager __instance, int id, int level, bool checkLock)
        {
            // Delegate to the pure, tested decision logic in SkillEnforcement.
            // The prefix itself never throws; all enforcement errors deny.
            // Plugin.SkillInfo and SkillEnforcement.SkillInfo have the same shape.
            return SkillEnforcement.DecideSkillLearn(
                Plugin.IsSessionBound(),
                Plugin.Permissions,
                sid =>
                {
                    var pi = Plugin.ResolveSkillInfo(sid);
                    return pi == null ? null :
                        new SkillEnforcement.SkillInfo { Category = pi.Category, Tier = pi.Tier };
                },
                id,
                m => Plugin.Log.LogInfo(m),
                m => Plugin.StatusDisplay?.ShowBlockedAction(m));
        }
    }
}
