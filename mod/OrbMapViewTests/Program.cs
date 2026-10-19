using System;
using CraftopiaArchipelago;
using Oc.Maps;
using UnityEngine;
class Program {
 static void A(bool v,string s){if(!v)throw new Exception(s);}
 static void Main(){
  var state=new OrbMapState();state.ApplySession("seed",new[]{new OrbMarker(1,2001,123,45,-789),new OrbMarker(2,2002,456,10,-222)});
  var view=new OrbMapView();view.Refresh(state);var made=UnityEngine.Object.Created;
  A(made.Count==2,"all selected markers created without exploration");
  A(made[0].Parent==OcUI_WorldMapMng.Inst.iconHandler.iconFolder,"native pan/offset parent");
  A(made[0].Position.x==123&&made[0].Position.y==-789,"native X/Z projection");
  A(made[0].Scale.x==.5f,"inverse zoom");view.Refresh(state);A(made.Count==2,"cached markers reused");
  OcUI_WorldMapMng.Inst.gameObject.activeInHierarchy=false;view.Refresh(state);
  OcUI_WorldMapMng.Inst.gameObject.activeInHierarchy=true;OcUI_WorldMapMng.Inst.CurrentScale=4;view.Refresh(state);
  A(made.Count==2&&made[0].Scale.x==.25f,"reopen and zoom reuse");
  state.ApplyCompleted("seed",new long[]{2001});view.Refresh(state);
  A(made.Count==2&&made[0].Status.Contains("not collected here")&&made[0].iconImage.color.r<.6f,"release grey without fake pickup");
  GUI.Next=true;view.DrawToggle();view.Refresh(state);A(made[0].gameObject.Destroyed&&!made[1].gameObject.Destroyed,"hide completed only");
  OcUI_WorldMapMng.Inst=new OcUI_WorldMapMng();view.Refresh(state);A(made[1].gameObject.Destroyed&&made.Count==3,"scene replacement cleans old markers");
  state.ApplySession("other",Array.Empty<OrbMarker>());view.Refresh(state);A(made[2].gameObject.Destroyed,"profile clears old markers");
  view.Dispose();Console.WriteLine("OrbMapViewTests passed");
 }
}
