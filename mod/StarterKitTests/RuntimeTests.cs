using System;
using System.Collections.Generic;
namespace Oc {
 public class Player {public int GUID=123;}
 public static class OcPlMng {public static Player LocalPl=new Player();}
 public class OcSaveManager {public static OcSaveManager Inst=new OcSaveManager();public static int Calls;public void SaveGameData(bool a,bool b){Calls++;CraftopiaArchipelago.StarterKitSaveObserver.Saved=true;}}
}
namespace CraftopiaArchipelago {
 public static class UserStorage {public static string Root;}
 public class Setting {public string Value="slot";}
 public class ValidatorStub {public long CurrentEpoch=1;public Func<int,bool> IsEnrolled=g=>g==123;}
 public class BridgeStub {public int ConnectionId=1;public bool IsConnected=true;}
 public class PipelineStub {public HashSet<long> Receipts=new HashSet<long>();public void EnqueueInbound(long e,List<long> ids){foreach(var i in ids)Receipts.Add(i);}}
 public class PlayerStatusDisplay {public static double Now;public void Transition(string k,string m){}public void Notify(string k,string m,double n){Console.WriteLine(m);}}
 public static class Plugin {public static ValidatorStub Validator=new ValidatorStub();public static BridgeStub Bridge=new BridgeStub();public static Setting SlotName=new Setting();public static PipelineStub Pipeline=new PipelineStub();public static PlayerStatusDisplay StatusDisplay=new PlayerStatusDisplay();}
 public class StarterKitSaveObserver {public static bool Saved;int count,item;public static StarterKitSaveObserver Track(string t,int g,int c,int i){Saved=false;return new StarterKitSaveObserver{count=c,item=i};}public bool Verify(string t,int g)=>Saved&&StarterKitInventoryAdapter.Count(item)==count;public static void Cancel(){Saved=false;}}
 public static class StarterKitInventoryAdapter {public static bool Full;public static Dictionary<int,int> Counts=new Dictionary<int,int>();public static int Count(int id)=>Counts.TryGetValue(id,out var n)?n:0;public static CurrencyMutation TryApply(int id,int n){if(Full)return CurrencyMutation.NoMutation;Counts[id]=Count(id)+n;return CurrencyMutation.Applied;}}
 public static class RuntimeTests {
  static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
  static Dictionary<string,object> Session(string choice)=>new Dictionary<string,object>{{"starter_kit_protocol",1L},{"multiplayer_mode",0L},{"epoch",1L},{"slot","slot"},{"seed_name","seed"},{"starter_choice",choice}};
  static void Pump(int n){for(int i=0;i<n;i++){PlayerStatusDisplay.Now+=2;StarterKitRuntime.Pump(true);}}
  public static void Run(){
   UserStorage.Root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starter-test-"+Guid.NewGuid());System.IO.Directory.CreateDirectory(UserStorage.Root);
   try{
    StarterKitRuntime.ApplySession(Session("Bow"),1);
    Pump(2);Check(StarterKitInventoryAdapter.Count(92)==0,"missing enrollment cannot grant");
    StarterKitRuntime.Suspend();StarterKitRuntime.AttestNew(123);StarterKitInventoryAdapter.Full=true;Pump(3);Check(StarterKitInventoryAdapter.Count(92)==0,"full inventory defers");
    StarterKitInventoryAdapter.Full=false;Pump(2); // one live save and its confirmation
    Plugin.Bridge.ConnectionId=2;StarterKitRuntime.ApplySession(Session("Bow"),2);Pump(70);
    Check(StarterKitInventoryAdapter.Count(92)==1&&StarterKitInventoryAdapter.Count(181)==30,"complete bow kit once");
    StarterKitRuntime.Suspend();Pump(70);Check(StarterKitInventoryAdapter.Count(181)==30,"reload never duplicates consumed kit");
    StarterKitInventoryAdapter.Counts[181]=0;StarterKitRuntime.Suspend();Pump(70);Check(StarterKitInventoryAdapter.Count(181)==0,"consumed arrows do not refill");
    StarterKitRuntime.ResetSession();var staff=Session("MagicStaff");staff["seed_name"]="fresh-seed";
    StarterKitRuntime.ApplySession(staff,2);StarterKitRuntime.AttestNew(123);Pump(8);
    Check(StarterKitInventoryAdapter.Count(6392)==1&&Plugin.Pipeline.Receipts.Contains(StarterKitCatalog.SkillPointReceipt),"staff weapon and transactional skill point");
    StarterKitRuntime.Suspend();Pump(8);Check(StarterKitInventoryAdapter.Count(6392)==1&&Plugin.Pipeline.Receipts.Count==1,"staff reload stable receipt");
    StarterKitRuntime.ResetSession();var old=Session("Sword");old.Remove("starter_kit_protocol");StarterKitRuntime.ApplySession(old,2);StarterKitRuntime.AttestNew(123);Pump(5);Check(StarterKitInventoryAdapter.Count(44)==0,"old seed unchanged");
    StarterKitRuntime.ResetSession();var race=Session("Bow");race["seed_name"]="consume-during-save";
    StarterKitRuntime.ApplySession(race,2);StarterKitRuntime.AttestNew(123);Pump(3);
    StarterKitInventoryAdapter.Counts[181]=0;Pump(10);int calls=Oc.OcSaveManager.Calls;Pump(20);
    Check(Oc.OcSaveManager.Calls==calls,"unverifiable consumed arrow must stop save retries");
    Check(StarterKitInventoryAdapter.Count(181)==0,"unverified arrow never automatically regranted");
    Console.WriteLine("Starter runtime enrollment/reconnect/reload tests passed");
   }finally{StarterKitRuntime.ResetSession();System.IO.Directory.Delete(UserStorage.Root,true);}
  }
 }
}
