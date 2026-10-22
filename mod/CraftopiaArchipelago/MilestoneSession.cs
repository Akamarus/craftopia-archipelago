using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace CraftopiaArchipelago
{
    internal static class MilestoneSession
    {
        internal const string Catalog="milestones_20261016_r1";
        internal const string CurrentCatalog="milestones_20261017_r2";
        internal const string YardenCatalog="milestones_20261022_r3";
        internal const string ValleyCatalog="milestones_20261022_r4";
        static string activeCatalog;
        internal static readonly Dictionary<string,long> Frozen=new Dictionary<string,long>();
        static readonly Dictionary<int,Dictionary<string,object>> towers=new Dictionary<int,Dictionary<string,object>>();
        static readonly HashSet<string> selected=new HashSet<string>();
        static readonly Queue<Dictionary<string,object>> retry=new Queue<Dictionary<string,object>>();
        internal static bool HasUnpersistedObservations => retry.Count > 0;
        static string seed,slot,worldHash;
        static long epoch;
        static bool enabled,valid,eligible;
        static int boundCharacter;
        static float nextRegistration;
        static object boundWorld;
        internal static string Key(string family,int source,int stage) => "milestone:"+family+":"+source+":"+stage;
        internal static object Field(object value,string name) {
            for(var t=value?.GetType();t!=null;t=t.BaseType) {
                var f=t.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
                if(f!=null) return f.GetValue(value);
            }
            return null;
        }
        internal static void LoadCatalog(string catalog=Catalog) {
            if(catalog!=Catalog && catalog!=CurrentCatalog && catalog!=YardenCatalog && catalog!=ValleyCatalog) throw new FormatException("Unsupported milestone catalog");
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(catalog+".json"))
            using(var reader=new StreamReader(stream)) {
                var d=MiniJson.ParseObject(reader.ReadToEnd());
                if(MiniJson.GetString(d,"catalog_id")!=catalog || !(d["locations"] is List<object> rows)) throw new FormatException("Milestone catalog");
                Frozen.Clear();towers.Clear();
                foreach(Dictionary<string,object> r in rows) {
                    int source=checked((int)(long)r["source_id"]);
                    Frozen.Add(Key((string)r["family"],source,checked((int)(long)r["stage"])),(long)r["ap_location_id"]);
                    if((string)r["family"]=="tower")towers[source]=r;
                }
            }
        }
        static bool TowerMatches(object tower,Dictionary<string,object> row) {
            if(!(tower is UnityEngine.Component component) || component==null || !(Field(tower,"_IsTower") is bool isTower) || !isTower) return false;
            if(!(Field(Field(tower,"_Data"),"_WorldHeritageItemId") is int definition))return false;
            var p=component.transform.position;
            var expected=(Dictionary<string,object>)row["position"];
            return TowerIdentity.Matches(checked((int)(long)row["definition_id"]),Convert.ToDouble(expected["x"]),Convert.ToDouble(expected["y"]),Convert.ToDouble(expected["z"]),definition,p.x,p.y,p.z);
        }
        static Oc.OcBldg_WorldHeritage FindTower(int source) {
            if(!towers.TryGetValue(source,out var row))return null;
            var registry=Field(Oc.OcInstallObjMng.Inst,"_WorldHeritageBuff") as List<Oc.OcBldg_WorldHeritage>;
            if(registry==null)return null;
            Oc.OcBldg_WorldHeritage match=null;
            foreach(var tower in registry)if(TowerMatches(tower,row)) {
                if(match!=null)return null;
                match=tower;
            }
            return match;
        }
        internal static int? TowerSource(object tower) {
            int? match=null;
            foreach(var pair in towers)if(TowerMatches(tower,pair.Value)) {
                if(match!=null || !ReferenceEquals(FindTower(pair.Key),tower))return null;
                match=pair.Key;
            }
            return match;
        }
        internal static void Bind(Dictionary<string,object> m) {
            eligible=false; valid=false;
            enabled=m!=null && m.ContainsKey("milestone_catalog");
            if(!enabled) return;
            try {
                string catalog=MiniJson.GetString(m,"milestone_catalog");
                if((catalog!=Catalog && catalog!=CurrentCatalog && catalog!=YardenCatalog && catalog!=ValleyCatalog) || !(m["milestone_schema"] is long schema) || schema!=1 ||
                    !(m["multiplayer_mode"] is long mode) || mode!=0 || !(m["milestone_locations"] is Dictionary<string,object> map) || map.Count==0) return;
                LoadCatalog(catalog);
                var next=new HashSet<string>();
                foreach(var pair in map) {
                    if(!(pair.Value is long id) || !Frozen.TryGetValue(pair.Key,out var frozen) || frozen!=id) return;
                    next.Add(pair.Key);
                }
                string s=MiniJson.GetString(m,"seed_name"), sl=MiniJson.GetString(m,"slot");long ep=MiniJson.GetLong(m,"epoch");
                if(string.IsNullOrWhiteSpace(s) || sl!=Plugin.SlotName.Value || ep<=0) return;
                if(seed!=s || slot!=sl || epoch!=ep || activeCatalog!=catalog) {
                    if(retry.Count>0) { Notice("Milestone persistence failed; keep this session for recovery.");return; }
                    boundWorld=null;boundCharacter=0;worldHash=null;
                }
                seed=s;slot=sl;epoch=ep;activeCatalog=catalog;selected.Clear(); foreach(var k in next) selected.Add(k);
                valid=MilestoneHooks.Installed;
            } catch { valid=false; }
        }
        static string Hash(string value) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-",""); }
        static void Notice(string text) { try { Plugin.StatusDisplay?.Transition("milestones",text); } catch { } }
        static int? BaselineState(string key) {
            var parts=key.Split(':');int source=int.Parse(parts[2]);
            var wm=Oc.Maps.OcWorldManager.Inst;
            if(wm==null || wm.IsLoading || wm.CurrentWorld==null || wm.InGameData==null) return null;
            switch(parts[1]) {
                case "tower":
                    var saved=wm.GetWorldHeritageSaveDatas();
                    if(saved==null)return null;
                    var live=FindTower(source);
                    if(live==null)return null;
                    var rows=saved.Where(r=>r.GUID==live.GUID).ToArray();
                    if(rows.Length>1)return null;
                    int? current=Field(live,"_UnderBuildingNum") is int stage ? stage : (int?)null;
                    if(rows.Length==0 && current==null)return null;
                    if(current<0 || (rows.Length==1 && rows[0].CurrentProgress<0))return null;
                    return Math.Max(current??0,rows.Length==1?rows[0].CurrentProgress:0);
                case "age": return wm.CurrentWorld.worldLevel;
                case "dungeon":
                    var history=wm.InGameData.dungeonClear;
                    var runtime=Field(Oc.Dungeon.OcDungeonManager.Inst,"DungeonClearedList") as List<int>;
                    if(history==null || runtime==null)return null;
                    return history.Contains(source)||runtime.Contains(source)?1:0;
                case "category":
                    var manager=Oc.Missions.OcMissionManager.Inst;
                    if(manager==null || !manager.DataLoaded)return null;
                    return manager.IsCategoryRewardTaken(source)?1:0;
                default: return null;
            }
        }
        // Called only by the explicit enrollment buttons, never every GUI frame.
        internal static object CaptureFreshEnrollmentWorld() {
            try {
                var manager=Oc.Maps.OcWorldManager.Inst;
                var world=manager?.CurrentWorld;
                if(manager==null || manager.IsLoading || world==null || manager.InGameData==null ||
                    Oc.OcGameSceneShareSingleton.IsOpening || Oc.Network.OcNetMng.Inst.IsMultiPlay ||
                    world.worldLevel!=0 || !(Field(world,"createdDate") is long created) || created<=0 ||
                    !(Field(world,"worldSeed") is byte worldSeed) || Plugin.GetCharacterGuid()==0 ||
                    (enabled && (!valid || !MilestoneBaseline.Accept(selected,BaselineState)))) return null;
                return new FreshWorldBaseline(world,created,worldSeed,Plugin.GetCharacterGuid());
            } catch { return null; }
        }
        internal static bool SameFreshEnrollmentWorld(object snapshot) {
            try {
                if(!(snapshot is FreshWorldBaseline baseline))return false;
                var manager=Oc.Maps.OcWorldManager.Inst;
                var world=manager?.CurrentWorld;
                if(manager==null || manager.IsLoading || world==null || manager.InGameData==null ||
                    Oc.OcGameSceneShareSingleton.IsOpening || Oc.Network.OcNetMng.Inst.IsMultiPlay ||
                    !(Field(world,"createdDate") is long created) || !(Field(world,"worldSeed") is byte worldSeed))return false;
                return baseline.Matches(world,created,worldSeed,Plugin.GetCharacterGuid(),world.worldLevel,
                    !enabled || (valid && MilestoneBaseline.Accept(selected,BaselineState)));
            } catch { return false; }
        }
        internal static void Pump(bool allowed) {
            eligible=false;
            if(!enabled) return;
            try {
                if(!valid) { Notice("Milestone checks unavailable: session or required hooks invalid."); return; }
                if(!allowed || !Plugin.IsDeliveryEligible() || Plugin.Validator.CurrentEpoch!=epoch || Oc.Network.OcNetMng.Inst.IsMultiPlay || Oc.OcGameSceneShareSingleton.IsOpening) return;
                int character=Plugin.GetCharacterGuid();
                var world=Oc.Maps.OcWorldManager.Inst?.CurrentWorld;
                if(world==null || character==0) return;
                var enrollment=Plugin.EnrollmentState?.Read(character,epoch,slot,Plugin.Validator.HasHistory==null || Plugin.Validator.HasHistory());
                if(!enrollment.HasValue || (enrollment.Value.Status!=EntitlementStatus.Ok && enrollment.Value.Status!=EntitlementStatus.NoRecordConfirmed)) {
                    Notice("Milestone checks waiting for approved character enrollment.");return;
                }
                if(Plugin.Validator.IsEnrolled==null || !Plugin.Validator.IsEnrolled(character)) {
                    if(UnityEngine.Time.realtimeSinceStartup>=nextRegistration) {
                        nextRegistration=UnityEngine.Time.realtimeSinceStartup+5f;
                        Plugin.Bridge.SendMilestoneEnrollment(character,epoch,seed,slot);
                    }
                    Notice("Milestone checks registering the approved character with the bridge.");return;
                }
                if(!ReferenceEquals(world,boundWorld) || character!=boundCharacter) {
                    var date=Field(world,"createdDate");var worldSeed=Field(world,"worldSeed");
                    if(!(date is long created) || created<=0 || !(worldSeed is byte ws)) { Notice("Milestone checks waiting for world identity.");return; }
                    string fingerprint=Hash(created.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+ws);
                    string path=Path.Combine(UserStorage.Root,"craftopia-milestone-"+Hash(seed+":"+slot+":"+epoch+":"+character)+".bound");
                    if(!SoulOrbCharacterBinding.RequiresSession(path) && !MilestoneBaseline.Accept(selected,BaselineState)) {
                        Notice("Pilot needs a fresh character and world near the first tower; waiting for an uncompleted baseline.");return;
                    }
                    if(!SoulOrbCharacterBinding.Mark(path,fingerprint)) { Notice("Milestone checks held: this character belongs to a different test world.");return; }
                    boundWorld=world;boundCharacter=character;worldHash=fingerprint;
                }
                while(retry.Count>0) {
                    if(!Plugin.Bridge.SendMilestoneObservation(retry.Peek())) { Notice("Milestone check waiting for durable storage; preserve files for recovery.");return; }
                    retry.Dequeue();
                }
                eligible=true;Notice(null);
            } catch { Notice("Milestone checks held: world or storage could not be verified."); }
        }
        internal static bool Ready => enabled && valid && eligible && Plugin.IsDeliveryEligible() &&
            Plugin.Validator.CurrentEpoch==epoch && !Oc.OcGameSceneShareSingleton.IsOpening && !Oc.Network.OcNetMng.Inst.IsMultiPlay;
        internal static bool Selected(string key) => selected.Contains(key);
        internal static bool SameWorld(object world,int character,long expectedEpoch) => Ready && ReferenceEquals(world,boundWorld) &&
            ReferenceEquals(world,Oc.Maps.OcWorldManager.Inst?.CurrentWorld) && character==boundCharacter && character==Plugin.GetCharacterGuid() && epoch==expectedEpoch;
        internal static long Epoch => epoch;
        internal static void Complete(string family,int source,int stage) {
            string k=Key(family,source,stage);
            if(!Ready || !selected.Contains(k)) return;
            var message=new Dictionary<string,object>{{"cmd","check"},{"check_category","milestone"},{"family",family},
                {"source_id",(long)source},{"stage",(long)stage},{"observation_id",k},{"seed_name",seed},{"slot",slot},
                {"epoch",epoch},{"character_guid",boundCharacter.ToString(System.Globalization.CultureInfo.InvariantCulture)},{"world_fingerprint",worldHash}};
            if(!Plugin.Bridge.SendMilestoneObservation(message)) {
                eligible=false;
                if(retry.Count<128) retry.Enqueue(message);
                Notice("Milestone persistence failed; check held for recovery.");
            }
        }
    }
}
