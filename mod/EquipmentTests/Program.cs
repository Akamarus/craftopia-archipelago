using System;
using System.IO;
using CraftopiaArchipelago;

class Program
{
    static int assertions;
    static void Check(bool ok, string name) { assertions++; if (!ok) throw new Exception(name); }
    static void Reject(Action action, string name)
    {
        try { action(); } catch (FormatException) { assertions++; return; }
        throw new Exception(name);
    }
    static int Main()
    {
        SessionTests.Run();
        var json = File.ReadAllText("worlds/craftopia/equipment_catalog_20261010_r1.json");
        var catalog = EquipmentCatalog.Parse(json);
        var reviewed = EquipmentCatalog.ParseReviewedLoot(json);
        var reviewedR2 = EquipmentCatalog.ParseReviewedLootR2(json);
        var bow = catalog.Snapshot(6187);
        var bowPermissions = new PermissionManager(null, null);
        Check(!EquipmentEnforcement.Allow(true, reviewedR2, bowPermissions, () => bow, () => 0, null), "r2 bow locked");
        bowPermissions.UnlockEquipmentTier("Sword", 2);
        Check(!EquipmentEnforcement.Allow(true, reviewedR2, bowPermissions, () => bow, () => 0, null), "sword ownership does not unlock bow");
        bowPermissions.UnlockEquipmentTier("Bow", 1);
        Check(!EquipmentEnforcement.Allow(true, reviewedR2, bowPermissions, () => bow, () => 0, null), "r2 bow needs tier two");
        bowPermissions.UnlockEquipmentTier("Bow", 2);
        Check(EquipmentEnforcement.Allow(true, reviewedR2, bowPermissions, () => bow, () => 0, null), "r2 bow tier two age zero");
        Check(!EquipmentEnforcement.Allow(true, reviewed, bowPermissions, () => bow, () => 9, null), "r1 bow remains unsupported");
        Check(!EquipmentEnforcement.Allow(true, catalog, bowPermissions, () => bow, () => 9, null), "base bow remains unsupported");
        bow.WeaponCategory = 5;
        Check(!EquipmentEnforcement.Allow(true, reviewedR2, bowPermissions, () => bow, () => 0, null), "changed bow metadata denied");
        Check(reviewedR2.Decision(95) == "progressive", "active Silver Bow preserved");
        Check(reviewedR2.Decision(6161) == "unsupported", "same-name legacy Silver Bow excluded");
        Check(reviewedR2.Decision(6188) == "unsupported", "higher goblin bow remains unsupported");
        Check(reviewedR2.Decision(6421) == "unsupported", "Wizard Staff deferred");
        foreach (int excluded in new[] { 1704,1711,1713,1716,1718,1722,1795,1803,1806,1810,1817,1824,1865,1866,1905,2511,2512,2517,2518,2539,2540,2541,2542,2552,2553,5501,5502,5505,5506,5515,5516,5518,6014,6016,6017,6018,6085,6161,6163,6166,6167,6169,6175,6267,6375,6414,6435,6468 })
            Check(reviewedR2.Decision(excluded) == "unsupported", "exact historical exclusion preserved " + excluded);
        var lootPermissions = new PermissionManager(null, null);
        var club = catalog.Snapshot(6031);
        Check(!EquipmentEnforcement.Allow(true, reviewed, lootPermissions, () => club, () => 0, null), "loot locked without sword");
        lootPermissions.UnlockEquipmentTier("Sword", 1);
        Check(!EquipmentEnforcement.Allow(true, reviewed, lootPermissions, () => club, () => 0, null), "loot requires sword two");
        lootPermissions.UnlockEquipmentTier("Sword", 2);
        Check(EquipmentEnforcement.Allow(true, reviewed, lootPermissions, () => club, () => 0, null), "reviewed club allows at tier two without artificial age");
        Check(EquipmentEnforcement.Allow(true, reviewedR2, lootPermissions, () => club, () => 0, null), "r2 preserves reviewed club");
        Check(!EquipmentEnforcement.Allow(true, catalog, lootPermissions, () => club, () => 0, null), "old catalog unchanged");
        Check(!EquipmentEnforcement.Allow(true, reviewed, lootPermissions, () => catalog.Snapshot(6032), () => 9, null), "club variants remain unreviewed");
        club.WorkbenchLevel = 1;
        Check(!EquipmentEnforcement.Allow(true, reviewed, lootPermissions, () => club, () => 0, null), "changed loot metadata denied");
        ReadTests.Run(catalog);
        Check(catalog.Count == 7052, "all observed positive IDs accounted for");
        var permissions = new PermissionManager(null, null);
        var stoneSword = catalog.Snapshot(44);
        var copperSword = catalog.Snapshot(45);
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => stoneSword, () => 0, null), "locked sword denied");
        permissions.UnlockEquipmentTier("Sword", 1);
        Check(EquipmentEnforcement.Allow(true, catalog, permissions, () => stoneSword, () => 0, null), "starter sword permitted");
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => copperSword, () => 1, null), "higher tier denied");
        permissions.UnlockEquipmentTier("Sword", 2);
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => copperSword, () => 0, null), "AP does not bypass age");
        Check(EquipmentEnforcement.Allow(true, catalog, permissions, () => copperSword, () => 1, null), "tier plus age permits original");
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => catalog.Snapshot(93), () => 1, null), "bow independent");
        Check(catalog.Decision(3270) == "unsupported", "combat gloves are not tools");
        Check(EquipmentEnforcement.Allow(false, null, null, () => throw new Exception(), () => throw new Exception(), null), "disabled never evaluates game state");
        Check(!EquipmentEnforcement.Allow(true, null, permissions, () => stoneSword, () => 0, null), "missing catalog denies");
        Check(!EquipmentEnforcement.Allow(true, catalog, null, () => stoneSword, () => 0, null), "missing permissions denies combat");
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => null, () => 0, null), "missing item denies");
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => throw new Exception(), () => 0, _ => throw new Exception()), "read/log exceptions deny");
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => copperSword, () => throw new Exception(), null), "unknown age denies combat");
        var changed = catalog.Snapshot(44);
        changed.WorkbenchLevel = 9;
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => changed, () => 9, null), "live metadata mismatch denies");
        Check(EquipmentEnforcement.Allow(true, catalog, permissions, () => stoneSword, () => 0, null), "snapshot cannot mutate catalog");
        changed = catalog.Snapshot(44); changed.Id = 32767;
        Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => changed, () => 9, null), "unknown ID denies");
        Check(!EquipmentEnforcement.AllowLoadout(true, catalog, permissions,
            () => new[] { stoneSword, catalog.Snapshot(93) }, () => 1, null), "one locked item denies entire loadout");
        Check(EquipmentEnforcement.AllowLoadout(true, catalog, permissions,
            () => new[] { stoneSword, copperSword }, () => 1, null), "fully owned loadout permits original");
        Check(!EquipmentEnforcement.AllowLoadout(true, catalog, permissions,
            () => null, () => 1, null), "unreadable loadout denies");
        Check(EquipmentEnforcement.AllowLoadout(true, catalog, permissions,
            () => Array.Empty<EquipmentSnapshot>(), () => 1, null), "empty loadout allows removal");
        Check(!EquipmentEnforcement.AllowLoadout(true, catalog, permissions,
            () => throw new Exception(), () => 1, _ => throw new Exception()), "loadout/log exception denies");
        int progressive = 0, unsupported = 0, vanilla = 0;
        for (int id = 1; id < 32767; id++)
        {
            var row = catalog.Snapshot(id);
            if (row == null) continue;
            var mode = catalog.Decision(id);
            if (mode == "progressive") progressive++;
            else if (mode == "unsupported")
            {
                unsupported++;
                Check(!EquipmentEnforcement.Allow(true, catalog, permissions, () => row, () => 99, null), "unsupported never inferred");
            }
            else
            {
                vanilla++;
                Check(EquipmentEnforcement.Allow(true, catalog, null, () => row, () => throw new Exception(), null), "non-gated item retains vanilla");
            }
        }
        Reject(() => EquipmentCatalog.Parse(json.Replace("equipment_20261010_r1", "unknown")), "catalog mismatch");
        Reject(() => EquipmentCatalog.Parse(json.Replace("\"schema_version\": 1", "\"schema_version\": true")), "boolean schema");
        Reject(() => EquipmentCatalog.Parse(json.Replace("\"tier\":1", "\"tier\":99")), "invalid derived tier");
        Reject(() => EquipmentCatalog.Parse("{}"), "missing fields");
        Reject(() => EquipmentCatalog.Parse(json.Replace("\"id\":45,", "\"id\":44,")), "duplicate IDs");
        Reject(() => EquipmentCatalog.Parse(json.Replace("\"wpCategory\":0,", "\"wpCategory\":999,")), "unknown enum must not become vanilla");
        Console.WriteLine($"PASS {assertions} assertions; {progressive} progressive, {unsupported} unsupported, {vanilla} vanilla");
        return 0;
    }
}
