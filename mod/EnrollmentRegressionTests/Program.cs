using System;
using System.IO;
using System.Collections.Generic;
using CraftopiaArchipelago;
class Program {
 static int checks, failures;
 static void Check(bool ok,string name) { checks++; if(!ok)failures++; Console.WriteLine((ok?"PASS ":"FAIL ")+name); }
 static string Root() { var p=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(p);return p; }
 static EnrollmentState Open(string path) {var s=new EnrollmentState(path,_=>{},_=>{});s.Load();return s;}
 static void FailedSecondWrite() {
  string root=Root(), path=Path.Combine(root,"enrollment.json"), applied=Path.Combine(root,"applied.json"), intent=Path.Combine(root,"intent.json");
  var s=Open(path);Check(s.Attest(-7,123,"Test","Disposable","test"),"explicit attestation");
  int mutations=0;
  Func<List<long>,GrantApplyResult> mutate=ids=>{mutations+=ids.Count;return GrantApplyResult.Confirmed;};
  var txn=new GrantTransaction(intent,null,ids=>{PersistedGrantState.MergeApplied(applied,123,ids);return true;},_=>{},_=>{},s,"Test");
  Check(txn.Execute(new List<long>{0},-7,123,1,mutate).Count==1,"first reward committed");
  Directory.CreateDirectory(path+".tmp");
  Check(txn.Execute(new List<long>{1},-7,123,1,mutate).Count==0,"failed second write withholds ack");
  Check(txn.Execute(new List<long>{1},-7,123,1,mutate).Count==0,"continued write failure still withholds ack");
  Check(File.Exists(intent),"intent retained until durable success");
  Directory.Delete(path+".tmp");
  Check(txn.Execute(new List<long>{1},-7,123,1,mutate).Count==1,"persist retry acknowledges once");
  Check(mutations==2,"persist retry never remutates");
  Check(s.GetPoints(-7,123,"Test")==2 && Open(path).GetPoints(-7,123,"Test")==2,"retry saves both points in memory and on disk");
  Check(!File.Exists(intent),"intent clears only after success");
 }
 static void BatchRetryAndReplay() {
  string root=Root(), path=Path.Combine(root,"enrollment.json"), applied=Path.Combine(root,"applied.json"), intent=Path.Combine(root,"intent.json");
  bool inject=true;
  var s=new EnrollmentState(path,msg=>{if(inject && msg.Contains("index=10,"))Directory.CreateDirectory(path+".tmp");},_=>{});s.Load();
  Check(s.Attest(-7,123,"Test","Disposable","test"),"batch attestation");
  int mutations=0;
  Func<List<long>,GrantApplyResult> mutate=ids=>{mutations+=ids.Count;return GrantApplyResult.Confirmed;};
  var txn=new GrantTransaction(intent,null,ids=>{PersistedGrantState.MergeApplied(applied,123,ids);return true;},_=>{},_=>{},s,"Test");
  var batch=new List<long>{10,11,12};
  Check(txn.Execute(batch,-7,123,1,mutate).Count==0,"mid-batch failure holds ack");
  Check(Open(path).GetPoints(-7,123,"Test")==1,"only first batch index is durable");
  var restarted=new GrantTransaction(intent,null,_=>true,_=>{},_=>{},Open(path),"Test");
  Check(restarted.CheckForIncompleteIntent(out _),"restart during partial batch requires recovery hold");
  inject=false;Directory.Delete(path+".tmp");
  Check(txn.Execute(batch,-7,123,1,mutate).Count==3,"owner retries remaining batch writes");
  Check(mutations==3 && Open(path).GetPoints(-7,123,"Test")==3,"batch retry no extra mutation or entitlement");
  Check(txn.Execute(batch,-7,123,1,mutate).Count==3 && mutations==3,"lost ack replay does not remutate");
  var restored=Open(path);
  Check(restored.CommitEntitlement(-7,123,"Test",1,10) && restored.GetPoints(-7,123,"Test")==3,"durable receipt retry after restart does not recount");
  Check(restored.CommitEntitlement(-7,123,"Test",2,13) && Open(path).GetPoints(-7,123,"Test")==5,"distinct later reward accumulates");
  Check(!restored.CommitEntitlement(-7,999,"Test",1,14),"wrong epoch cannot commit");
  Check(!restored.CommitEntitlement(-7,123,"Other",1,14),"wrong slot cannot commit");
 }
 static void AuthorityResolution() {
  string root=Root(), path=Path.Combine(root,"state.json");
  var legacy=new EntitlementStore(Path.Combine(root,"legacy.json"),_=>{},_=>{});legacy.Load();
  Check(legacy.Commit(-7,123,0),"test legacy record exists");
  Func<EnrollmentState,bool,EntitlementRead> resolve=(s,h)=>EnrollmentState.Resolve(s,legacy,-7,123,"Test",true,h);
  Check(resolve(null,false).Status==EntitlementStatus.Ok,"explicit legacy-only mode retained");
  Check(resolve(new EnrollmentState(path,_=>{},_=>{}),false).Status==EntitlementStatus.Unknown,"unloaded authority cannot fall back");
  var s=Open(path);
  Check(resolve(s,false).Status==EntitlementStatus.UncertainHistory,"missing authority cannot fall back");
  s.Attest(-7,123,"Test","Disposable","test");
  Check(resolve(s,false).Status==EntitlementStatus.NoRecordConfirmed,"attested no-history remains fresh");
  Check(resolve(s,true).Status==EntitlementStatus.UncertainHistory,"attested with history holds");
  Check(EnrollmentState.Resolve(s,legacy,-7,999,"Test",true,false).Status==EntitlementStatus.EpochMismatch,"wrong epoch holds");
  Check(EnrollmentState.Resolve(s,legacy,-7,123,"Other",true,false).Status==EntitlementStatus.EpochMismatch,"wrong slot holds");
  Check(EnrollmentState.Resolve(s,legacy,8,123,"Test",true,false).Status==EntitlementStatus.UncertainHistory,"unknown character holds");
  s.CommitEntitlement(-7,123,"Test",2,0);
  Check(resolve(Open(path),true).Points==2,"committed authority overrides stale legacy zero");
  File.Delete(path);
  Check(resolve(Open(path),true).Status==EntitlementStatus.UncertainHistory,"lost authority with history holds");
  foreach(string record in new[]{
    "broken", "{\"schema\":2,\"enrollments\":{\"-7\":{\"epoch\":123,\"slot\":\"Test\",\"phase\":\"committed\",\"committed_indices\":[0]}}}",
    Entry("true","[0]"),Entry("4294967296","[0]"),Entry("-1","[0]"),Entry("1","[]"),Entry("1","[0,0]"),Entry("1","[-1]"),Entry("1","[true]")}) {
   File.WriteAllText(path,record);
   var bad=Open(path);var read=resolve(bad,true);
   Check(read.Status==EntitlementStatus.StoreUnreadable,"invalid authority rejected without legacy fallback");
   var gate=new ValidationGate(new RecoveryHoldStore(Path.Combine(root,Guid.NewGuid()+".holds")));
   var snapshot=new ValidationSnapshot {Player=new object(),LoadEvent=1,Ready=true,Input=new ValidationInput {
    Identity=IdentityStatus.Verified,Guid=-7,Epoch=123,Level=1,K=2,TotalsKnown=true,Entitlement=read.Status}};
   var call=gate.Begin(snapshot,false);
   Check(!call.RunOriginal && gate.Last.Outcome==ValidationOutcome.Defer,"invalid authority cannot run vanilla or authorize grants");
   Check(!bad.CommitEntitlement(-7,123,"Test",1,99)&&File.ReadAllText(path)==record,"invalid evidence preserved on attempted commit");
  }
 }
 static string Entry(string points,string indices)=>"{\"schema\":2,\"enrollments\":{\"-7\":{\"epoch\":123,\"slot\":\"Test\",\"phase\":\"committed\",\"points\":"+points+",\"committed_indices\":"+indices+"}}}";
 static int Main() {FailedSecondWrite();BatchRetryAndReplay();AuthorityResolution();failures += CoordinatorTests.Run();Console.WriteLine($"Assertions: {checks}, failures: {failures}");return failures==0?0:1;}
}
