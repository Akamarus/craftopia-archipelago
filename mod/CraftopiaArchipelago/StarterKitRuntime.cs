using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace CraftopiaArchipelago {
 // Main-thread only. No authority to initialize a receipt outside fresh enrollment.
 internal static class StarterKitRuntime {
  static string seed,slot,choice; static long epoch; static int connection,guid,position;
  static bool enabled,pointQueued,changed; static string lastNotice; static StarterKitItem[] items=Array.Empty<StarterKitItem>();
  static CurrencyReceiptFile file; static CurrencyDelivery delivery; static StarterKitSaveObserver observer;
  static double nextSave,saveDeadline; static bool saveTimedOut;
  static bool Number(Dictionary<string,object> m,string k,long v)=>m.TryGetValue(k,out var raw)&&raw is long n&&n==v;
  static void Notice(string message){
   Plugin.StatusDisplay?.Transition("starter-kit",message);
   if(message!=lastNotice&&!message.StartsWith("Saving starter kit:"))Plugin.StatusDisplay?.Notify("starter-kit:"+message,message,PlayerStatusDisplay.Now);
   lastNotice=message;
  }
  internal static void Suspend(){file?.Dispose();file=null;delivery=null;observer=null;StarterKitSaveObserver.Cancel();guid=0;position=0;pointQueued=false;changed=false;lastNotice=null;saveTimedOut=false;nextSave=0;saveDeadline=0;}
  internal static void ResetSession(){Suspend();enabled=false;seed=null;slot=null;choice=null;epoch=0;}
  internal static void ApplySession(Dictionary<string,object> m,int c){
   string newSeed=MiniJson.GetString(m,"seed_name"),newSlot=MiniJson.GetString(m,"slot"),newChoice=MiniJson.GetString(m,"starter_choice");
   bool valid=Number(m,"starter_kit_protocol",1)&&Number(m,"multiplayer_mode",0)&&Number(m,"epoch",Plugin.Validator.CurrentEpoch)&&
    c==Plugin.Bridge.ConnectionId&&newSlot==Plugin.SlotName.Value&&!string.IsNullOrWhiteSpace(newSeed)&&StarterKitCatalog.Items(newChoice).Length>0;
   if(!valid||seed!=newSeed||slot!=newSlot||choice!=newChoice||epoch!=Plugin.Validator.CurrentEpoch)Suspend();
   enabled=valid;seed=newSeed;slot=newSlot;choice=newChoice;epoch=Plugin.Validator.CurrentEpoch;connection=c;items=StarterKitCatalog.Items(choice);
  }
  static string StatePath(int character,int id){
   using(var hash=SHA256.Create()){
    var identity=MiniJson.Stringify(new Dictionary<string,object>{{"seed",seed},{"slot",slot},{"epoch",epoch},{"guid",(long)character},{"choice",choice},{"item",(long)id}});
    return Path.Combine(UserStorage.Root,"craftopia-starter-"+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-","").ToLowerInvariant()+".json");
   }
  }
  internal static void AttestNew(int character){
   if(!enabled)return;
   try{foreach(var item in items)using(var fresh=new CurrencyReceiptFile(StatePath(character,item.ItemId)))
    if(!CurrencyDelivery.InitializeFresh(fresh,slot,epoch,character,item.ItemId))throw new IOException();
   }catch{Notice("Starter kit enrollment could not be saved; delivery held for review.");}
  }
  internal static void Pump(bool eligible){
   if(!enabled||!eligible||Plugin.Bridge==null||!Plugin.Bridge.IsConnected||connection!=Plugin.Bridge.ConnectionId||epoch!=Plugin.Validator.CurrentEpoch)return;
   var player=Oc.OcPlMng.LocalPl;
   if(player==null||player.GUID==0||Plugin.Validator.IsEnrolled==null||!Plugin.Validator.IsEnrolled(player.GUID))return;
   try{
    if(guid!=player.GUID){Suspend();guid=player.GUID;}
    if(position>=items.Length){
     if(!pointQueued&&(choice=="MagicStaff"||choice=="all_basic")){
      Plugin.Pipeline.EnqueueInbound(epoch,new List<long>{StarterKitCatalog.SkillPointReceipt});pointQueued=true;
     }
     return;
    }
    var item=items[position];
    if(file==null){file=new CurrencyReceiptFile(StatePath(guid,item.ItemId));delivery=new CurrencyDelivery(file,slot,epoch,guid,item.ItemId);}
    if(delivery.Held){Notice("Starter kit delivery held for review; preserve its receipt files.");return;}
    // One unit per receipt means a nearly full stack cannot partially consume a batch.
    // Weapons have one receipt; arrows use thirty single-unit receipts.
    for(int unit=0;unit<item.Amount;unit++){
     var outcome=delivery.Deliver(unit,1,true,n=>StarterKitInventoryAdapter.TryApply(item.ItemId,n));
     if(outcome==CurrencyResult.Acknowledge)continue;
     if(outcome==CurrencyResult.WaitingForSave){
      changed=true;
      if(saveTimedOut){Notice("Starter kit save could not be confirmed; delivery paused for review.");return;}
      if(saveDeadline==0)saveDeadline=PlayerStatusDisplay.Now+10;
      if(observer==null)observer=StarterKitSaveObserver.Track(delivery.MutationToken,guid,StarterKitInventoryAdapter.Count(item.ItemId),item.ItemId);
      if(observer!=null&&observer.Verify(delivery.MutationToken,guid)&&delivery.ConfirmSaved(unit,delivery.MutationToken,slot,epoch,guid,true)){
       observer=null;saveDeadline=0;StarterKitSaveObserver.Cancel();continue;
      }
      if(PlayerStatusDisplay.Now>=saveDeadline){saveTimedOut=true;Notice("Starter kit save could not be confirmed; delivery paused for review.");return;}
      if(PlayerStatusDisplay.Now>=nextSave){nextSave=PlayerStatusDisplay.Now+1;Oc.OcSaveManager.Inst.SaveGameData(false,false);}
      Notice("Saving starter kit: "+item.Name);return;
     }
     Notice(outcome==CurrencyResult.Deferred?"Starter kit waiting for inventory space.":"Starter kit delivery held for review; preserve its receipt files.");return;
    }
    if(changed)Notice("Starter kit delivered: "+item.Name);changed=false;file.Dispose();file=null;delivery=null;position++;
   }catch{Notice("Starter kit delivery could not be verified; no repeat grant attempted.");}
  }
 }
}
