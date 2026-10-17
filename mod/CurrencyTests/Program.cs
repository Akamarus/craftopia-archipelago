using System;
using System.IO;
using CraftopiaArchipelago;

sealed class MemoryStore : ICurrencyReceiptStore {
    public string Data; public bool Fail;
    public string Read() => Data;
    public bool CompareExchange(string expected, string next) {
        if(Fail || expected != Data) return false;
        Data=next; return true;
    }
}
class Program {
    static int assertions;
    static void Check(bool ok, string why) { assertions++; if(!ok) throw new Exception(why); }
    static CurrencyDelivery Fresh(MemoryStore store) {
        Check(CurrencyDelivery.InitializeFresh(store,"seed-slot",42,-123),"fresh initialize");
        return new CurrencyDelivery(store,"seed-slot",42,-123);
    }
    static void Main() {
        var marker=Path.Combine(Path.GetTempPath(),"orb-binding-"+Guid.NewGuid());
        try {
            Check(!SoulOrbCharacterBinding.RequiresSession(marker),"ordinary character has no binding");
            Check(SoulOrbCharacterBinding.Mark(marker,"seed:slot:epoch"),"durable character marker");
            Check(SoulOrbCharacterBinding.RequiresSession(marker),"restart requires session before collecting");
            Check(SoulOrbCharacterBinding.Matches(marker,"seed:slot:epoch"),"matching session restores binding");
            Check(!SoulOrbCharacterBinding.Matches(marker,"other:slot:epoch"),"wrong seed cannot release binding");
            Check(!SoulOrbCharacterBinding.Mark(marker,"other:slot:epoch"),"wrong seed cannot overwrite binding");
            File.WriteAllText(marker,"");
            Check(SoulOrbCharacterBinding.RequiresSession(marker),"corrupt marker still blocks vanilla");
            Check(!SoulOrbCharacterBinding.Mark(marker,"seed:slot:epoch"),"corrupt binding not repaired automatically");
            Check(File.ReadAllText(marker)=="","marker not overwritten");
        } finally { if(File.Exists(marker)) File.Delete(marker); }
        int writes=0, finishes=0;
        Func<bool> persist=()=>{writes++; return true;};
        Action finish=()=>{Check(writes>0,"orb persistence precedes collection"); finishes++;};
        Check(SoulOrbReplacement.Collect(false,true,false,false,true,persist,finish),"unbound vanilla");
        Check(SoulOrbReplacement.Collect(true,false,false,false,true,persist,finish),"unselected vanilla");
        Check(SoulOrbReplacement.Collect(true,true,true,false,true,persist,finish),"historical orb vanilla repeat reward");
        Check(!SoulOrbReplacement.Collect(true,true,false,true,true,persist,finish),"already collected cannot repeat currency");
        Check(!SoulOrbReplacement.Collect(true,true,false,false,false,persist,finish),"ineligible orb stays uncollected");
        Check(writes==0 && finishes==0,"no side effects before eligibility");
        Check(!SoulOrbReplacement.Collect(true,true,false,false,true,()=>false,finish) && finishes==0,"failed outbox blocks collection");
        Check(!SoulOrbReplacement.Collect(true,true,false,false,true,()=>throw new IOException(),finish),"outbox exception blocks vanilla");
        Check(!SoulOrbReplacement.Collect(true,true,false,false,true,persist,finish) && finishes==1,"selected orb replaces vanilla");
        Check(!SoulOrbReplacement.Collect(true,true,false,false,true,persist,()=>throw new Exception()),"finish exception cannot restore vanilla reward");
        Check(CurrencySaveProof.SameSavePath(Path.GetFullPath("save.db"),Path.GetFullPath("save.db").Replace('\\','/')),"mixed separator save paths match");
        Check(!CurrencySaveProof.SameSavePath(Path.GetFullPath("save.db"),Path.GetFullPath("other.db")),"different save rejected");
        Check(!CurrencySaveProof.SameSavePath(null,Path.GetFullPath("save.db")),"missing path rejected");
        string player="{\"GUID\":-123}";
        string inventoryJson="{\"inventorySaveData\":{\"importantList\":[{\"itemInBox\":[{\"item\":{\"itemId\":3840},\"count\":2}]}]}}";
        var proof=CurrencySaveProof.Create("token",-123,2,player,inventoryJson);
        Check(proof!=null,"post-grant snapshot accepted");
        Check(proof.Matches("token",-123,(p,i)=>p==player && i==inventoryJson),"exact saved records match");
        Check(!proof.Matches("old",-123,(p,i)=>true),"old mutation token rejected");
        Check(!proof.Matches("token",123,(p,i)=>true),"other character rejected");
        Check(!proof.Matches("token",-123,(p,i)=>false),"unsaved snapshot rejected");
        Check(!proof.Matches("token",-123,(p,i)=>throw new IOException()),"read failure rejects proof");
        Check(CurrencySaveProof.Create("token",-123,3,player,inventoryJson)==null,"pre-grant count rejected");
        Check(CurrencySaveProof.Create("token",123,2,player,inventoryJson)==null,"snapshot character mismatch");
        Check(CurrencySaveProof.Create("token",-123,2,player,inventoryJson.Replace(":2",":2.0"))==null,"fractional stack rejected");
        Check(CurrencySaveProof.Create("token",-123,2,player,"{}")==null,"missing inventory rejected");
        Check(CurrencySaveProof.Create("",-123,2,player,inventoryJson)==null,"missing mutation token rejected");
        int inventory=0;
        Check(CurrencyInventoryVerifier.Apply(1,()=>inventory,n=> {inventory+=n; return 0;})==CurrencyMutation.Applied,"exact inventory increase");
        Check(CurrencyInventoryVerifier.Apply(1,()=>inventory,n=>n)==CurrencyMutation.NoMutation,"full inventory keeps pending");
        Check(CurrencyInventoryVerifier.Apply(2,()=>inventory,n=> {inventory++; return 1;})==CurrencyMutation.Ambiguous,"partial insertion holds");
        Check(CurrencyInventoryVerifier.Apply(1,()=>inventory,n=>0)==CurrencyMutation.Ambiguous,"false success holds");
        Check(CurrencyInventoryVerifier.Apply(1,()=>throw new Exception(),n=>throw new Exception())==CurrencyMutation.NoMutation,"unreadable before mutation defers");
        Check(CurrencyInventoryVerifier.Apply(1,()=>inventory,n=>throw new Exception())==CurrencyMutation.Ambiguous,"insert exception holds");
        var store=new MemoryStore(); var d=Fresh(store); int mutations=0;
        Func<int,CurrencyMutation> apply=n=> { mutations++; return CurrencyMutation.Applied; };
        Check(d.Deliver(0,1,false,apply)==CurrencyResult.Deferred,"gate blocks");
        Check(mutations==0,"no mutation while gated");
        Check(d.Deliver(0,1,true,apply)==CurrencyResult.WaitingForSave,"no early ack");
        Check(d.Deliver(0,1,true,apply)==CurrencyResult.WaitingForSave && mutations==1,"retry never mutates twice");
        Check(d.Deliver(1,1,true,apply)==CurrencyResult.Deferred,"pending batch not overwritten");
        Check(!d.ConfirmSaved(0,d.MutationToken,"other",42,-123,true),"wrong slot proof");
        Check(!d.ConfirmSaved(0,d.MutationToken,"seed-slot",42,123,true),"wrong character proof");
        Check(!d.ConfirmSaved(0,d.MutationToken,"seed-slot",43,-123,true),"wrong epoch proof");
        Check(!d.ConfirmSaved(0,"stale","seed-slot",42,-123,true),"stale proof");
        Check(!d.ConfirmSaved(0,d.MutationToken,"seed-slot",42,-123,false),"failed save proof");
        store.Fail=true;
        Check(!d.ConfirmSaved(0,d.MutationToken,"seed-slot",42,-123,true),"commit failure withholds ack");
        store.Fail=false;
        Check(d.ConfirmSaved(0,d.MutationToken,"seed-slot",42,-123,true),"retry commit");
        Check(d.Deliver(0,1,true,apply)==CurrencyResult.Acknowledge && mutations==1,"committed duplicate");
        d=new CurrencyDelivery(store,"seed-slot",42,-123);
        Check(d.Deliver(0,1,true,apply)==CurrencyResult.Acknowledge && mutations==1,"restart dedup");
        Check(d.Deliver(0,2,true,apply)==CurrencyResult.Hold,"changed receipt payload held");
        Check(new CurrencyDelivery(store,"seed-slot",43,-123).Held,"identity mismatch held");

        store=new MemoryStore(); d=Fresh(store); store.Fail=true;
        Check(d.Deliver(0,1,true,apply)==CurrencyResult.Hold && mutations==1,"intent failure no mutation");
        store=new MemoryStore(); d=Fresh(store);
        Check(d.Deliver(1,1,true,n=>CurrencyMutation.NoMutation)==CurrencyResult.Deferred,"full inventory deferred");
        Check(!new CurrencyDelivery(store,"seed-slot",42,-123).Held,"no-mutation intent cleared");
        Check(d.Deliver(1,1,true,n=>CurrencyMutation.Ambiguous)==CurrencyResult.Hold,"ambiguous hold");
        Check(new CurrencyDelivery(store,"seed-slot",42,-123).Held,"restart holds intent");
        Check(!CurrencyDelivery.InitializeFresh(store,"seed-slot",42,-123),"never reset history");
        store=new MemoryStore(); d=Fresh(store);
        Check(d.Deliver(0,1,true,n=>throw new Exception())==CurrencyResult.Hold,"mutation exception hold");
        Check(new CurrencyDelivery(new MemoryStore(),"seed-slot",42,-123).Held,"missing ledger hold");
        Check(new CurrencyDelivery(new MemoryStore{Data="{}"},"seed-slot",42,-123).Held,"corrupt ledger hold");
        store=new MemoryStore(); d=Fresh(store);
        store.Data=store.Data.Replace("\"epoch\":42", "\"epoch\":42.5");
        Check(new CurrencyDelivery(store,"seed-slot",42,-123).Held,"fractional epoch held");
        store=new MemoryStore(); d=Fresh(store); store.Data=null;
        Check(d.Deliver(3,1,true,apply)==CurrencyResult.Hold,"deleted file held");
        store=new MemoryStore(); d=Fresh(store);
        Check(d.Deliver(-1,1,true,apply)==CurrencyResult.Hold,"invalid receipt rejected");
        store=new MemoryStore(); d=Fresh(store);
        Check(d.Deliver(4,0,true,apply)==CurrencyResult.Hold,"zero amount rejected");
        store=new MemoryStore(); d=Fresh(store);
        Check(d.Deliver(4,1,true,apply)==CurrencyResult.WaitingForSave,"prepare duplicate gate test");
        Check(d.ConfirmSaved(4,d.MutationToken,"seed-slot",42,-123,true),"commit duplicate gate test");
        Check(d.Deliver(4,1,false,apply)==CurrencyResult.Deferred,"duplicate ack requires eligibility");
        store.Data="{}";
        Check(d.Deliver(4,1,true,apply)==CurrencyResult.Hold,"changed file blocks duplicate ack");
        int reads=0;
        Check(CurrencyInventoryVerifier.Apply(1,()=> ++reads==1 ? 0 : throw new IOException(),n=>0)==CurrencyMutation.Ambiguous,"readback failure holds after mutation");

        var path=Path.Combine(Path.GetTempPath(),"currency-test-"+Guid.NewGuid()+".json");
        try {
            using(var file=new CurrencyReceiptFile(path)) {
                bool exclusive=false;
                try { using(var second=new CurrencyReceiptFile(path)) {} }
                catch(IOException) { exclusive=true; }
                Check(exclusive,"second file owner refused");
                Check(CurrencyDelivery.InitializeFresh(file,"slot",5,6),"disk initialization");
                Check(!CurrencyDelivery.InitializeFresh(file,"slot",5,6),"disk no overwrite");
                d=new CurrencyDelivery(file,"slot",5,6);
                Check(d.Deliver(2,1,true,n=>CurrencyMutation.Applied)==CurrencyResult.WaitingForSave,"disk intent");
                Check(d.ConfirmSaved(2,d.MutationToken,"slot",5,6,true),"disk commit");
            }
            using(var file=new CurrencyReceiptFile(path))
                Check(new CurrencyDelivery(file,"slot",5,6).Deliver(2,1,true,apply)==CurrencyResult.Acknowledge,"disk reload dedup");
        } finally { foreach(var p in new[]{path,path+".lock",path+".tmp"}) if(File.Exists(p)) File.Delete(p); }
        Console.WriteLine($"CurrencyTests: {assertions} assertions passed");
    }
}
