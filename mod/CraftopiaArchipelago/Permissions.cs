using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Permission model for the full gameplay design.
    ///
    /// Categories:
    /// - Equipment: category + tier permissions govern crafting AND use.
    /// - Production: station/process permissions govern use.
    /// - Skills: one unlock per specialized tree.
    ///
    /// This is the data model and enforcement API. Game-specific hooks
    /// (Harmony patches) are implemented separately and require runtime
    /// verification. Unsupported hooks fail visibly and remain excluded.
    /// </summary>
    public class PermissionManager
    {
        private readonly Action<string> logInfo;
        private readonly Action<string> logWarning;

        // Equipment: category -> max unlocked tier.
        // Tier 0 = locked; tier N = can use up to tier N.
        private readonly Dictionary<string, int> equipmentTiers =
            new Dictionary<string, int>();

        // Production: station/process ID -> unlocked.
        private readonly HashSet<string> productionUnlocks =
            new HashSet<string>();

        // Skills: tree ID -> unlocked.
        private readonly HashSet<string> skillTrees =
            new HashSet<string>();

        public PermissionManager(Action<string> logInfo, Action<string> logWarning)
        {
            this.logInfo = message => { try { logInfo?.Invoke(message); } catch { } };
            this.logWarning = message => { try { logWarning?.Invoke(message); } catch { } };
        }

        /// <summary>
        /// Grant an equipment tier unlock. Category is game-defined
        /// (e.g., "sword", "bow"); tier is 1-based.
        /// </summary>
        public void UnlockEquipmentTier(string category, int tier)
        {
            if (string.IsNullOrEmpty(category) || tier < 1)
            {
                logWarning("[AP] invalid equipment unlock: " + category + "/" + tier);
                return;
            }
            int current = 0;
            equipmentTiers.TryGetValue(category, out current);
            if (tier > current)
            {
                equipmentTiers[category] = tier;
                logInfo("[AP] equipment unlocked: " + category + " tier " + tier);
            }
        }

        /// <summary>
        /// Check if equipment can be used. Returns true if the category/tier
        /// is unlocked. Higher-tier items may be stored but not used.
        /// </summary>
        public bool CanUseEquipment(string category, int tier)
        {
            if (string.IsNullOrEmpty(category) || tier < 1)
                return false;
            int unlocked = 0;
            equipmentTiers.TryGetValue(category, out unlocked);
            return tier <= unlocked;
        }

        /// <summary>
        /// Grant a production unlock (station or process).
        /// </summary>
        public void UnlockProduction(string stationId)
        {
            if (string.IsNullOrEmpty(stationId))
                return;
            if (productionUnlocks.Add(stationId))
                logInfo("[AP] production unlocked: " + stationId);
        }

        /// <summary>
        /// Check if a production station/process can be used.
        /// </summary>
        public bool CanUseProduction(string stationId)
        {
            if (string.IsNullOrEmpty(stationId))
                return false;
            return productionUnlocks.Contains(stationId);
        }

        /// <summary>
        /// Check if a production group is unlocked.
        /// Group IDs: smelting, cooking, farming, livestock_fishing, alchemy,
        /// recycling, water_processing, equipment_care, automation, creature_combining.
        /// </summary>
        public bool IsProductionUnlocked(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
                return false;
            // Production items are "Production: <group_id>"
            return productionUnlocks.Contains("Production: " + groupId) ||
                   productionUnlocks.Contains(groupId);
        }

        /// <summary>
        /// Grant a skill tree unlock.
        /// </summary>
        public void UnlockSkillTree(string treeId)
        {
            if (string.IsNullOrEmpty(treeId))
                return;
            if (skillTrees.Add(treeId))
                logInfo("[AP] skill tree unlocked: " + treeId);
        }

        /// <summary>
        /// Check if a skill tree is unlocked (by tree ID string).
        /// </summary>
        public bool IsSkillTreeUnlocked(string treeId)
        {
            if (string.IsNullOrEmpty(treeId))
                return false;
            return skillTrees.Contains(treeId);
        }

        /// <summary>
        /// Check if a skill category+tip is unlocked.
        /// Category: e.g., "Combat", "Magic", "Life", "Trick", "Tamer", "Basic".
        /// Tier: 1-5.
        /// Basic is always unlocked. Combat T1 is a starter (unlocked at session start).
        /// </summary>
        public bool IsSkillTreeUnlocked(string category, int tier)
        {
            if (string.IsNullOrEmpty(category))
                return false;
            // Basic is always unlocked.
            if (category == "Basic")
                return true;
            // Combat T1 is a starter.
            if (category == "Combat" && tier <= 1)
                return true;
            // Check for category unlock.
            string categoryKey = "Skill Tree: " + category;
            if (skillTrees.Contains(categoryKey))
                return true;
            // Check for tier-specific unlock (Combat T2-T5).
            if (category == "Combat")
            {
                string tierKey = $"Skill Tree: Combat Tier {tier}";
                // Must have all tiers up to the requested tier.
                for (int t = 2; t <= tier; t++)
                {
                    if (!skillTrees.Contains($"Skill Tree: Combat Tier {t}"))
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a human-readable explanation of what's blocking an action.
        /// </summary>
        public string GetBlockExplanation(string actionType, string id)
        {
            switch (actionType)
            {
                case "equipment":
                    return "Equipment locked: unlock '" + id +
                        "' via Archipelago progression.";
                case "production":
                    return "Production locked: unlock '" + id +
                        "' via Archipelago progression.";
                case "skill":
                    return "Skill tree locked: unlock '" + id +
                        "' via Archipelago progression.";
                default:
                    return "Action blocked by Archipelago progression.";
            }
        }

        /// <summary>
        /// Clear all permissions (for session change or testing).
        /// </summary>
        public void Clear()
        {
            equipmentTiers.Clear();
            productionUnlocks.Clear();
            skillTrees.Clear();
            logInfo("[AP] permissions cleared");
        }

        // Read-only presentation of the same permissions used by enforcement.
        public List<string> GetGuideRows()
        {
            var rows = new List<string>();
            foreach (string category in new[] { "Sword", "Bow", "Shield", "Lance", "SwordTwoHand", "Katana", "Gun", "MagicStaff", "VacuumGun", "Head", "Body", "AssistEquip" }) {
                equipmentTiers.TryGetValue(category, out int tier);
                string name = category == "SwordTwoHand" ? "Two-handed weapon" : category == "MagicStaff" ? "Magic staff" : category == "VacuumGun" ? "Vacuum gun" : category == "Head" ? "Head armor" : category == "Body" ? "Body armor" : category == "AssistEquip" ? "Combat support" : category;
                rows.Add(name + "|" + (tier > 0 ? "Tier " + tier : "Locked"));
            }
            foreach (string group in new[] { "smelting", "cooking", "farming", "livestock_fishing", "alchemy", "recycling", "water_processing", "equipment_care", "automation", "creature_combining" }) {
                string name = char.ToUpperInvariant(group[0]) + group.Substring(1).Replace('_', ' ');
                rows.Add(name + "|" + (IsProductionUnlocked(group) ? "Unlocked" : "Locked"));
            }
            rows.Add("Basic skills|Available from the start");
            for (int tier = 1; tier <= 5; tier++)
                rows.Add("Combat skills " + tier + "|" + (IsSkillTreeUnlocked("Combat", tier) ? "Unlocked" : "Locked"));
            foreach (string tree in new[] { "Magic", "Life", "Trick", "Tamer" })
                rows.Add(tree + " skills|" + (IsSkillTreeUnlocked(tree, 1) ? "Unlocked" : "Locked"));
            return rows;
        }
    }
}
