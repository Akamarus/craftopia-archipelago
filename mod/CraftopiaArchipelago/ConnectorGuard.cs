using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    public static class ConnectorGuard
    {
        // Join the existing non-host path, which performs discovery and cleanup.
        // Reject an unrecognized layout instead of installing a partial guard.
        public static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> input, MethodInfo allow)
        {
            var code = input.ToList();
            int host = -1;
            for (int i = 0; i + 1 < code.Count; i++)
                if (code[i].operand is MethodInfo method && method.Name == "get_IsHost"
                    && method.DeclaringType.FullName == "Oc.Network.OcNetMng")
                {
                    if (host >= 0) throw new InvalidOperationException("Ambiguous connector host boundary");
                    host = i;
                }
            if (host < 0 || code[host + 1].opcode != OpCodes.Brfalse
                || code[host + 1].operand?.GetType().FullName != "System.Reflection.Emit.Label")
                throw new InvalidOperationException("Unsupported connector host boundary");
            object discovery = code[host + 1].operand;
            // HarmonyX's net-framework Label signature cannot be referenced directly
            // by netstandard2.1. Preserve its boxed labels without converting them.
            var labels = typeof(CodeInstruction).GetField("labels");
            int target = code.FindIndex(op => ((System.Collections.IEnumerable)labels.GetValue(op))
                .Cast<object>().Any(label => label.Equals(discovery)));
            if (target <= host + 1 || !code.Skip(target).Take(3).Any(op =>
                op.opcode == OpCodes.Ldfld && op.operand is FieldInfo field && field.Name == "_SerachTimer"))
                throw new InvalidOperationException("Unsupported connector discovery boundary");
            if (allow == null || allow.ReturnType != typeof(bool) || !allow.IsStatic
                || allow.GetParameters().Length != 1 || allow.GetParameters()[0].ParameterType != typeof(object))
                throw new InvalidOperationException("Invalid connector predicate");
            code.InsertRange(host + 2, new[] {
                new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, allow),
                new CodeInstruction(OpCodes.Brfalse, discovery)
            });
            return code;
        }
    }
}
