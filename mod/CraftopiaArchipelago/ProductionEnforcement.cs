using System;
using System.Collections.Generic;
using System.Reflection;

namespace CraftopiaArchipelago
{
    // Call only BEFORE the original action consumes resources. Never gate a
    // late StartProcessReq: its callers may already have charged the player.
    public static class ProductionEnforcement
    {
        private static readonly Dictionary<string, string> Groups = new Dictionary<string, string>
        {
            ["Oc.OcBldg_Automation"] = "automation",
            ["Oc.OcBldg_ItemGenerator_Refiner"] = "equipment_care",
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

        public static string ResolveStationObject(object station)
        {
            string type = station?.GetType().FullName;
            if (type == "Oc.OcBldg_ItemGenerator")
            {
                // F9 2026-10-18: Generator (Small) is generic, as is Well.
                // Exempt only the observed item/family pair, never the whole type.
                var generatorItem = Field(station, "<_BldgItem_InItemSlot>k__BackingField");
                var generatorData = Field(generatorItem, "<data>k__BackingField");
                return Field(generatorData, "id") is int id && id == 2000 &&
                    Field(generatorData, "familyId") is int familyId && familyId == 20401
                    ? "excluded_power" : null;
            }
            if (type != "Oc.OcBldg_CraftingBench") return ResolveStation(type);
            // Runtime capture 2026-10-10: one class backs distinct stations.
            // Read the same item-data family used by the game's recipe lookup.
            // Explicit audited recipe families only; no name guessing.
            var item = Field(station, "<_BldgItem_InItemSlot>k__BackingField");
            var data = Field(item, "<data>k__BackingField");
            var rawFamily = Field(data, "familyId");
            if (!(rawFamily is int family)) return null;
            switch (family)
            {
                case 20011: return "opening";
                // Capture 2026-10-14: Blacksmith (item 272). Recipe benches
                // retain vanilla costs/age checks and independent equipment gates.
                case 20017: return "opening";
                // Installed recipe-family audit 2026-10-16. Manual component
                // and furniture recipes retain native age/material checks;
                // automation is gated separately by the automation hook.
                case 20013: return "opening";
                case 20020: return "opening";
                case 20012: return "alchemy";
                case 20018: return "smelting";
                case 20015: return "cooking";
                // F9 2026-10-16: Aging Facility item 416 uses the generic
                // bench and family 20206. Match the existing Aging UI gate.
                case 20206: return "equipment_care";
                default: return null;
            }
        }

        private static object Field(object instance, string name)
        {
            for (Type type = instance?.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(instance);
            }
            return null;
        }

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
