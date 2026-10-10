using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    // Call only BEFORE the original action consumes resources. Never gate a
    // late StartProcessReq: its callers may already have charged the player.
    public static class ProductionEnforcement
    {
        private static readonly Dictionary<string, string> Groups = new Dictionary<string, string>
        {
            ["Oc.OcBldg_CraftingBench"] = "opening",
            ["Oc.OcBldg_Automation"] = "automation",
            ["Oc.OcBldg_ItemGenerator_Refiner"] = "smelting",
            ["Oc.OcBldg_Campfire"] = "cooking",
            ["Oc.OcBldg_ItemGenerator_CookingPot"] = "cooking",
            ["Oc.OcBldg_Farm"] = "farming",
            ["Oc.OcBldg_WheatField"] = "farming",
            ["Oc.OcBldg_BreedPlant"] = "farming",
            ["Oc.OcBldg_SeedMakingMachine"] = "farming",
            ["Oc.OcBldg_ItemGenerator_StockFarm"] = "livestock_fishing",
            ["Oc.OcBldg_ItemGenerator_CrabPot"] = "livestock_fishing",
            ["Oc.OcBldg_ItemGenerator_FishPool"] = "livestock_fishing",
            ["Oc.OcBldg_FilletMachine"] = "livestock_fishing",
            ["Oc.OcBldg_ItemGenerator_Alchemy"] = "alchemy",
            ["Oc.OcBldg_ItemGenerator_RecycleMachine"] = "recycling",
            ["Oc.OcBldg_ItemGenerator_Composter"] = "recycling",
            ["Oc.OcBldg_WeaponDisassembly"] = "recycling",
            ["Oc.OcBldg_ItemGenerator_WaterPurification"] = "water_processing",
            ["Oc.OcBldg_ItemGenerator_WashingMachine"] = "water_processing",
            ["Oc.OcBldg_Dryer"] = "water_processing",
            ["Oc.OcBldg_RepairWorkshop"] = "equipment_care",
            ["Oc.OcBldg_Aging"] = "equipment_care",
            ["Oc.OcBldg_ItemGenerator_CombinedEm"] = "creature_combining",
            ["Oc.OcBldg_ItemGenerator_ThermalPowerPlant"] = "excluded_power"
        };

        public static string ResolveStation(string typeName)
            => typeName != null && Groups.TryGetValue(typeName, out var group) ? group : null;

        public static bool CanStart(bool bound, PermissionManager permissions,
            Func<string> resolveGroup, Action<string> diagnostic)
        {
            if (!bound) return true;
            string reason;
            try
            {
                string group = resolveGroup();
                if (group == "opening" || group == "excluded_power") return true;
                if (group != null && permissions != null && permissions.IsProductionUnlocked(group)) return true;
                reason = group == null ? "station identity unavailable" : "requires Production: " + group;
            }
            catch { reason = "permission evaluation failed"; }
            try { diagnostic?.Invoke("[AP] Production blocked before cost: " + reason); } catch { }
            return false;
        }
    }
}
