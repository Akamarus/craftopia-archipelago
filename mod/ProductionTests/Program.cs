using System;
using System.Reflection;
using CraftopiaArchipelago;

class Program
{
    static int checks;
    static void Assert(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
    static void Main()
    {
        var type = typeof(PermissionManager).Assembly.GetType("CraftopiaArchipelago.ProductionEnforcement");
        Assert(type != null, "Production enforcement must exist as a tested pre-cost decision");
        var method = type.GetMethod("CanStart");
        bool Allow(bool bound, PermissionManager permissions, Func<string> resolve,
            Action<string> log = null) => (bool)method.Invoke(null, new object[] {bound, permissions, resolve, log});
        var p = new PermissionManager(null, null);
        Assert(Allow(false, null, () => throw new Exception()), "unbound preserves vanilla");
        Assert(!Allow(true, null, () => "cooking"), "missing permissions deny");
        Assert(!Allow(true, p, () => null), "unknown station deny");
        Assert(!Allow(true, p, () => throw new Exception()), "resolver failure deny");
        Assert(!Allow(true, p, () => "cooking", _ => throw new Exception()), "diagnostic failure cannot bypass");
        Assert(Allow(true, p, () => "opening"), "opening crafting remains free");
        Assert(Allow(true, p, () => "excluded_power"), "explicitly excluded power remains vanilla");
        int materials = 18, products = 0;
        if (Allow(true, p, () => "cooking")) { materials -= 3; products++; }
        Assert(materials == 18 && products == 0, "locked pre-cost action must not consume or produce");
        p.UnlockProduction("Production: cooking");
        if (Allow(true, p, () => "cooking")) { materials -= 3; products++; }
        Assert(materials == 15 && products == 1, "owned action runs vanilla cost exactly once");
        Assert(!Allow(true, p, () => "smelting"), "ownership is group scoped");
        Assert(Allow(true, p, () => "cooking"), "bound permissions survive transport disconnect/hold");
        p.Clear();
        Assert(!Allow(true, p, () => "cooking"), "new session loses old ownership");
        var resolve = type.GetMethod("ResolveStation");
        string Group(string name) => (string)resolve.Invoke(null, new object[] {name});
        Assert(Group("Oc.OcBldg_Campfire") == "cooking", "exact campfire type");
        Assert(Group("Other.FakeRefiner") == null, "substring impostor cannot acquire permissions");
        Assert(Group("Oc.OcBldg_CraftingBench") == null, "generic bench requires family evidence");
        Assert(Group("Oc.OcBldg_ItemGenerator_Refiner") == "equipment_care", "refiner is weapon refinement, not smelting");
        var objectResolver = type.GetMethod("ResolveStationObject");
        Assert(objectResolver != null, "station object resolver required");
        string Station(object value) => (string)objectResolver.Invoke(null, new[] { value });
        Assert(Allow(true, p, () => Station(new Oc.OcBldg_ItemGenerator(2000, 20401))), "small generator runs without AP production unlock");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_ItemGenerator(332, 20101))), "well sharing generic machine type is not exempt");
        Assert(Station(new Oc.OcBldg_ItemGenerator(9999, 20401)) == null, "same family alone cannot bypass production");
        Assert(Station(new Oc.OcBldg_ItemGenerator(2000, 9999)) == null, "generator requires matching family evidence");
        Assert(Station(new Oc.OcBldg_ItemGenerator(null, null)) == null, "missing generator data stays denied");
        Assert(Station(new Oc.OcBldg_CraftingBench(20012)) == "alchemy", "audited potion family requires alchemy");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20012))), "locked potion bench denies");
        p.UnlockProduction("Production: cooking");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20012))), "cooking cannot unlock potion bench");
        p.UnlockProduction("Production: alchemy");
        Assert(Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20012))), "alchemy permits potion bench");
        p.Clear();
        foreach (int family in new[] { 20013, 20020 })
            Assert(Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(family))), "audited manual recipe family retains vanilla checks");
        Assert(Station(new Oc.OcBldg_CraftingBench(20011)) == "opening", "observed opening workbench");
        Assert(Station(new Oc.OcBldg_CraftingBench(20018)) == "smelting", "observed stone furnace");
        Assert(Station(new Oc.OcBldg_CraftingBench(20015)) == "cooking", "observed cooking pot");
        Assert(Station(new Oc.OcBldg_CraftingBench(20206)) == "equipment_care", "observed Aging Facility uses generic bench family 20206");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20206))), "aging remains locked without Equipment Care");
        p.UnlockProduction("Production: cooking");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20206))), "Cooking does not bypass Aging Facility gate");
        p.Clear();
        p.UnlockProduction("Production: equipment_care");
        Assert(Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20206))), "Equipment Care permits Aging Facility before vanilla costs");
        p.Clear();
        Assert(Station(new Oc.OcBldg_CraftingBench(20017)) == "opening", "observed Blacksmith family is a recipe bench");
        Assert(Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20017))),
            "Blacksmith station does not require an unrelated production unlock; item gates still run separately");
        Assert(Station(new Oc.OcBldg_CraftingBench(999)) == null, "unknown bench family cannot become free");
        Assert(Station(new Oc.OcBldg_CraftingBench(null)) == null, "missing item data cannot become free");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20018))), "locked furnace denied");
        p.UnlockProduction("Production: smelting");
        Assert(Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20018))), "owned furnace allowed");
        Assert(!Allow(true, p, () => Station(new Oc.OcBldg_CraftingBench(20015))), "smelting does not unlock cooking pot");
        Assert(Group("Oc.OcBldg_Automation") == "automation", "automation not free bench");
        Assert(Group("Oc.OcBldg_ItemGenerator_ThermalPowerPlant") == "excluded_power", "excluded power explicit");
        Console.WriteLine($"ProductionTests: {checks} assertions passed");
    }
}

namespace Oc {
    class StationData { public int familyId; public int id; public StationData(int value, int itemId = 0) { familyId = value; id = itemId; } }
    class StationItem { public StationData data { get; } public StationItem(int value, int itemId = 0) { data = new StationData(value, itemId); } }
    class StationBase { public StationItem _BldgItem_InItemSlot { get; } public StationBase(int? family) { _BldgItem_InItemSlot = family.HasValue ? new StationItem(family.Value) : null; } }
    class OcBldg_ItemGenerator { public StationItem _BldgItem_InItemSlot { get; } public OcBldg_ItemGenerator(int? id, int? family) { _BldgItem_InItemSlot = id.HasValue && family.HasValue ? new StationItem(family.Value, id.Value) : null; } }
    class OcBldg_CraftingBench : StationBase { public OcBldg_CraftingBench(int? family) : base(family) { } }
}
