using System;
using System.IO;
using CraftopiaArchipelago;
static class ManagedSessionTests {
 public static void Run() {
  void Check(bool value,string why) { if(!value)throw new Exception(why); }
  var path=Path.Combine(AppContext.BaseDirectory,"ap-equipment-"+Guid.NewGuid()+".json");
  var s=EquipmentSession.Managed("slot",path);
  Check(s.Bind(50,"slot",EquipmentCatalog.CatalogId,1,0),"valid contract can initialize before enrollment");
  Check(!s.ContractReady && !s.Ready(-123,50,"slot",true),"unapproved contract cannot deliver");
  Check(!s.Approve(-123,51,"slot"),"wrong epoch cannot approve");
  Check(s.Approve(-123,50,"slot"),"exact enrollment persists approval");
  Check(s.ContractReady && s.Ready(-123,50,"slot",true),"approved exact identity can deliver");
  Check(!s.Ready(123,50,"slot",true),"another character blocked");
  var resumed=EquipmentSession.Managed("slot",path);
  Check(resumed.Bind(50,"slot",EquipmentCatalog.CatalogId,1,0) && resumed.Ready(-123,50,"slot",true),"approval survives reload");
  Check(!resumed.Bind(51,"slot",EquipmentCatalog.CatalogId,1,0),"conflicting seed refused");
  Check(!resumed.Bind(50,"slot",EquipmentCatalog.CatalogId,1,0),"conflict latches hold");
  var partial=path+"-partial"; File.WriteAllText(partial+".tmp","incomplete");
  Check(!EquipmentSession.Managed("slot",partial).Bind(50,"slot",EquipmentCatalog.CatalogId,1,0),"partial approval holds");
  foreach(var bad in new[]{"1.5","true","null","0"}) {
   File.WriteAllText(path,"{\"schema\":"+bad+",\"guid\":-123,\"epoch\":50,\"slot\":\"slot\",\"catalog\":\""+EquipmentCatalog.CatalogId+"\"}");
   Check(!EquipmentSession.Managed("slot",path).Bind(50,"slot",EquipmentCatalog.CatalogId,1,0),"malformed schema refused");
  }
  File.WriteAllText(path,"bad");
  Check(!EquipmentSession.Managed("slot",path).Bind(50,"slot",EquipmentCatalog.CatalogId,1,0),"corrupt approval holds");
  Console.WriteLine("PASS managed equipment persistence and binding");
 }
}
