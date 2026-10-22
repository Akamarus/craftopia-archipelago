using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using CraftopiaArchipelago;
class Program {
 static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
 static void Main() {
  string added=MilestoneSession.Key("category",28,0);
  string old=MilestoneSession.Key("dungeon",1,0);
  MilestoneSession.LoadCatalog();
  Check(MilestoneSession.Frozen[old]==4100 && !MilestoneSession.Frozen.ContainsKey(added),"Legacy default changed");
  MilestoneSession.LoadCatalog(MilestoneSession.CurrentCatalog);
  Check(MilestoneSession.Frozen[old]==4100 && MilestoneSession.Frozen[added]==4228,"New catalog missing additions");
  Check(MilestoneSession.Frozen[MilestoneSession.Key("category",141,0)]==4341,"Tutorial missing");
  Check(MilestoneSession.Frozen[MilestoneSession.Key("dungeon",2,0)]==4101 && !MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",202,0)),"Story dungeon identity incorrect");
  MilestoneSession.LoadCatalog(MilestoneSession.YardenCatalog);
  Check(MilestoneSession.Frozen[MilestoneSession.Key("dungeon",3,0)]==4102,"Hunters Story missing");
  Check(MilestoneSession.Frozen[MilestoneSession.Key("dungeon",4,0)]==4103,"Storms Story missing");
  Check(!MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",203,0)) && !MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",204,0)),"Repeat dungeon leaked");
  Check(MilestoneSession.Frozen[MilestoneSession.Key("tower",-793448649,3)]==3022,"Mountain negative source lost");
  MilestoneSession.LoadCatalog(MilestoneSession.ValleyCatalog);
  Check(MilestoneSession.Frozen[MilestoneSession.Key("dungeon",5,0)]==4104 && MilestoneSession.Frozen[MilestoneSession.Key("dungeon",6,0)]==4105,"Valley Story IDs missing");
  Check(!MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",205,0)) && !MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",206,0)),"Valley repeat leaked");
  foreach(var catalog in new[]{MilestoneSession.Catalog,MilestoneSession.CurrentCatalog,MilestoneSession.YardenCatalog,MilestoneSession.ValleyCatalog,MilestoneSession.Catalog}) {
   // Binding selects the catalog before evaluating seed/slot identity.
   MilestoneSession.Bind(new Dictionary<string,object>{{"milestone_catalog",catalog},{"milestone_schema",1L},
     {"multiplayer_mode",0L},{"milestone_locations",new Dictionary<string,object>{{old,4100L}}}});
   Check(MilestoneSession.Frozen.ContainsKey(added)==(catalog!=MilestoneSession.Catalog),"Bind used wrong frozen catalog");
   Check(MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",3,0))==(catalog==MilestoneSession.YardenCatalog || catalog==MilestoneSession.ValleyCatalog),"Yarden catalog leaked");
  }
  try { MilestoneSession.LoadCatalog("unknown");throw new Exception("Unknown catalog accepted"); }
  catch(FormatException) { }
  var guids=new List<int>();
  foreach(var name in new[]{"geezah_orbs_20261016_r1","millewind_orbs_20261017_r1","yarden_orbs_20261022_r1","owatatsu_orbs_20261022_r1","brigandine_orbs_20261022_r1"}) {
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name+".json"))
   using(var reader=new StreamReader(stream)) {
    foreach(Dictionary<string,object> row in (List<object>)MiniJson.ParseObject(reader.ReadToEnd())["locations"])
     guids.Add(checked((int)(long)row["guid"]));
   }
  }
  var session=new Dictionary<string,object>{{"seed_name","test"},{"slot","Yarden"},{"epoch",1L}};
  OrbMapTracker.Bind(session,guids);
  Check(OrbMapTracker.State.Visible(false).Count()==239,"All regional map markers must appear immediately");
  var yarden=OrbMapTracker.State.Visible(false).Last();
  OrbMapTracker.State.MarkCollected(OrbMapTracker.Identity(session),yarden.Guid);
  Check(OrbMapTracker.State.Visible(true).Count()==238,"Collected marker filtering");
  session["seed_name"]="yarden";
  OrbMapTracker.Bind(session,guids.Take(137));
  Check(OrbMapTracker.State.Visible(false).Count()==137,"Older Yarden sessions must retain 137 markers");
  session["seed_name"]="old";
  OrbMapTracker.Bind(session,guids.Take(88));
  Check(OrbMapTracker.State.Visible(false).Count()==88,"Older sessions must not show Yarden markers");
  Console.WriteLine("Milestone catalog compatibility: legacy, current and Yarden assertions passed");
 }
}
