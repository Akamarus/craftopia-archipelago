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
