using System;
using System.IO;
using System.Collections.Generic;
using CraftopiaArchipelago;
class Program {
 static int failures; static int assertions;
 static void Check(bool ok,string name){assertions++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static int Main(){
 var p=new object();var e=new Eligibility(_=>{},_=>{});
 e.OnLoadStart(p,-7,1);e.OnLoadComplete(p,-7,1);
 e.OnValidation(ValidationOutcome.RunVanilla,p,-7,123,1);
 Check(!e.IsEligible(p,-7,123,1),"request to run original is not validation success");
 bool rejected=false;try{MiniJson.ParseObject("{\"epoch\":1} garbage");}catch{rejected=true;}
 Check(rejected,"trailing garbage is rejected");
 e.OnValidation(ValidationOutcome.Pass,p,-7,123,1);
 e.OnLoadStart(p,-7,2);e.OnLoadComplete(p,-7,1);
 e.OnValidation(ValidationOutcome.Pass,p,-7,123,1);
 Check(!e.IsEligible(p,-7,123,1),"stale load completion cannot resurrect old event");
 RunRepairTests();
 Console.WriteLine("Assertions: "+assertions+", failures: "+failures);
 return failures==0?0:1;
  }
 static ValidationSnapshot Snap(object p,long epoch=123,int actual=0,EntitlementStatus status=EntitlementStatus.NoRecordConfirmed,long load=1)
 => new ValidationSnapshot {Player=p,LoadEvent=load,Ready=true,Input=new ValidationInput {
  TotalsKnown=true,Identity=IdentityStatus.Verified,Guid=-7,Epoch=epoch,Level=1,K=2,
  UnspentPoints=actual,Entitlement=status,BoundGuid=-7,BoundEpoch=epoch}};
 static void RunRepairTests() {
 string root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(root);
 string state=Path.Combine(root,"grants.json");
 string[] bad={ "true","null","1.5","1.0","1e0","\"1\"","-1","9223372036854775808" };
 foreach(var item in bad) {
  string json="{\"epoch\":123,\"applied\":["+item+"],\"quarantined\":[]}";
  File.WriteAllText(state,json); bool read=false,write=false;
  try {PersistedGrantState.Read(state);} catch {read=true;}
  try {PersistedGrantState.MergeApplied(state,123,new long[]{9});} catch {write=true;}
  Check(read&&write&&File.ReadAllText(state)==json,"startup and writer reject/preserve "+item);
 }
 foreach(var json in new[]{"{\"epoch\":true,\"applied\":[]}","{\"epoch\":0,\"applied\":[]}",
  "{\"epoch\":123}","{\"epoch\":123,\"applied\":false}","{\"epoch\":123,\"applied\":[1,1]}",
  "{\"epoch\":123,\"applied\":[1],\"quarantined\":[1]}",
  "{\"epoch\":123,\"epoch\":124,\"applied\":[]}","{\"epoch\":+123,\"applied\":[]}"}) {
  bool rejected=false;try{PersistedGrantState.Parse(json);}catch{rejected=true;}Check(rejected,"invalid state rejected "+json);
 }
 File.WriteAllText(state,"{\"epoch\":123,\"applied\":[1]}");
 PersistedGrantState.MergeApplied(state,123,new long[]{2});var saved=PersistedGrantState.Read(state);
 Check(saved.Applied.SetEquals(new long[]{1,2}),"valid legacy state merges actual indices");
 bool mismatch=false;try{PersistedGrantState.MergeApplied(state,124,new long[]{3});}catch{mismatch=true;}
 Check(mismatch&&PersistedGrantState.Read(state).Applied.Count==2,"epoch mismatch preserves file");
 var p=new object();
 var holdPath=Path.Combine(root,"holds.json");
 var gate=new ValidationGate(new RecoveryHoldStore(holdPath));
 var initial=gate.Begin(Snap(p,0),false);
 Check(!initial.RunOriginal&&gate.CanSoftRetry&&gate.Last.Outcome==ValidationOutcome.Defer,"epoch wait is transient");
 gate.Begin(Snap(p),true);
 Check(gate.Last.Outcome==ValidationOutcome.Pass,"late session matching totals pass without original");
 var e=new Eligibility(_=>{},_=>{}); e.OnLoadStart(p,-7,1);e.OnLoadComplete(p,-7,1);
 e.OnSessionChange(123); e.OnValidation(gate.Last.Outcome,p,-7,123,1);
 Check(e.IsEligible(p,-7,123,1),"late session preserves proven load completion");
 gate.Begin(Snap(p,123,5),true);
 Check(gate.Last.Outcome==ValidationOutcome.Defer&&!gate.CanSoftRetry,"soft mismatch cannot authorize vanilla or grant");
 gate.Begin(Snap(p),true);
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"coincidentally matching totals cannot clear hold");
 gate=new ValidationGate(new RecoveryHoldStore(holdPath));gate.Begin(Snap(p),false);
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"hold survives restart");
 foreach(int actual in new[]{0,2}) {
  var input=Snap(p,123,actual,EntitlementStatus.Ok);input.Input.EntitlementPoints=1;
  var g=new ValidationGate(new RecoveryHoldStore(Path.Combine(root,"difference"+actual)));
  g.Begin(input,false);input.Input.UnspentPoints=1;g.Begin(input,true);
  Check(g.Last.Outcome==ValidationOutcome.Defer,"both AP discrepancy directions stay held "+actual);
 }
 foreach(var status in new[]{EntitlementStatus.UncertainHistory,EntitlementStatus.StoreUnreadable,EntitlementStatus.EpochMismatch,EntitlementStatus.PendingIntent}) {
  string hp=Path.Combine(root,"status"+status);
  var g=new ValidationGate(new RecoveryHoldStore(hp));g.Begin(Snap(p,123,0,status),false);
  g=new ValidationGate(new RecoveryHoldStore(hp));g.Begin(Snap(p),true);
  Check(g.Last.Outcome==ValidationOutcome.Defer,"persistent status survives restart "+status);
 }
 gate=new ValidationGate(new RecoveryHoldStore(Path.Combine(root,"original")));
 var call=gate.Begin(Snap(p,123,5),false);
 Check(call.RunOriginal&&gate.Last.Outcome==ValidationOutcome.Defer,"original path awaits postfix");
 gate.Complete(call,Snap(p));
 Check(gate.Last.Outcome==ValidationOutcome.Pass,"matching original readback confirms");
 call=gate.Begin(Snap(p),false);gate.Complete(call,Snap(new object()));
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"changed player readback blocked");
 call=gate.Begin(Snap(p),false);gate.Complete(call,Snap(p,124));
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"changed epoch readback blocked");
 call=gate.Begin(Snap(p),false);gate.Complete(call,Snap(p,123,0,EntitlementStatus.NoRecordConfirmed,2));
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"same-object reload invalidates readback");
 call=gate.Begin(Snap(p),false);
 gate.Begin(Snap(p),true);
 gate.Complete(call,Snap(p));
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"superseded invocation cannot publish newer permission");
 call=gate.Begin(Snap(p),false);gate.Failed(call);gate.Begin(Snap(p),true);
 Check(gate.Last.Outcome==ValidationOutcome.Defer,"original exception remains held");
 int writes=0;string retryPath=Path.Combine(root,"retry");
 var holds=new RecoveryHoldStore(retryPath,(path,json)=>{writes++;if(writes==1)throw new IOException();File.WriteAllText(path,json);});
 holds.Hold(-7,123,"discrepancy");
 Check(holds.IsHeld(-7,123)&&writes==1,"failed hold write blocks immediately");
 holds.RetryWrite();
 Check(writes==2&&new RecoveryHoldStore(retryPath).IsHeld(-7,123),"write-only retry survives restart");
 File.WriteAllText(retryPath,"broken");holds=new RecoveryHoldStore(retryPath);
 Check(holds.IsHeld(-8,999)&&!holds.RetryWrite()&&File.ReadAllText(retryPath)=="broken","corrupt holds block all and preserve evidence");
 string ep=Path.Combine(root,"entitlement.json"),gp=Path.Combine(root,"delivery.json"),ip=Path.Combine(root,"intent.json");
 File.WriteAllText(ep,"{}");
 var store=new EntitlementStore(ep,_=>{},_=>{});store.Load();
 Check(store.Read(-7,123,true,false).Status==EntitlementStatus.UncertainHistory,
  "enrollment hint and empty ledger do not prove missing character history is fresh");
 string corruptEp=Path.Combine(root,"bad-entitlement");
 File.WriteAllText(corruptEp,"{\"-7\":{\"123\":{\"points\":true}}}");
 var brokenStore=new EntitlementStore(corruptEp,_=>{},_=>{});brokenStore.Load();
 Check(brokenStore.Read(-7,123,true,false).Status==EntitlementStatus.StoreUnreadable,
  "malformed entitlement never becomes an empty fresh store");
 int mutations=0,writeAttempts=0;
 var txn=new GrantTransaction(ip,store,ids=>{
    if(++writeAttempts==1)return false;
    PersistedGrantState.MergeApplied(gp,123,ids);return true;
 },_=>{},_=>{});
 var pipeline=new GrantPipeline();pipeline.OnGrantItems(123,new List<long>{10});
 var deliveryGate=new ValidationGate(new RecoveryHoldStore(Path.Combine(root,"delivery-holds")));
 deliveryGate.Begin(Snap(p,0),true);
 Func<List<long>,GrantApplyResult> apply=ids=>txn.Execute(ids,-7,123,1,x=>{
   mutations++;return GrantApplyResult.Confirmed;
 }).Count>0?GrantApplyResult.Confirmed:GrantApplyResult.Retryable;
 List<long> ack=new List<long>();
 if(deliveryGate.Last.Outcome==ValidationOutcome.Pass)ack=pipeline.OnFrame(0,apply,()=>true);
 Check(mutations==0&&ack.Count==0,"unvalidated delivery neither mutates nor acks");
 deliveryGate.Begin(Snap(p),true);
 ack=pipeline.OnFrame(0,apply,()=>true);
 Check(mutations==1&&ack.Count==0&&File.Exists(ip),"apply followed by disk failure withholds ack and keeps intent");
 ack=pipeline.OnFrame(10000,apply,()=>true);
 Check(mutations==1&&ack.Count==1&&!File.Exists(ip)&&store.Read(-7,123,true,true).Points==1,
  "persist-only retry completes once through production transaction and pipeline");
 var disk=PersistedGrantState.Read(gp);pipeline=new GrantPipeline();
 pipeline.Restore(disk.Epoch,new List<long>(disk.Applied),new List<long>(disk.Quarantined));
 var replay=pipeline.OnGrantItems(123,new List<long>{10});
 Check(replay.Reack.Count==1&&replay.FreshQueued==0&&mutations==1,"restart replay recognizes durably applied reward");
 e=new Eligibility(_=>{throw new Exception();},_=>{throw new Exception();});
 e.OnLoadStart(p,-7,1);e.OnLoadComplete(p,-7,1);e.OnValidation(ValidationOutcome.Pass,p,-7,123,1);
 e.Block("error");
 Check(!e.IsEligible(p,-7,123,1),"throwing diagnostics never preserve eligibility");
 }
}




