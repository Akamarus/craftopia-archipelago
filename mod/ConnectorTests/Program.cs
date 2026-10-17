using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Oc.Network { public class OcNetMng { public static bool get_IsHost() => true; } }
public class ConnectorFixture
{
    public float _SerachTimer;
    public bool unlocked;
    public int costs, discoveries;
    public static bool Allow(object value) => ((ConnectorFixture)value).unlocked;
    public static void Cost(ConnectorFixture value) => value.costs++;
    public static void Discover(ConnectorFixture value) { value.discoveries++; value.unlocked = true; }
}
class Program
{
    static void Main()
    {
        var type = Assembly.GetExecutingAssembly().GetType("CraftopiaArchipelago.ConnectorGuard");
        if (type == null) throw new Exception("Connector must gate crafting without skipping discovery");
        var rewrite = type.GetMethod("Rewrite");
        var dm = new DynamicMethod("ConnectorUpdate", typeof(void), new[] {typeof(ConnectorFixture)}, typeof(Program));
        var il = dm.GetILGenerator();
        var discovery = il.DefineLabel();
        var target = new CodeInstruction(OpCodes.Ldarg_0);
        target.labels.Add(discovery);
        var source = new List<CodeInstruction> {
            new CodeInstruction(OpCodes.Call, typeof(Oc.Network.OcNetMng).GetMethod("get_IsHost")),
            new CodeInstruction(OpCodes.Brfalse, discovery),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, typeof(ConnectorFixture).GetMethod("Cost")),
            target,
            new CodeInstruction(OpCodes.Ldfld, typeof(ConnectorFixture).GetField("_SerachTimer")),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, typeof(ConnectorFixture).GetMethod("Discover")),
            new CodeInstruction(OpCodes.Ret)
        };
        var result = ((IEnumerable<CodeInstruction>)rewrite.Invoke(null, new object[] {
            source, typeof(ConnectorFixture).GetMethod("Allow")})).ToList();
        foreach (var op in result)
        {
            foreach (var label in op.labels) il.MarkLabel(label);
            if (op.operand is Label l) il.Emit(op.opcode, l);
            else if (op.operand is MethodInfo m) il.Emit(op.opcode, m);
            else if (op.operand is FieldInfo f) il.Emit(op.opcode, f);
            else if (op.operand == null) il.Emit(op.opcode);
            else throw new Exception("Unexpected operand");
        }
        var update = (Action<ConnectorFixture>)dm.CreateDelegate(typeof(Action<ConnectorFixture>));
        var fixture = new ConnectorFixture();
        update(fixture);
        if (fixture.costs != 0 || fixture.discoveries != 1) throw new Exception("Locked target must preserve discovery without costs");
        update(fixture);
        if (fixture.costs != 1 || fixture.discoveries != 2) throw new Exception("New unlocked target must recover");
        try { rewrite.Invoke(null, new object[] {new List<CodeInstruction>(), typeof(ConnectorFixture).GetMethod("Allow")}); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        { Console.WriteLine("ConnectorTests: lifecycle, cost isolation and unsupported IL rejection passed"); return; }
        throw new Exception("Changed game IL must reject patch installation");
    }
}
