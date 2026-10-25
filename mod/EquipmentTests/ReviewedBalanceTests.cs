using System;
using System.IO;
using System.Collections.Generic;
using CraftopiaArchipelago;

static class ReviewedBalanceTests
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run(string original)
    {
        var overlay = File.ReadAllText("worlds/craftopia/equipment_reviewed_20261024_r1.json");
        var catalog = EquipmentCatalog.ParseReviewedBalance(original, overlay);
        var old = EquipmentCatalog.ParseReviewedLootR2(original);
        var root = MiniJson.ParseObject(overlay);
        var rows = (List<object>)root["items"];
        Check(rows.Count == 206, "206 eligible assignments");
        foreach (Dictionary<string, object> row in rows)
        {
            int id = (int)(long)row["id"], tier = (int)(long)row["tier"], age = (int)(long)row["minimum_age"];
            string category = (string)row["category"];
            var item = catalog.Snapshot(id);
            var p = new PermissionManager(null, null);
            if (tier > 1) p.UnlockEquipmentTier(category, tier - 1);
            Check(!EquipmentEnforcement.Allow(true, catalog, p, () => item, () => 9, null), "lower tier denied " + id);
            p.UnlockEquipmentTier(category, tier);
            Check(EquipmentEnforcement.Allow(true, catalog, p, () => item, () => age, null), "exact tier/age allows " + id);
            if (age > 0) Check(!EquipmentEnforcement.Allow(true, catalog, p, () => item, () => age - 1, null), "native age retained " + id);
            Check(old.Decision(id) == "unsupported" || id == 6031 || id == 6187, "old catalog unchanged " + id);
            item.WeaponCategory = -1;
            Check(!EquipmentEnforcement.Allow(true, catalog, p, () => item, () => 9, null), "changed metadata denied " + id);
        }
        foreach (int id in new[] {3300,3301,3302,3303,6375,6402,6414,6435,6468,6161})
            Check(catalog.Decision(id) == "unsupported", "legacy excluded " + id);
        var s = new EquipmentSession(true, 123, 50, "slot");
        Check(!new EquipmentSession(true,123,50,"slot").Bind(50,"slot","equipment_reviewed_20261024_r1",2,0), "old capability rejected");
        Check(s.Bind(50,"slot","equipment_reviewed_20261024_r1",3,0), "new contract accepted");
        Check(!s.Bind(50,"slot",EquipmentCatalog.ReviewedLootR2CatalogId,2,0), "cannot switch catalog within seed");
        Console.WriteLine("PASS reviewed equipment: 206 tier/age/metadata boundaries and frozen catalogs");
    }
}
