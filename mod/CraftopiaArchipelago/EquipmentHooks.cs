using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    internal static class EquipmentHooks
    {
        static EquipmentCatalog originalCatalog, reviewedCatalog, reviewedR2Catalog;
        static EquipmentCatalog catalog => Plugin.EquipmentCatalogIdentity == EquipmentCatalog.ReviewedLootR2CatalogId
            ? reviewedR2Catalog : Plugin.EquipmentCatalogIdentity == EquipmentCatalog.ReviewedLootCatalogId
            ? reviewedCatalog : originalCatalog;
        [ThreadStatic] static int craftActionDepth;
        static readonly System.Collections.Generic.HashSet<string> diagnostics = new System.Collections.Generic.HashSet<string>();
        internal static void Diagnostic(string message)
        {
            try {
                if (diagnostics.Contains(message)) return;
                if (diagnostics.Count >= 256) diagnostics.Clear();
                diagnostics.Add(message);
                Plugin.Log.LogWarning("[AP] " + message);
            } catch { }
        }
        internal static void Install(Harmony harmony)
        {
            if (typeof(Oc.OcPlMaster).Module.ModuleVersionId != new Guid("62ff4b5b-7684-4c41-b5f0-f44aa19360ca"))
                throw new InvalidOperationException("Equipment hook evidence does not match this game build");
            using (var stream = typeof(Plugin).Assembly.GetManifestResourceStream("equipment_catalog_20261010_r1.json"))
            using (var reader = new System.IO.StreamReader(stream)) {
                var json = reader.ReadToEnd();
                originalCatalog = EquipmentCatalog.Parse(json);
                reviewedCatalog = EquipmentCatalog.ParseReviewedLoot(json);
                reviewedR2Catalog = EquipmentCatalog.ParseReviewedLootR2(json);
            }
            var craftAction = typeof(Oc.OcPlMaster).Assembly.GetType("Oc.Item.UI.OcItemUI_CraftMng",true)
                .GetMethod("CraftButtonExecute",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            var begin = typeof(EquipmentHooks).GetMethod(nameof(BeginCraft),BindingFlags.Static|BindingFlags.NonPublic);
            var end = typeof(EquipmentHooks).GetMethod(nameof(EndCraft),BindingFlags.Static|BindingFlags.NonPublic);
            harmony.Patch(craftAction,prefix:new HarmonyMethod(begin),finalizer:new HarmonyMethod(end));
            bool beginFound=false,endFound=false;
            foreach(var p in Harmony.GetPatchInfo(craftAction).Prefixes) if(p.owner==Plugin.GUID && p.PatchMethod==begin) beginFound=true;
            foreach(var p in Harmony.GetPatchInfo(craftAction).Finalizers) if(p.owner==Plugin.GUID && p.PatchMethod==end) endFound=true;
            if(!beginFound || !endFound) throw new InvalidOperationException("Craft notice boundary not registered");
            Patch(harmony,"Oc.Item.UI.OcItemUI_CraftMng","CanCraft",typeof(bool),nameof(ItemBool),"Oc.Item.ItemData","System.Int32");
            InstallRecipeNotice(harmony,"RefreshInfo",nameof(RecipeSelected));
            InstallRecipeNotice(harmony,"OnCloseUI",nameof(ClearRecipe));
            InstallRecipeNotice(harmony,"Unselect",nameof(ClearRecipe));
            Patch(harmony,"Oc.Item.OcItem_CraftEnchantSelect","TryCraftItem",typeof(Oc.Item.OcItem),nameof(CraftItem),"Oc.Item.ItemData","System.Int32");
            Patch(harmony,"Oc.OcChestComponent","canAutoCraftNum",typeof(byte),nameof(AutoCraft),"Oc.Item.ItemData");
            Patch(harmony,"Oc.Item.UI.OcItemUI_EquipmentMng","CanEquipItem",typeof(bool),nameof(StackBool),"Oc.Item.OcItemStack","System.Boolean");
            Patch(harmony,"Oc.Item.UI.OcItemUI_EquipSlot","CanEquip",typeof(bool),nameof(StackBool),"Oc.Item.OcItemStack","System.Boolean");
            Patch(harmony,"Oc.Item.UI.OcItemUI_Equip_List","TryEquipItem",typeof(bool),nameof(StackBool),"Oc.Item.OcItemStack");
            Patch(harmony,"Oc.Item.UI.OcItemUI_EquipMyset+<>c__DisplayClass19_0","<ApplyMyset>b__0",typeof(void),nameof(Loadout),"Oc.OcUI_CommonDialogFolder+ButtonType");
        }
        static void Patch(Harmony harmony,string type,string name,Type result,string prefix,params string[] args)
        {
            var assembly=typeof(Oc.OcPlMaster).Assembly;
            var parameters=Array.ConvertAll(args,x => Type.GetType(x) ?? assembly.GetType(x,true));
            var target=assembly.GetType(type,true).GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,parameters,null);
            if (target == null || target.ReturnType != result) throw new MissingMethodException(type,name);
            var hook=typeof(EquipmentHooks).GetMethod(prefix,BindingFlags.Static|BindingFlags.NonPublic);
            harmony.Patch(target,prefix:new HarmonyMethod(hook));
            bool found=false;
            foreach(var patch in Harmony.GetPatchInfo(target).Prefixes)
                if(patch.owner==Plugin.GUID && patch.PatchMethod==hook) found=true;
            if(!found) throw new InvalidOperationException("Equipment hook registration failed");
            Plugin.Log.LogInfo("[AP] equipment hook verified: " + type + "." + name);
        }
        static int Age()
        {
            var world=Oc.Maps.OcWorldManager.Inst;
            if (world == null || world.CurrentWorld == null || Oc.OcGameSceneShareSingleton.IsOpening)
                throw new InvalidOperationException("World age unavailable");
            return world.WorldLevel;
        }
        static void InstallRecipeNotice(Harmony harmony,string name,string callback)
        {
            var target=typeof(Oc.OcPlMaster).Assembly.GetType("Oc.Item.UI.OcItemUI_CraftMng",true)
                .GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            var hook=typeof(EquipmentHooks).GetMethod(callback,BindingFlags.Static|BindingFlags.NonPublic);
            harmony.Patch(target,postfix:new HarmonyMethod(hook));
            foreach(var p in Harmony.GetPatchInfo(target).Postfixes)
                if(p.owner==Plugin.GUID && p.PatchMethod==hook) return;
            throw new InvalidOperationException("Recipe notice hook not registered: " + name);
        }
        static void ClearRecipe() { try { Plugin.StatusDisplay?.SetRecipeNotice(null); } catch { } }
        static void RecipeSelected(object __instance)
        {
            ClearRecipe();
            try {
                // Inspected getter reads selectingCell.stack.ItemData; no game mutation.
                var item=__instance.GetType().GetProperty("selectingItem",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(__instance);
                if(item==null || !Plugin.EquipmentRequired) return;
                EquipmentEnforcement.Allow(true,catalog,Plugin.EquipmentPermissions(),
                    () => EquipmentRead.Item(item),Age,message => Plugin.StatusDisplay?.SetRecipeNotice(message));
            } catch { } // Cosmetic observer cannot alter the original menu or enforcement.
        }
        static void BeginCraft(out bool __state) { __state=true; craftActionDepth++; }
        static void EndCraft(bool __state) { if(__state && craftActionDepth>0) craftActionDepth--; }
        static void ActionDiagnostic(string message) {
            Diagnostic(message);
            try { Plugin.StatusDisplay?.ShowBlockedAction(message); } catch { }
        }
        static bool Allow(Func<EquipmentSnapshot> read, bool action=false) => EquipmentEnforcement.Allow(
            Plugin.EquipmentRequired,catalog,Plugin.EquipmentPermissions(),read,Age,action ? ActionDiagnostic : Diagnostic);
        static bool ItemBool(object __0, ref bool __result)
        {
            if (Allow(() => EquipmentRead.Item(__0),craftActionDepth>0)) return true;
            __result=false; return false;
        }
        static bool StackBool(Oc.Item.OcItemStack __0, MethodBase __originalMethod, ref bool __result)
        {
            // These polymorphic getters resolve assigned stacks; inspected as read-only.
            if (Allow(() => EquipmentRead.Item(__0.item.data),__originalMethod.Name == "TryEquipItem")) return true;
            __result=false; return false;
        }
        static bool CraftItem(object __0, ref Oc.Item.OcItem __result)
        {
            if (Allow(() => EquipmentRead.Item(__0),true)) return true;
            __result=null; return false;
        }
        static bool AutoCraft(object __0, ref byte __result)
        {
            if (Allow(() => EquipmentRead.Item(__0))) return true;
            __result=0; return false;
        }
        static bool Loadout(object __instance, object __0)
        {
            if (!Plugin.EquipmentRequired) return true;
            try {
                if(Convert.ToInt32(__0)!=2) return true; // Cancel/close must remain usable.
                return EquipmentEnforcement.AllowLoadout(true,catalog,Plugin.EquipmentPermissions(),() => {
                    var sheet=EquipmentRead.Field(__instance,"myset");
                    var saved=EquipmentRead.Field(sheet,"savedata");
                    var manager=Oc.Item.OcItemDataMng.Inst;
                    return EquipmentRead.Loadout(EquipmentRead.Field(saved,"equips") as IEnumerable,
                        EquipmentRead.Field(manager,"validItemDataList") as IEnumerable);
                },Age,ActionDiagnostic);
            } catch { ActionDiagnostic("Equipment loadout blocked before transfer: unreadable context."); return false; }
        }
        internal static bool CurrentEquipmentAllowed()
        {
            if (!Plugin.EquipmentRequired) return true;
            return EquipmentEnforcement.AllowLoadout(true,catalog,Plugin.EquipmentPermissions(),() => {
                var items=EquipmentRead.Field(Oc.OcPlMng.LocalPl.EquipCtrl,"_EquipItem") as IEnumerable;
                if(items==null) throw new InvalidOperationException();
                var result=new System.Collections.Generic.List<EquipmentSnapshot>();
                foreach(Oc.Item.OcItem item in items) if(item!=null) result.Add(EquipmentRead.Item(item.data));
                return result;
            },Age,Diagnostic);
        }
    }
}
