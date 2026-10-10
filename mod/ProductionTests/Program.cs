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
        Assert(Group("Oc.OcBldg_CraftingBench") == "opening", "opening bench exact");
        Assert(Group("Oc.OcBldg_Automation") == "automation", "automation not free bench");
        Assert(Group("Oc.OcBldg_ItemGenerator_ThermalPowerPlant") == "excluded_power", "excluded power explicit");
        Console.WriteLine($"ProductionTests: {checks} assertions passed");
    }
}
