using System;
using CraftopiaArchipelago;
class Store:ICurrencyReceiptStore { public string Data; public string Read()=>Data; public bool CompareExchange(string e,string n){if(Data!=e)return false;Data=n;return true;} }
class Program {
 static void Check(bool v,string m){if(!v)throw new Exception(m);}
 static void Main(){
 Check(StarterKitCatalog.Items("Bow")[1].Amount==30,"bow ammo");
 Check(StarterKitCatalog.Items("MagicStaff")[0].ItemId==6392,"tier1 staff");
 Check(StarterKitCatalog.Items("invalid").Length==0,"reject unknown");
 Check(StarterKitCatalog.Items("all_basic").Length==8,"all basic plus arrows");
 var s=new Store();Check(CurrencyDelivery.InitializeFresh(s,"slot",1,123,92),"initialize item");
 var d=new CurrencyDelivery(s,"slot",1,123,92);int mutations=0;
 Check(d.Deliver(0,1,true,n=>{mutations++;return CurrencyMutation.NoMutation;})==CurrencyResult.Deferred,"full inventory");
 Check(d.Deliver(0,1,true,n=>{mutations++;return CurrencyMutation.Applied;})==CurrencyResult.WaitingForSave,"grant");
 Check(new CurrencyDelivery(s,"slot",1,123,92).Held,"interrupted intent holds");
 Check(!d.ConfirmSaved(0,"bad","slot",1,123,true),"wrong save token");
 Check(d.ConfirmSaved(0,d.MutationToken,"slot",1,123,true),"saved");
 var loaded=new CurrencyDelivery(s,"slot",1,123,92);
 Check(loaded.Deliver(0,1,true,n=>{throw new Exception("duplicate");})==CurrencyResult.Acknowledge,"restart dedup");
 Check(new CurrencyDelivery(s,"slot",1,123,44).Held,"wrong item identity");
 var p="{\"GUID\":123}"; var inv="{\"inventorySaveData\":{\"equipmentList\":[{\"itemInBox\":[{\"item\":{\"itemId\":92},\"count\":1}]}]}}";
 Check(CurrencySaveProof.Create("t",123,1,p,inv,92,"equipmentList")!=null,"weapon save proof");
 Check(CurrencySaveProof.Create("t",123,1,p,inv)==null,"currency proof still requires currency");
 Console.WriteLine("Starter kit transaction checks passed"); RuntimeTests.Run();
 }
}
