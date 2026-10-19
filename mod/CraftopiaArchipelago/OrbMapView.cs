using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Oc.Maps;
namespace CraftopiaArchipelago {
 internal sealed class OrbMapView : IDisposable {
  readonly Dictionary<int,OcUI_Map_Icon_FullMap> icons=new Dictionary<int,OcUI_Map_Icon_FullMap>();
  OcUI_WorldMapMng owner;OcUI_Map_Icon_FullMap prefab;RectTransform folder;
  int revision=-1;float scale=-1;bool hideCollected,active;Sprite sprite;Texture2D texture;
  static readonly FieldInfo Handler=AccessTools.Field(typeof(OcUI_WorldMapMng),"iconHandler");
  static readonly FieldInfo Prefab=AccessTools.Field(typeof(OcUI_WorldMap_IconHandler),"pf_icon");
  static readonly FieldInfo Folder=AccessTools.Field(typeof(OcUI_WorldMap_IconHandler),"iconFolder");
  static readonly FieldInfo Image=AccessTools.Field(typeof(OcUI_Map_Icon),"iconImage");
  void MakeSprite(){
   if(sprite!=null)return;texture=new Texture2D(24,24){hideFlags=HideFlags.HideAndDontSave};
   for(int y=0;y<24;y++)for(int x=0;x<24;x++){
    float d=(x-11.5f)*(x-11.5f)+(y-11.5f)*(y-11.5f);
    texture.SetPixel(x,y,d>121?Color.clear:d>81?new Color(.15f,.18f,.16f,1):Color.white);
   }
   texture.Apply();sprite=Sprite.Create(texture,new Rect(0,0,24,24),new Vector2(.5f,.5f));sprite.hideFlags=HideFlags.HideAndDontSave;
  }
  public void Refresh(OrbMapState state){
   var map=OcUI_WorldMapMng.Inst;
   bool visible=map!=null&&map.gameObject.activeInHierarchy&&Oc.OcPlMng.LocalPl!=null&&Oc.Maps.OcWorldManager.Inst!=null&&!Oc.Maps.OcWorldManager.Inst.IsLoading;
   if(owner!=map){ClearIcons();owner=map;prefab=null;folder=null;}
   if(!visible){active=false;return;}active=true;
   if(folder==null){var handler=Handler.GetValue(owner);if(handler==null)return;prefab=Prefab.GetValue(handler) as OcUI_Map_Icon_FullMap;folder=Folder.GetValue(handler) as RectTransform;}
   if(prefab==null||folder==null)return;
   if(revision!=state.Revision){
    MakeSprite();var keep=new HashSet<int>();
    foreach(var marker in state.Visible(hideCollected)){
     keep.Add(marker.Guid);
     if(!icons.TryGetValue(marker.Guid,out var icon)||icon==null){icon=UnityEngine.Object.Instantiate(prefab,folder,false);icon.gameObject.name="AP Soul Orb "+marker.Guid;icons[marker.Guid]=icon;}
     icon.SetIcon(sprite);icon.SetPosition(new Vector2(marker.X,marker.Z));icon.SetSize(new Vector2(18,18));
     var img=Image.GetValue(icon) as UnityEngine.UI.Image;
     if(img!=null){img.color=marker.Completion==OrbCompletion.Available?new Color(1,.78f,.16f,1):new Color(.52f,.55f,.55f,1);img.raycastTarget=true;}
     string status=marker.Completion==OrbCompletion.Collected?"Collected":marker.Completion==OrbCompletion.Checked?"AP check complete (not collected here)":"Uncollected";
     icon.SetMassageTag("Soul Orb",status);icon.SetActiveIfRequired(true);
    }
    var remove=new List<int>();foreach(var pair in icons)if(!keep.Contains(pair.Key)){if(pair.Value!=null)UnityEngine.Object.Destroy(pair.Value.gameObject);remove.Add(pair.Key);}
    foreach(var id in remove)icons.Remove(id);revision=state.Revision;scale=-1;
   }
   float current=owner.CurrentScale;
   if(current>0&&current!=scale){foreach(var icon in icons.Values)if(icon!=null)icon.SetScale(new Vector3(1/current,1/current,1));scale=current;}
  }
  public void DrawToggle(){
   if(!active)return;
   bool next=GUI.Toggle(new Rect(24,96,220,30),hideCollected,"Hide collected AP soul orbs");
   if(next!=hideCollected){hideCollected=next;revision=-1;}
  }
  void ClearIcons(){foreach(var icon in icons.Values)if(icon!=null)UnityEngine.Object.Destroy(icon.gameObject);icons.Clear();revision=-1;scale=-1;active=false;}
  public void Dispose(){ClearIcons();owner=null;folder=null;prefab=null;if(sprite!=null)UnityEngine.Object.Destroy(sprite);if(texture!=null)UnityEngine.Object.Destroy(texture);sprite=null;texture=null;}
 }
}