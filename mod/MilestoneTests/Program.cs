using System;
class Program {
 static void Main() {
  var towerType=typeof(Program).Assembly.GetType("CraftopiaArchipelago.TowerIdentity");
  if(towerType==null)throw new Exception("Missing stable tower identity");
  var match=towerType.GetMethod("Matches");
  foreach(var c in new[]{new[]{3806d,-2179.257,137.47,-989.8522,1},new[]{3807d,-2179.257,137.47,-989.8522,0},new[]{3806d,-2178,137.47,-989.8522,0},new[]{3806d,double.NaN,137.47,-989.8522,0}}) {
   bool result=(bool)match.Invoke(null,new object[]{3806,-2179.257,137.47,-989.8522,(int)c[0],c[1],c[2],c[3]});
   if(result!=(c[4]==1))throw new Exception("Invalid tower identity match");
  }
  Console.WriteLine("Tower identity: 4 assertions passed");
  var type=typeof(Program).Assembly.GetType("CraftopiaArchipelago.MilestoneDecision");
  if(type==null) throw new Exception("Missing milestone decision");
  var m=type.GetMethod("Accept"); int count=0;
  foreach(var family in new[]{"tower","age","dungeon","category"}) {
   foreach(bool success in new[]{false,true}) foreach(bool loading in new[]{false,true}) foreach(bool same in new[]{false,true}) {
    var result=(bool)m.Invoke(null,new object[]{family,0,1,success,loading,same});
    if(result!=(success&&!loading&&same)) throw new Exception("Invalid acceptance "+family);
    count++;
   }
   foreach(var pair in new[]{new[]{1,1},new[]{1,0},new[]{0,2},new[]{-1,0}}) {
    if((bool)m.Invoke(null,new object[]{family,pair[0],pair[1],true,false,true})) throw new Exception("Invalid transition");count++;
   }
  }
  if((bool)m.Invoke(null,new object[]{"unknown",0,1,true,false,true})) throw new Exception("Unknown family accepted");
  Console.WriteLine("MilestoneTests: "+(count+1)+" transition assertions passed");
  var baseline=typeof(Program).Assembly.GetType("CraftopiaArchipelago.MilestoneBaseline");
  if(baseline==null)throw new Exception("Missing baseline guard");
  var check=baseline.GetMethod("Accept");
  foreach(var value in new int?[]{null,-1,0,1,3}) {
   Func<string,int?> read=k=>value;
   bool accepted=(bool)check.Invoke(null,new object[]{new[]{"milestone:tower:1:1","milestone:category:2:0"},read});
   if(accepted!=(value==0))throw new Exception("Nonfresh/unknown baseline accepted");
  }
  Func<string,int?> mixed=k=>k.Contains("category")?1:0;
  if((bool)check.Invoke(null,new object[]{new[]{"milestone:tower:1:1","milestone:category:2:0"},mixed}))throw new Exception("Claimed category accepted");
  Func<string,int?> throwing=k=>throw new Exception("read");
  if((bool)check.Invoke(null,new object[]{new[]{"x"},throwing}))throw new Exception("Reader exception accepted");
  Console.WriteLine("Milestone baseline: 7 assertions passed");
 }
}
