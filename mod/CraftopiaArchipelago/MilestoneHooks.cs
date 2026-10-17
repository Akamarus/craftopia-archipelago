using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    internal static class MilestoneHooks
    {
        internal static bool Installed { get; private set; }
        internal sealed class Snapshot { internal string Family; internal int Source,Before,Character; internal object World; internal long Epoch; }
        internal static void Install(Harmony harmony) {
            MilestoneSession.LoadCatalog();
            Patch(harmony,typeof(Oc.OcBldg_WorldHeritage),"TryProgressConstruction",Type.EmptyTypes,nameof(TowerBefore),nameof(TowerAfter));
            Patch(harmony,typeof(Oc.OcUI_AgeEvolution),"EvoAndCloseUI",Type.EmptyTypes,nameof(AgeBefore),nameof(AgeAfter));
            Patch(harmony,typeof(Oc.Dungeon.OcDungeonManager),"AddDungeonClear",new[]{typeof(int)},nameof(DungeonBefore),nameof(DungeonAfter));
            Patch(harmony,typeof(Oc.Missions.OcMissionManager),"TryTakeCategoryReward",new[]{typeof(int)},nameof(CategoryBefore),nameof(CategoryAfter));
            Installed=true;
        }
        static void Patch(Harmony h,Type type,string method,Type[] args,string pre,string post) {
            var target=AccessTools.Method(type,method,args);
            if(target==null) throw new MissingMethodException(type.FullName,method);
            h.Patch(target,new HarmonyMethod(typeof(MilestoneHooks),pre),new HarmonyMethod(typeof(MilestoneHooks),post));
            var info=Harmony.GetPatchInfo(target);
            if(!info.Prefixes.Any(p=>p.owner==h.Id && p.PatchMethod.Name==pre) || !info.Postfixes.Any(p=>p.owner==h.Id && p.PatchMethod.Name==post)) throw new InvalidOperationException("Milestone hook missing");
        }
        static Snapshot Start(string family,int source,int before) {
            if(!MilestoneSession.Ready) return null;
            return new Snapshot{Family=family,Source=source,Before=before,Character=Plugin.GetCharacterGuid(),World=Oc.Maps.OcWorldManager.Inst?.CurrentWorld,Epoch=MilestoneSession.Epoch};
        }
        static void Finish(Snapshot state,int after,bool success) {
            if(state==null) return;
            if(MilestoneDecision.Accept(state.Family,state.Before,after,success,Oc.OcGameSceneShareSingleton.IsOpening,
                MilestoneSession.SameWorld(state.World,state.Character,state.Epoch)))
                MilestoneSession.Complete(state.Family,state.Source,state.Family=="tower" || state.Family=="age" ? after : 0);
        }
        static int Stage(object tower) => MilestoneSession.Field(tower,"_UnderBuildingNum") is int n ? n : -1;
        static void TowerBefore(object __instance,out Snapshot __state) {
            __state=null;try {
                if(!(MilestoneSession.Field(__instance,"_IsTower") is bool t) || !t) return;
                var source=MilestoneSession.TowerSource(__instance);
                if(source is int id) __state=Start("tower",id,Stage(__instance));
            } catch { }
        }
        static void TowerAfter(object __instance,Snapshot __state) { try { Finish(__state,Stage(__instance),true); } catch { } }
        static void AgeBefore(out Snapshot __state) { __state=null;try { __state=Start("age",0,Oc.Maps.OcWorldManager.Inst.WorldLevel); } catch { } }
        static void AgeAfter(Snapshot __state) { try { Finish(__state,Oc.Maps.OcWorldManager.Inst.WorldLevel,true); } catch { } }
        static void DungeonBefore(Oc.Dungeon.OcDungeonManager __instance,int __0,out Snapshot __state) {
            __state=null;try { if((MilestoneSession.Field(__instance,"DungeonClearedList") as List<int>)!=null) __state=Start("dungeon",__0,(MilestoneSession.Field(__instance,"DungeonClearedList") as List<int>).Contains(__0)?1:0); } catch { }
        }
        static void DungeonAfter(Oc.Dungeon.OcDungeonManager __instance,int __0,Snapshot __state) { try { Finish(__state,(MilestoneSession.Field(__instance,"DungeonClearedList") as List<int>).Contains(__0)?1:0,true); } catch { } }
        static void CategoryBefore(Oc.Missions.OcMissionManager __instance,int __0,out Snapshot __state) {
            __state=null;try { if(__instance.IsCategoryCompelete(__0)) __state=Start("category",__0,__instance.IsCategoryRewardTaken(__0)?1:0); } catch { }
        }
        static void CategoryAfter(Oc.Missions.OcMissionManager __instance,int __0,bool __result,Snapshot __state) { try { Finish(__state,__instance.IsCategoryRewardTaken(__0)?1:0,__result); } catch { } }
    }
}
