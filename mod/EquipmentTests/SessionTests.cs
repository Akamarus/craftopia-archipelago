using System;
using CraftopiaArchipelago;

static class SessionTests
{
    public static void Run()
    {
        void Check(bool value) { if (!value) throw new Exception("Equipment session regression"); }
        var legacy = new EquipmentSession(false, 0, 0, "slot");
        Check(legacy.Bind(50, "slot", null, 0, 0));
        Check(!legacy.Required);
        var active = new EquipmentSession(true, -123, 50, "slot");
        var reviewed = new EquipmentSession(true, -123, 50, "slot");
        var reviewedR2 = new EquipmentSession(true, -123, 50, "slot");
        Check(reviewedR2.Bind(50, "slot", "equipment_loot_20261016_r2", 2, 0));
        Check(!reviewedR2.Bind(50, "slot", "equipment_loot_20261016_r1", 2, 0));
        Check(!new EquipmentSession(true, -123, 50, "slot").Bind(50, "slot", "equipment_loot_20261016_r2", 1, 0));
        Check(reviewed.Bind(50, "slot", "equipment_loot_20261016_r1", 2, 0));
        Check(reviewed.Catalog == "equipment_loot_20261016_r1");
        Check(!reviewed.Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 0));
        Check(!new EquipmentSession(true, -123, 50, "slot").Bind(50, "slot", "equipment_loot_20261016_r1", 1, 0));
        Check(active.Required && !active.Ready(-123, 50, "slot", true));
        Check(active.Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 0));
        Check(active.Ready(-123, 50, "slot", true));
        Check(!active.Ready(123, 50, "slot", true));
        Check(!active.Admit(active.Ready(123,50,"slot",true),()=>true));
        Check(active.Admit(active.Ready(-123,50,"slot",true),()=>true));
        Check(!active.Admit(true,()=>false));
        Check(!active.Admit(true,()=>throw new System.Exception()));
        Check(!active.Ready(-123, 50, "slot", false));
        Check(!active.Ready(-123, 51, "slot", true));
        Check(active.Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 0));
        Check(active.Ready(-123, 50, "slot", true));
        Check(!active.Bind(50, "slot", null, 0, 0));
        Check(active.Required && !active.Ready(-123, 50, "slot", true));
        Check(!active.Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 0));
        Check(!legacy.Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 0));
        Check(legacy.Required); // A rejected request must not become vanilla.
        foreach (var other in new[] { "wrong", "" })
            Check(!new EquipmentSession(true, -123, 50, "slot").Bind(50, "slot", other, 1, 0));
        Check(!new EquipmentSession(true, -123, 50, "slot").Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 1));
        Check(!new EquipmentSession(true, 0, 50, "slot").Bind(50, "slot", EquipmentCatalog.CatalogId, 1, 0));
        Console.WriteLine("PASS equipment session contracts");
    }
}
