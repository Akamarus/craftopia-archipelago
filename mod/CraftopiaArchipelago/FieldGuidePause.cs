using System;
using UnityEngine;
namespace CraftopiaArchipelago {
 internal sealed class FieldGuidePause : IDisposable {
  PauseLease lease;object world,player;bool opened;Oc.OcInput.OcInputTrack input;
  readonly object key=new object();
  public string Message {get;private set;}="";
  public void SetOpen(bool open){
   if(!open){Dispose();return;}if(opened)return;opened=true;
   var manager=Oc.Maps.OcWorldManager.Inst;
   player=Oc.OcPlMng.LocalPl;world=manager?.CurrentWorld;
   if(Oc.OcPlMng.LocalPl==null||manager==null||manager.IsLoading||Oc.OcGameSceneShareSingleton.IsOpening){Message="";return;}
   input=Oc.OcInput.OcInputTrack.Inst;if(input==null){Dispose();throw new InvalidOperationException("Game input unavailable");}
   world=manager.CurrentWorld;
   bool pause=Oc.Network.OcNetMng.Inst!=null&&!Oc.Network.OcNetMng.Inst.IsMultiPlay;
   var owner=input;
   lease=new PauseLease(()=>Time.timeScale,v=>Time.timeScale=v,
     ()=>owner.DisableAllInputWithKey(key),()=>{if(owner!=null)owner.TryEnableInputWithKey(key,.1f);});
   try{lease.Open(pause);Message=pause?"Game paused":"Multiplayer: world remains live";}
   catch{Dispose();throw;}
  }
  public void Maintain()=>lease?.Maintain();
  public bool WorldChanged(){
   if(!opened)return false;
   var manager=Oc.Maps.OcWorldManager.Inst;
   return !ReferenceEquals(player,Oc.OcPlMng.LocalPl)||!ReferenceEquals(world,manager?.CurrentWorld)||
    (lease!=null&&(manager==null||manager.IsLoading||Oc.OcGameSceneShareSingleton.IsOpening));
  }
  public void Dispose(){var old=lease;lease=null;opened=false;world=null;player=null;input=null;Message="";old?.Dispose();}
 }
}