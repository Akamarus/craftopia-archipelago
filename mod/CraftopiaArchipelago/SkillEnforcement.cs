// Pure skill-learn enforcement decision logic.
// Extracted from Patches.TryLearnSkillPrefix for headless testing.
// Has no Unity/Harmony dependencies.

using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Skill learn enforcement decisions.
    ///
    /// Rules:
    /// - Not session-bound: allow (fail-open, vanilla behavior).
    /// - Session-bound: fail-closed. Any enforcement error denies.
    ///   - Permissions null -> deny (cannot check).
    ///   - Unknown skill ID -> deny (cannot permission-check).
    ///   - Tree/tier locked -> deny.
    ///   - Exception during enforcement -> deny.
    /// Diagnostics (logging, status display) are non-throwing and
    /// cannot affect the decision.
    /// </summary>
    public static class SkillEnforcement
    {
        /// <summary>
        /// Session binding policy for enforcement.
        ///
        /// A bound AP session remains bound for enforcement purposes even
        /// during a recovery hold or disconnect. The hold blocks grants/acks
        /// (delivery eligibility), but must never turn a bound player into
        /// vanilla mode. Enforcement uses existing permissions while bound.
        ///
        /// This is the contract that Plugin.IsSessionBound must implement.
        /// </summary>
        public static bool IsEnforcementBound(bool sessionBound)
        {
            // Binding is independent of recovery hold and connectivity.
            // Only the explicit session state matters.
            return sessionBound;
        }

        /// <summary>
        /// Delivery eligibility: grants/acks require bound session AND
        /// no recovery hold. Separate from enforcement binding.
        /// </summary>
        public static bool IsDeliveryEligible(bool sessionBound, bool recoveryHold)
        {
            return sessionBound && !recoveryHold;
        }
        /// <summary>
        /// Skill catalog entry: category and tier for a skill ID.
        /// Mirrors Plugin.SkillInfo for testability.
        /// </summary>
        public class SkillInfo
        {
            public string Category;
            public int Tier;
        }

        public static bool DecideSkillLearn(
            bool sessionBound,
            PermissionManager permissions,
            Func<int, SkillInfo> resolveSkill,
            int skillId,
            Action<string> logInfo,
            Action<string> showBlocked)
        {
            // Not bound: vanilla behavior, fail-open.
            if (!sessionBound) return true;

            try
            {
                // Bound but permissions unavailable: cannot check -> deny.
                if (permissions == null)
                {
                    SafeDiagnostic(logInfo, showBlocked,
                        $"[AP] Skill blocked: permissions unavailable for skill {skillId}.");
                    return false;
                }

                var skillInfo = resolveSkill(skillId);
                if (skillInfo == null)
                {
                    SafeDiagnostic(logInfo, showBlocked,
                        $"[AP] Skill blocked: unknown skill ID {skillId} not in catalog 20261008_r1.");
                    return false;
                }

                if (!permissions.IsSkillTreeUnlocked(skillInfo.Category, skillInfo.Tier))
                {
                    SafeDiagnostic(logInfo, showBlocked,
                        $"[AP] Skill locked: {skillInfo.Category} Tier {skillInfo.Tier} " +
                        "requires an Archipelago unlock.");
                    return false;
                }

                return true;
            }
            catch
            {
                // Enforcement error while bound: deny. Never allow on error.
                SafeDiagnostic(logInfo, showBlocked,
                    $"[AP] Skill blocked: enforcement error for skill {skillId}.");
                return false;
            }
        }

        /// <summary>
        /// Non-throwing diagnostic: logging and status display must never
        /// throw, or they could bypass a deny decision.
        /// </summary>
        public static void SafeDiagnostic(
            Action<string> logInfo, Action<string> showBlocked, string message)
        {
            try { logInfo?.Invoke(message); } catch { }
            try { showBlocked?.Invoke(message); } catch { }
        }

        /// <summary>
        /// Parse the versioned skill catalog JSON into ID -> SkillInfo.
        /// Returns null on invalid JSON; empty dictionary on valid but empty.
        /// Validates catalog_id, category names, and tier range.
        /// </summary>
        public static Dictionary<int, SkillInfo> ParseSkillCatalog(
            string json, string expectedCatalogId, Action<string> logError)
        {
            var result = new Dictionary<int, SkillInfo>();
            try
            {
                var data = MiniJson.ParseObject(json);
                if (data == null)
                {
                    logError?.Invoke("[AP] Skill catalog: invalid JSON");
                    return null;
                }
                if (!data.TryGetValue("catalog_id", out object cid) ||
                    !(cid as string == expectedCatalogId))
                {
                    logError?.Invoke("[AP] Skill catalog: unexpected catalog_id");
                    return null;
                }
                var skills = data.TryGetValue("skills", out object s) ?
                    s as Dictionary<string, object> : null;
                if (skills == null)
                {
                    logError?.Invoke("[AP] Skill catalog: missing skills");
                    return null;
                }
                foreach (var kv in skills)
                {
                    if (!int.TryParse(kv.Key, out int id) || id <= 0)
                        continue;
                    var entry = kv.Value as Dictionary<string, object>;
                    if (entry == null) continue;
                    string category = entry.TryGetValue("category", out object c) ? c as string : null;
                    if (string.IsNullOrEmpty(category)) continue;
                    int tier = 0;
                    if (entry.TryGetValue("tier", out object t) && t is long tl)
                        tier = (int)tl;
                    if (tier < 1 || tier > 5) continue;
                    result[id] = new SkillInfo { Category = category, Tier = tier };
                }
                return result;
            }
            catch (Exception e)
            {
                logError?.Invoke("[AP] Skill catalog parse failed: " + e.GetType().Name);
                return null;
            }
        }
    }
}
