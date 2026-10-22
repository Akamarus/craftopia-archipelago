using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace CraftopiaArchipelago {
 internal static class OrbMapTracker {
  internal static readonly OrbMapState State=new OrbMapState();
  static Dictionary<int,OrbMarker> coordinates;
  internal static string Identity(Dictionary<string,object> m)=>MiniJson.Stringify(new Dictionary<string,object>{{"seed",MiniJson.GetString(m,"seed_name")},{"slot",MiniJson.GetString(m,"slot")},{"epoch",MiniJson.GetLong(m,"epoch")}});
  static void Load(){
   if(coordinates!=null)return;var loaded=new Dictionary<int,OrbMarker>();
   foreach(var name in new[]{"geezah_orbs_20261016_r1","millewind_orbs_20261017_r1","yarden_orbs_20261022_r1"}){
    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name+".json"))
    using(var reader=new StreamReader(stream)){
     var json=MiniJson.ParseObject(reader.ReadToEnd());
     foreach(var obj in (List<object>)json["locations"]){
      var r=(Dictionary<string,object>)obj;var p=(Dictionary<string,object>)r["position"];
      int guid=checked((int)(long)r["guid"]);
      loaded.Add(guid,new OrbMarker(guid,(long)r["ap_location_id"],Convert.ToSingle(p["x"]),Convert.ToSingle(p["y"]),Convert.ToSingle(p["z"])));
     }
    }
   }
   coordinates=loaded;
  }
  internal static void Bind(Dictionary<string,object> message,IEnumerable<int> selected){
   try{
    Load();var rows=new List<OrbMarker>();int missing=0;
    foreach(int guid in selected)if(coordinates.TryGetValue(guid,out var m))rows.Add(m);else missing++;
    State.ApplySession(Identity(message),rows);
    if(missing>0)Plugin.Log?.LogWarning("[AP] Map omitted "+missing+" orb(s) without verified regional coordinates.");
    Plugin.Bridge?.RestoreOrbMapPending();
   }catch(Exception ex){State.Clear();Plugin.Log?.LogWarning("[AP] Orb map catalog unavailable: "+ex.GetType().Name);}
  }
  internal static void Snapshot(Dictionary<string,object> message){
   if(MiniJson.GetLong(message,"orb_map_protocol")!=1)return;
   var identity=Identity(message);State.ApplyCompleted(identity,MiniJson.GetLongList(message,"checked"));
   foreach(long guid in MiniJson.GetLongList(message,"collected"))if(guid>=int.MinValue&&guid<=int.MaxValue)State.MarkCollected(identity,(int)guid);
  }
 }
}
