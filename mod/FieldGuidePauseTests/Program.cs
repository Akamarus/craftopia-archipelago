using System;
using CraftopiaArchipelago;
class Program{
 static void A(bool v,string s){if(!v)throw new Exception(s);}
 static void Main(){float t=.5f;int locks=0;var p=new PauseLease(()=>t,v=>t=v,()=>locks++,()=>locks--);
 p.Open(true);p.Open(true);A(t==0&&locks==1,"idempotent pause and input lock");p.Close();p.Close();A(t==.5f&&locks==0,"restore prior speed once");
 t=0;p.Open(true);p.Close();A(t==0,"prepaused stays paused");
 t=1;p.Open(false);A(t==1&&locks==1,"multiplayer input only");p.Close();
 p.Open(true);t=.25f;p.Close();A(t==.25f,"external rate preserved");
 p.Open(true);p.Dispose();A(t==.25f&&locks==0,"cleanup releases ownership");
 t=1;p.Open(true);t=.75f;p.Maintain();A(t==0,"native time effect cannot leave guide running");
 t=.5f;p.Maintain();p.Close();A(t==.5f,"latest native rate restored");
 t=0;p.Open(true);t=.25f;p.Maintain();p.Close();A(t==.25f,"prepaused lease does not take time ownership");
 p.Open(false);p.Maintain();A(t==.25f,"multiplayer maintenance never freezes time");p.Close();
 var bad=new PauseLease(()=>t,v=>t=v,()=>throw new Exception(),()=>{});try{bad.Open(true);}catch{} A(t==.25f,"failed input acquisition cannot freeze");

 var input=new Oc.OcInput.OcInputTrack();Oc.OcInput.OcInputTrack.Inst=input;
 Oc.Network.OcNetMng.Inst=new Oc.Network.OcNetMng();var guide=new FieldGuidePause();
 guide.SetOpen(true);A(input.Locks==0,"main menu must not take game input");
 Oc.OcPlMng.LocalPl=new object();Oc.Maps.OcWorldManager.Inst=new Oc.Maps.OcWorldManager{CurrentWorld=new object()};
 A(guide.WorldChanged(),"loading a world from menu closes guide");guide.SetOpen(false);
 guide.SetOpen(true);A(UnityEngine.Time.timeScale==0&&input.Locks==1,"runtime pause");
 Oc.Maps.OcWorldManager.Inst.IsLoading=true;A(guide.WorldChanged(),"loading closes pause");guide.Dispose();A(UnityEngine.Time.timeScale==1&&input.Locks==0,"runtime cleanup");
 Oc.Maps.OcWorldManager.Inst.IsLoading=false;Oc.Network.OcNetMng.Inst.IsMultiPlay=true;
 guide.SetOpen(true);A(UnityEngine.Time.timeScale==1&&input.Locks==1,"remote world not paused");guide.Dispose();
 Oc.OcInput.OcInputTrack.Inst=null;
 try{guide.SetOpen(true);}catch(InvalidOperationException){}
 Oc.OcInput.OcInputTrack.Inst=input;guide.SetOpen(true);
 A(input.Locks==1,"failed opening can be retried");guide.Dispose();
 Console.WriteLine("FieldGuidePauseTests passed"); }
}