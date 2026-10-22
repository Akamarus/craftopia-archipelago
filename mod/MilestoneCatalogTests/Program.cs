using System;
using System.Collections.Generic;
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
  foreach(var catalog in new[]{MilestoneSession.Catalog,MilestoneSession.CurrentCatalog,MilestoneSession.YardenCatalog,MilestoneSession.Catalog}) {
   // Binding selects the catalog before evaluating seed/slot identity.
   MilestoneSession.Bind(new Dictionary<string,object>{{"milestone_catalog",catalog},{"milestone_schema",1L},
     {"multiplayer_mode",0L},{"milestone_locations",new Dictionary<string,object>{{old,4100L}}}});
   Check(MilestoneSession.Frozen.ContainsKey(added)==(catalog!=MilestoneSession.Catalog),"Bind used wrong frozen catalog");
   Check(MilestoneSession.Frozen.ContainsKey(MilestoneSession.Key("dungeon",3,0))==(catalog==MilestoneSession.YardenCatalog),"Yarden catalog leaked");
  }
  try { MilestoneSession.LoadCatalog("unknown");throw new Exception("Unknown catalog accepted"); }
  catch(FormatException) { }
  Console.WriteLine("Milestone catalog compatibility: legacy, current and Yarden assertions passed");
 }
}
