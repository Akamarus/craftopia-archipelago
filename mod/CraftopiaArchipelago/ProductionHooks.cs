using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    internal static class ProductionHooks
    {
        // Installed-build IL proves these entries precede costs. In particular,
        // Campfire.OnButtonExecute clears the stack BEFORE StartProcessReq.
        internal static readonly string[] Targets = {
            "Oc.Item.UI.OcItemUI_CraftMng|CraftButtonExecute",
            "Oc.Item.UI.OcItemUI_CampfireMng|OnButtonExecute",
            "Oc.Item.UI.OcItemUI_AgingMng|TryEnchantItem",
            "Oc.Item.UI.OcItemUI_BreedPlantMng|TryBreed",
            "Oc.Item.UI.OcItemUI_DryingMng|TryEnchantItem",
            "Oc.Item.UI.OcItemUI_FilletMachineMng|TryFillet",
            "Oc.Item.UI.OcItemUI_RepairWorkshopMng|OnButtonExecute",
            "Oc.Item.UI.OcItemUI_SeedMakingMachineMng|OnButtonExecute",
            "Oc.Item.UI.OcItemUI_WeaponDisassemblyMng|TryDisassembly",
            "Oc.OcBldg_ItemGenerator_Alchemy|moveCoe",
            "Oc.OcBldg_ItemGenerator_CombinedEm|moveCoe",
            "Oc.OcBldg_ItemGenerator_CookingPot|moveCoe",
            "Oc.OcBldg_ItemGenerator_FishPool|moveCoe",
            "Oc.OcBldg_ItemGenerator_Refiner|moveCoe",
            "Oc.OcBldg_ItemGenerator_WashingMachine|moveCoe",
            "Oc.OcBldg_ItemGenerator_RecycleMachineBase|move",
            "Oc.OcBldg_ItemGenerator_StockFarm|TryStartProcessReq",
            "Oc.OcBldg_ItemGenerator_CrabPot|StartProcessReq",
            "Oc.OcBldg_ItemGenerator|StartProcessReq",
            "Oc.OcBldg_FarmBase|checkWatering",
            "Oc.OcBldg_Automation|Update",
            "Oc.OcAttach_CraftConnecter|Update"
        };
        private static readonly Dictionary<string, string> UiGroups = new Dictionary<string, string> {
            ["OcItemUI_CampfireMng"] = "cooking", ["OcItemUI_AgingMng"] = "equipment_care",
            ["OcItemUI_BreedPlantMng"] = "farming", ["OcItemUI_DryingMng"] = "water_processing",
            ["OcItemUI_FilletMachineMng"] = "livestock_fishing", ["OcItemUI_RepairWorkshopMng"] = "equipment_care",
            ["OcItemUI_SeedMakingMachineMng"] = "farming", ["OcItemUI_WeaponDisassemblyMng"] = "recycling"
        };
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static string lastMessage;

        internal static bool Install(Harmony harmony)
        {
            foreach (string target in Targets)
            {
                var parts = target.Split('|');
                var type = typeof(Oc.OcPlMaster).Assembly.GetType(parts[0], true);
                var method = type.GetMethod(parts[1], Flags | BindingFlags.DeclaredOnly);
                if (method == null || (method.ReturnType != typeof(void) && method.ReturnType != typeof(bool)))
                    throw new InvalidOperationException("Unsupported production boundary: " + target);
                bool connector = type.FullName == "Oc.OcAttach_CraftConnecter";
                var prefix = typeof(ProductionHooks).GetMethod(connector ? nameof(ConnectorTranspiler) : method.ReturnType == typeof(bool)
                    ? nameof(BoolPrefix) : nameof(VoidPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                if (connector) harmony.Patch(method, transpiler: new HarmonyMethod(prefix));
                else harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                var info = Harmony.GetPatchInfo(method);
                bool verified = false;
                if (info != null) foreach (var patch in connector ? info.Transpilers : info.Prefixes)
                    if (patch.owner == Plugin.GUID && patch.PatchMethod == prefix) verified = true;
                if (!verified) throw new InvalidOperationException("Unregistered production boundary: " + target);
                Plugin.Log.LogInfo("[AP] production hook verified: " + target);
            }
            return true;
        }

        private static object Field(object instance, string name)
        {
            for (Type t = instance?.GetType(); t != null; t = t.BaseType)
            {
                var field = t.GetField(name, Flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(instance);
            }
            throw new MissingFieldException(name);
        }

        private static string Group(object instance, MethodBase original)
        {
            if (UiGroups.TryGetValue(original.DeclaringType.Name, out var group)) return group;
            if (original.DeclaringType.FullName == "Oc.Item.UI.OcItemUI_CraftMng")
            {
                var bench = Field(instance, "_workBench");
                return bench == null ? "opening" : ProductionEnforcement.ResolveStation(bench.GetType().FullName);
            }
            if (original.DeclaringType.FullName == "Oc.OcAttach_CraftConnecter")
            {
                var station = Field(instance, "_ConnectBldg");
                // Destroyed Unity objects can retain a managed reference.
                return station is UnityEngine.Object obj && obj
                    ? ProductionEnforcement.ResolveStation(station.GetType().FullName) : null;
            }
            return ProductionEnforcement.ResolveStation(instance?.GetType().FullName);
        }

        private static IEnumerable<CodeInstruction> ConnectorTranspiler(IEnumerable<CodeInstruction> instructions)
            => ConnectorGuard.Rewrite(instructions, typeof(ProductionHooks).GetMethod(
                nameof(ConnectorCanCraft), BindingFlags.Static | BindingFlags.NonPublic));

        private static bool ConnectorCanCraft(object instance)
        {
            try { return VoidPrefix(instance, typeof(Oc.OcPlMaster).Assembly.GetType("Oc.OcAttach_CraftConnecter")
                .GetMethod("Update", Flags)); }
            catch { return !Plugin.IsSessionBound(); }
        }

        private static bool VoidPrefix(object __instance, MethodBase __originalMethod)
        {
            return ProductionEnforcement.CanStart(Plugin.IsSessionBound(), Plugin.Permissions,
                () => Group(__instance, __originalMethod), message => {
                    if (lastMessage == message) return;
                    lastMessage = message;
                    try { Plugin.Log?.LogInfo(message); } catch { }
                    try { Plugin.StatusDisplay?.ShowBlockedAction(message); } catch { }
                });
        }
        private static bool BoolPrefix(object __instance, MethodBase __originalMethod, ref bool __result)
        {
            if (VoidPrefix(__instance, __originalMethod)) return true;
            __result = false;
            return false;
        }
    }
}
