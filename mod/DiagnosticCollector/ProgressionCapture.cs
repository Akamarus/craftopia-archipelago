using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CraftopiaDiagnostic
{
    internal static class ProgressionCapture
    {
        private static readonly string[] ItemFields = {
            "id", "status", "isOldData", "StatusMapOnly", "rank", "rarity", "familyId", "categoryId", "inventoryTabId", "existPrefab",
            "equipSlot", "wpCategory", "workbenchId", "workbenchLv", "playerCraftCount", "carftTimeCost",
            "materialA_Id", "materialA_count", "materialB_Id", "materialB_count", "materialC_Id", "materialC_count",
            "materialD_Id", "materialD_count", "materialE_Id", "materialE_count", "_materialID", "_materialNum",
            "<IsBlueprintRequired>k__BackingField", "atk", "matk", "def", "maxStack", "price", "cantUse"
        };

        // Both installed getters were inspected: they only return the static
        // backing field. No lazy creation or initialization is performed.
        private static object Instance(string typeName)
        {
            var type = typeof(Oc.OcPlMaster).Assembly.GetType(typeName, true);
            return type.GetProperty("Inst", BindingFlags.Static | BindingFlags.Public |
                BindingFlags.FlattenHierarchy)?.GetValue(null);
        }
        internal static void Collect(Dictionary<string, object> output)
        {
            output["progression_limitations"] = new[] {
                "Loaded data snapshot, not complete accessibility proof.",
                "Rank/rarity are raw data, not assigned progression tiers.",
                "Craft bench map keys are raw IDs; station mapping still needs confirmation.",
                "This progression subsection excludes character identities, credentials and inventory contents; world-object evidence is in world_evidence."
            };
            try
            {
                var manager = Instance("Oc.Item.OcItemDataMng");
                var catalog = CatalogFields.Field(manager, "SoItemDataList");
                var all = CatalogFields.Field(catalog, "all") as IEnumerable;
                var rows = new List<object>();
                if (all != null) foreach (var item in all)
                {
                    if (item == null) continue;
                    var row = CatalogFields.Read(item, ItemFields);
                    // Only this reviewed localization getter is permitted. It
                    // does not issue identities, load prefabs or change items.
                    try {
                        row["display_name"] = item.GetType().GetProperty("DisplayName")?.GetValue(item) as string;
                        row["display_name_status"] = row["display_name"] == null ? "unavailable" : "collected";
                    }
                    catch (Exception e) { row["display_name_status"] = "error:" + e.GetType().Name; }
                    rows.Add(row);
                }
                output["items"] = rows;
                output["items_status"] = all == null ? "catalog_unavailable" : rows.Count == 0 ? "empty" : "collected";
                output["craft_bench_map"] = IdMap(CatalogFields.Field(manager, "craftBenchMap"));
                output["craft_bench_map_status"] = CatalogFields.Field(manager, "craftBenchMap") is IDictionary ? "collected" : "unavailable";
                var ageItems = new List<object>();
                if (CatalogFields.Field(manager, "ageEvoNewItemLists") is IEnumerable ages)
                    foreach (var age in ages) ageItems.Add(ItemIds(age));
                output["age_new_items"] = ageItems;
                output["age_new_items_status"] = CatalogFields.Field(manager, "ageEvoNewItemLists") is IEnumerable ? "collected" : "unavailable";
            }
            catch (Exception e) { output["items_status"] = "error:" + e.GetType().Name; }
            try
            {
                var resident = Instance("Oc.OcResidentData");
                var ages = CatalogFields.Field(resident, "_ageEvoData") as IEnumerable;
                var rows = new List<object>();
                if (ages != null) foreach (var age in ages)
                {
                    if (age == null) continue;
                    var row = CatalogFields.Read(age, new[] { "worldLevel", "requireGold", "isEnabled" });
                    var materials = new List<object>();
                    if (CatalogFields.Field(age, "requireItems") is IEnumerable required)
                        foreach (var requirement in required)
                            materials.Add(new Dictionary<string, object> {
                                ["item_id"] = CatalogFields.Field(CatalogFields.Field(requirement, "item"), "id"),
                                ["count"] = CatalogFields.Field(requirement, "count")
                            });
                    row["requireItems"] = materials;
                    row["featuredItems"] = ItemIds(CatalogFields.Field(age, "featuredItems"));
                    rows.Add(row);
                }
                output["ages"] = rows;
                output["ages_status"] = ages == null ? "unavailable" : rows.Count == 0 ? "empty" : "collected";
            }
            catch (Exception e) { output["ages_status"] = "error:" + e.GetType().Name; }
            try
            {
                // Active, already loaded scene objects only. No prefab loading,
                // spawning, scene traversal into players, or inventory reads.
                var type = typeof(Oc.OcPlMaster).Assembly.GetType("Oc.OcBuildingBase_Craft", true);
                var rows = new List<object>();
                foreach (var station in UnityEngine.Object.FindObjectsOfType(type))
                    rows.Add(CatalogFields.StationIdentity(station));
                output["loaded_stations"] = rows;
                output["loaded_stations_status"] = rows.Count == 0 ? "none_loaded" : "collected";
                output["loaded_stations_limitations"] = "Active scene stations only; no locations, owners or inventory contents. Not a complete station catalog.";
            }
            catch (Exception e) { output["loaded_stations_status"] = "error:" + e.GetType().Name; }
        }
        private static List<object> ItemIds(object items)
        {
            var ids = new List<object>();
            if (items is IEnumerable sequence) foreach (var item in sequence) ids.Add(CatalogFields.Field(item, "id"));
            return ids;
        }
        private static List<object> IdMap(object value)
        {
            var rows = new List<object>();
            if (value is IDictionary map) foreach (DictionaryEntry pair in map)
                rows.Add(new Dictionary<string, object> { ["workbench_id"] = pair.Key, ["item_ids"] = ItemIds(pair.Value) });
            return rows;
        }
    }
}
