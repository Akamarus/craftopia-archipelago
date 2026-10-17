using System;
using System.Collections.Generic;
using System.Reflection;
static class ProgressionEvidenceTests
{
    public static void Run()
    {
        var type=Assembly.GetExecutingAssembly().GetType("CraftopiaDiagnostic.ProgressionObservation");
        if(type==null) throw new Exception("Missing progression observation helper");
        var tracker=Activator.CreateInstance(type);
        var method=type.GetMethod("Observe");
        Func<string,int,int?,Dictionary<string,object>> observe=(c,i,s)=>(Dictionary<string,object>)method.Invoke(tracker,new object[]{c,i,s});
        void Check(bool value) { if(!value) throw new Exception("Progression comparison failed"); }
        Check(observe("a",1,0)==null);
        Check(observe("a",1,0)==null);
        var changed=observe("a",1,1);
        Check((int)changed["before"]==0 && (int)changed["after"]==1);
        Check(!changed.ContainsKey("completed"));
        Check(observe("b",1,2)==null);
        Check(observe("b",1,null)==null);
        Check(observe("b",1,3)==null);
        Check(observe(null,1,4)==null);
        Check(observe("b",1,5)==null);
        var capture=Assembly.GetExecutingAssembly().GetType("CraftopiaDiagnostic.ProgressionEvidence");
        Check(capture!=null);
        var tower=(Dictionary<string,object>)capture.GetMethod("Tower").Invoke(null,new object[]{new FakeTower()});
        Check((int)tower["guid"]==17 && (int)tower["stage"]==1);
        Check(tower["is_tower"] is bool b && b);
        Check(tower["definition"]!=null);
        var definition=(Dictionary<string,object>)tower["definition"];
        var stages=(List<object>)definition["stages"];
        var costs=(Dictionary<string,object>)((Dictionary<string,object>)stages[0])["costs"];
        var cost=(Dictionary<string,object>)((List<object>)costs["rows"])[0];
        Check((int)cost["item_id"]==711 && (int)cost["count"]==5);
        var missing=(Dictionary<string,object>)capture.GetMethod("Tower").Invoke(null,new object[]{new object()});
        Check(missing["guid"]==null && missing["stage"]==null && missing["is_tower"]==null);
        var large=new FakeTowerData { requireItemsAllProgress=new FakeStage[40] };
        var bounded=(Dictionary<string,object>)capture.GetMethod("TowerData").Invoke(null,new object[]{large});
        Check((bool)bounded["truncated"] && ((List<object>)bounded["stages"]).Count==32);
        var age=(Dictionary<string,object>)capture.GetMethod("Age").Invoke(null,new object[]{new FakeAge()});
        Check((int)age["world_level"]==2 && (int)age["gold"]==500);
        for(int i=2;i<=128;i++) Check(observe("b",i,0)==null);
        Check(observe("b",129,0)==null && observe("b",129,1)==null);
        var lower=observe("b",1,4);
        Check((int)lower["after"]==4);
        type.GetMethod("Retain").Invoke(tracker,new object[]{new HashSet<int>()});
        Check(observe("b",1,5)==null);
        Console.WriteLine("ProgressionEvidenceTests: baseline, change, context, null, lower level, capacity and despawn passed");
    }
}
class FakeTower { public int _GUID=17; public int _UnderBuildingNum=1; public bool _IsTower=true; public FakeTowerData _Data=new FakeTowerData(); public int GUID=>throw new Exception("getter called"); }
class FakeTowerData { public int _WorldHeritageItemId=123; public FakeStage[] requireItemsAllProgress={new FakeStage()}; }
class FakeStage { public FakeCost[] requireItems={new FakeCost()}; }
class FakeCost { public int count=5; public FakeItem item=new FakeItem(); }
class FakeItem { public int id=711; }
class FakeAge { public int worldLevel=2; public int requireGold=500; public bool isEnabled=true; public FakeCost[] requireItems={new FakeCost()}; }
