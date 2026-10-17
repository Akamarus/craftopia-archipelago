using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace CraftopiaDiagnostic
{
    internal sealed class ObservationHooks
    {
        internal sealed class Spec
        {
            internal string Type, Method;
            internal string[] Parameters, Fields;
            internal Spec(string type, string method, string[] parameters, string[] fields)
            { Type=type; Method=method; Parameters=parameters; Fields=fields; }
        }
        internal static readonly Spec[] Specs = {
            new Spec("Oc.OcSoulOrb_Spawner", "SetTaken", new string[0], WorldEvidenceCapture.OrbFields),
            new Spec("Oc.OcObj_RandomTreasure", "Pickup", new string[0], WorldEvidenceCapture.RandomTreasureFields),
            new Spec("Oc.OcObj_RandomTreasure", "Open_ForRpc", new[]{"System.Int32"}, WorldEvidenceCapture.RandomTreasureFields),
            new Spec("Oc.OcGimmick_TreasureBoxBase", "doComplete", new string[0], WorldEvidenceCapture.TreasureFields),
            new Spec("Oc.Maps.OcOWTreasureListHandler", "OnOpenTreasure", new[]{"System.Int32","System.Int64"}, new string[0]),
            new Spec("Oc.SoChallengeData", "GetReward", new string[0], new[]{"id","devName","_itemInfos","_money","_exp"}),
            new Spec("Oc.Quest.SoQuestData", "GetReward", new string[0], new[]{"questID","groupID","_itemInfos","_money","_exp"}),
            new Spec("Oc.Quest.OcQuestMng", "QuestClear", new[]{"System.Int32"}, new[]{"_lastClearQuest"}),
            new Spec("Oc.OcBldg_WorldHeritage", "TrySetProgress", new[]{"System.Int32","System.Boolean"}, new string[0]),
            new Spec("Oc.OcBldg_WorldHeritage", "TryProgressConstruction", new string[0], new string[0]),
            new Spec("Oc.OcBldg_WorldHeritage", "changeUnderBuilding", new[]{"System.Boolean"}, new string[0]),
            new Spec("Oc.OcUI_AgeEvolution", "EvoAndCloseUI", new string[0], new[]{"selectLevelIndex"}),
            new Spec("Oc.Maps.OcWorldManager", "AddWorldLevel", new[]{"System.Int32"}, new string[0]),
            new Spec("Oc.Maps.OcWorldManager", "SetWorldLevel", new[]{"System.Int32"}, new string[0]),
            new Spec("Oc.Dungeon.OcDungeonManager", "AddDungeonClear", new[]{"System.Int32"}, new[]{"DungeonClearedList"}),
            new Spec("Oc.Item.UI.OcUI_ShopUI", "TryPurchase", new string[0], new[]{"_buyNumber"}),
            new Spec("Oc.Item.UI.OcUI_SoulOrbUI", "StatusUpConfirm", new string[0], new[]{"_selectedButtonId","_exchangeCount"}),
            new Spec("Oc.Item.UI.OcUI_StatusUpPageSoulOrb", "OnExchangeButtonPressed", new string[0], new[]{"_exchangeCount","_lastExchange","_totalRewardCount"}),
            new Spec("Oc.Missions.OcMissionManager", "TryTakeCategoryReward", new[]{"System.Int32"}, new[]{"categoryTookList"}),
            new Spec("Oc.Em.OcEm", "TryAddKillCount", new string[0], WorldEvidenceCapture.EnemyFields),
            new Spec("Oc.Pet.OcPetMng", "NewPet", new[]{"Oc.OcEmType","System.Int32"}, new string[0]),
            new Spec("Oc.Missions.Mission", "TryGetReward", new string[0], new[]{"id","achievementID","worldLevel","targetValue"})
        };
        private static ObservationHooks active;
        private readonly ProgressionObservation progression=new ProgressionObservation();
        private object selectedWorld;
        private long worldGeneration;
        private DateTime nextPoll;
        private readonly Dictionary<int,UnityEngine.Component> trackedTowers = new Dictionary<int,UnityEngine.Component>();
        private void TrackTower(object value)
        {
            if(value is UnityEngine.Component component && component != null &&
                CatalogFields.Field(value,"_IsTower") is bool tower && tower) {
                int id=component.GetInstanceID();
                if(trackedTowers.ContainsKey(id) || trackedTowers.Count<128) trackedTowers[id]=component;
            }
        }
        internal void PollProgression()
        {
            if(DateTime.UtcNow<nextPoll || total>=10000 || writtenBytes>=64*1024*1024) return;
            nextPoll=DateTime.UtcNow.AddSeconds(0.5);
            try {
                var world=WorldEvidenceCapture.FindWorld();
                var selected=CatalogFields.Field(world,"_selectedWorld");
                if(!ReferenceEquals(selected,selectedWorld)) { selectedWorld=selected; worldGeneration++; }
                if(selected==null) { progression.Observe(null,0,null); return; }
                var context=worldGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var seen=new HashSet<int>();
                var removed=new List<int>();
                foreach(var pair in trackedTowers) {
                    var value=pair.Value;
                    if(value==null) { removed.Add(pair.Key); continue; }
                    if(seen.Count>=128) break;
                    if(!(value is UnityEngine.Component component) || !(CatalogFields.Field(value,"_IsTower") is bool tower) || !tower) continue;
                    int instance=component.GetInstanceID(); seen.Add(instance);
                    var raw=CatalogFields.Field(value,"_UnderBuildingNum");
                    var delta=progression.Observe(context,instance,raw is int stage ? (int?)stage : null);
                    if(delta==null) continue;
                    if(total>=10000) break;
                    delta["sequence"]=++sequence; delta["utc"]=DateTime.UtcNow.ToString("o");
                    delta["method"]="diagnostic::tower_stage_sample"; delta["tower"]=ProgressionEvidence.Tower(value);
                    delta["world_context"]=WorldEvidenceCapture.WorldContext(world); delta["capture_id"]=Capture;
                    buffer.Add(delta); total++;
                }
                progression.Retain(seen);
                foreach(var id in removed) trackedTowers.Remove(id);
            } catch { progression.Observe(null,0,null); }
        }
        private readonly Harmony harmony = new Harmony("com.craftopia.diagnostic.observers");
        private readonly Dictionary<MethodBase, Spec> installed = new Dictionary<MethodBase, Spec>();
        internal readonly List<object> HookStatus = new List<object>();
        private readonly ObservationBuffer buffer = new ObservationBuffer(256);
        internal readonly string Session = Guid.NewGuid().ToString("N");
        private readonly string directory;
        private readonly Action<string> log;
        private readonly int mainThread = Thread.CurrentThread.ManagedThreadId;
        private long sequence, writtenBytes, total, ignoredThread;
        private bool writeFailureLogged;
        internal string GameHash, CollectorHash;
        internal object World;
        internal DateTime WorldObservedAt;
        internal string Capture;
        internal ObservationHooks(string directory, Action<string> log) { this.directory=directory; this.log=log; }
        internal void Install()
        {
            active=this;
            foreach (var spec in Specs)
            {
                var status = new Dictionary<string, object> { ["type"]=spec.Type, ["method"]=spec.Method, ["parameters"]=spec.Parameters };
                try
                {
                    var type = typeof(Oc.OcPlMaster).Assembly.GetType(spec.Type, true);
                    MethodInfo found=null;
                    foreach (var m in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                    {
                        if (m.Name != spec.Method || m.GetParameters().Length != spec.Parameters.Length) continue;
                        bool match=true; var ps=m.GetParameters();
                        for(int i=0;i<ps.Length;i++) if(ps[i].ParameterType.FullName!=spec.Parameters[i]) match=false;
                        if(match) { if(found!=null) throw new AmbiguousMatchException(); found=m; }
                    }
                    if(found==null) throw new MissingMethodException();
                    installed[found]=spec;
                    harmony.Patch(found, prefix:new HarmonyMethod(typeof(ObservationHooks), nameof(Before)),
                        postfix:new HarmonyMethod(typeof(ObservationHooks), spec.Method=="TryTakeCategoryReward" ? nameof(AfterCategory) : nameof(After)));
                    var info=Harmony.GetPatchInfo(found);
                    bool pre=false,post=false;
                    foreach(var p in info.Prefixes) if(p.owner==harmony.Id) pre=true;
                    foreach(var p in info.Postfixes) if(p.owner==harmony.Id) post=true;
                    status["status"]=pre&&post ? "installed" : "verification_failed";
                }
                catch(Exception e) { status["status"]="unavailable"; status["error"]=e.GetType().Name; }
                HookStatus.Add(status);
            }
        }
        private Dictionary<string, object> Snapshot(object instance, Spec spec)
        {
            var result=spec.Type=="Oc.Item.UI.OcUI_StatusUpPageSoulOrb" ? MilestoneEvidence.Anubis(instance) : EvidenceProjection.Read(instance,spec.Fields);
            if(spec.Type=="Oc.OcBldg_WorldHeritage") {
                TrackTower(instance);
                result["tower"]=ProgressionEvidence.Tower(instance);
                result["world_context"]=WorldEvidenceCapture.WorldContext(WorldEvidenceCapture.FindWorld());
            }
            if(spec.Type=="Oc.Maps.OcWorldManager" || spec.Type=="Oc.OcUI_AgeEvolution") {
                var world=spec.Type=="Oc.Maps.OcWorldManager" ? instance : WorldEvidenceCapture.FindWorld();
                result["world_context"]=WorldEvidenceCapture.WorldContext(world);
                result["loading_fields"]=EvidenceProjection.Read(world,new[]{"_islandLoading"});
            }
            if(spec.Type=="Oc.OcUI_AgeEvolution") result["current_age_evo_field"]=ProgressionEvidence.Age(CatalogFields.Field(instance,"_currentAgeEvo"));
            if(spec.Type=="Oc.Item.UI.OcUI_ShopUI") result["selected_offer"]=EvidenceProjection.Read(
                CatalogFields.Field(instance,"selectingSheet"),new[]{"btnIndex","<Price>k__BackingField","_saleInfo"});
            return result;
        }
        // Void prefix cannot skip the game method. Only __state is assigned;
        // arguments, original return values, and game fields are never modified.
        private static void Before(object __instance, MethodBase __originalMethod, object[] __args, out Dictionary<string, object> __state)
        {
            __state=null;
            try
            {
                var a=active;
                if(a==null || a.total>=10000 || a.writtenBytes>=64*1024*1024) return;
                if(Thread.CurrentThread.ManagedThreadId!=a.mainThread) { Interlocked.Increment(ref a.ignoredThread); return; }
                if(!a.installed.TryGetValue(__originalMethod,out var spec)) return;
                var args=new List<object>(); var ps=__originalMethod.GetParameters();
                for(int i=0;i<__args.Length;i++) args.Add(new Dictionary<string,object> {
                    ["name"]=ps[i].Name,["type"]=ps[i].ParameterType.FullName,["value"]=__args[i] }); // exact scalar-only signatures
                __state=new Dictionary<string,object> { ["sequence"]=++a.sequence,["utc"]=DateTime.UtcNow.ToString("o"),
                    ["method"]=spec.Type+"::"+spec.Method,["arguments"]=args,["capture_id"]=a.Capture,
                    ["world_context"]=WorldEvidenceCapture.WorldContext(spec.Type=="Oc.Maps.OcWorldManager" ? __instance : a.World),
                    ["world_context_source"]=spec.Type=="Oc.Maps.OcWorldManager" ? "method_instance" : "cached_manager_candidate_not_authoritative",
                    ["world_context_cache_age_seconds"]=a.WorldObservedAt==default(DateTime) ? null : (object)(DateTime.UtcNow-a.WorldObservedAt).TotalSeconds,
                    ["before"]=a.Snapshot(__instance,spec),
                    ["interpretation"]="method_returned_not_proof_of_reward_or_new_completion" };
                if(spec.Method=="TryTakeCategoryReward" && __args.Length==1 && __args[0] is int category)
                    __state["category_before"]=MilestoneEvidence.Category(__instance,category);
            }
            catch { __state=null; }
        }
        private static void After(object __instance, MethodBase __originalMethod, Dictionary<string, object> __state)
        {
            try
            {
                var a=active; if(a==null || __state==null) return;
                if(a.installed.TryGetValue(__originalMethod,out var spec)) __state["after"]=a.Snapshot(__instance,spec);
                a.buffer.Add(__state); a.total++;
            }
            catch { /* Diagnostics never propagate into gameplay. */ }
        }
        private static void AfterCategory(object __instance, MethodBase __originalMethod, bool __result, Dictionary<string, object> __state)
        {
            try { if(__state!=null) {
                __state["native_result"]=__result;
                if(__state.TryGetValue("category_before",out var before) && before is Dictionary<string,object> row && row["category_id"] is int id)
                    __state["category_after"]=MilestoneEvidence.Category(__instance,id);
            } }
            catch { }
            After(__instance,__originalMethod,__state);
        }
        internal Dictionary<string,object> Status() => new Dictionary<string,object> {
            ["session"]=Session,["hooks"]=HookStatus,["observed"]=total,["dropped"]=buffer.Dropped,
            ["game_assembly_sha256"]=GameHash,["collector_sha256"]=CollectorHash,
            ["ignored_non_main_thread"]=ignoredThread,["written_bytes"]=writtenBytes,
            ["limit_reached"]=total>=10000||writtenBytes>=64*1024*1024 };
        internal void Flush()
        {
            var rows=buffer.Peek(); if(rows.Count==0 || writtenBytes>=64*1024*1024) return;
            try
            {
                var data=Status(); data["schema_version"]=5; data["collector_version"]="1.5.2"; data["events"]=rows;
                string json=JsonWriter.Stringify(data);
                string path=Path.Combine(directory,"observations_"+Session+"_"+sequence+"_"+Guid.NewGuid().ToString("N")+".json");
                Directory.CreateDirectory(directory);
                string pending=Path.Combine(directory,"observations_"+Session+".pending.tmp");
                File.WriteAllText(pending,json); File.Move(pending,path);
                writtenBytes+=new FileInfo(path).Length; buffer.Confirm(rows.Count);
                writeFailureLogged=false;
            }
            catch(Exception e) { if(!writeFailureLogged) { writeFailureLogged=true; log("Observation write deferred: "+e.GetType().Name); } }
        }
        internal void Dispose() { Flush(); if(active==this) active=null; harmony.UnpatchSelf(); }
    }
}
