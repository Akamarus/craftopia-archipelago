using System.Collections.Generic;
using System.Reflection;
namespace HarmonyLib { public static class AccessTools {public static FieldInfo Field(System.Type t,string n)=>t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);}}
namespace UnityEngine {
 public enum HideFlags{HideAndDontSave}
 public class Object {
  public static List<Oc.Maps.OcUI_Map_Icon_FullMap> Created=new();
  public static T Instantiate<T>(T source,RectTransform parent,bool local) where T:Object {
   var icon=new Oc.Maps.OcUI_Map_Icon_FullMap{Parent=parent};Created.Add(icon);return (T)(Object)icon;
  }
  public static void Destroy(Object o){if(o is GameObject g)g.Destroyed=true;}
 }
 public class GameObject:Object{public bool activeInHierarchy=true,Destroyed;public string name;}
 public class RectTransform:Object{}
 public struct Vector2 {public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public struct Rect{public Rect(float a,float b,float c,float d){}}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color clear=>new(0,0,0,0);public static Color white=>new(1,1,1,1);}
 public class Texture2D:Object{public HideFlags hideFlags;public Texture2D(int w,int h){}public void SetPixel(int x,int y,Color c){}public void Apply(){}}
 public class Sprite:Object{public HideFlags hideFlags;public static Sprite Create(Texture2D t,Rect r,Vector2 v)=>new();}
 public static class GUI{public static bool? Next;public static bool Toggle(Rect r,bool v,string s){var n=Next??v;Next=null;return n;}}
}
namespace UnityEngine.UI{public class Image{public UnityEngine.Color color;public bool raycastTarget;}}
namespace Oc{public static class OcPlMng{public static object LocalPl=new();}}
namespace Oc.Maps {
 public class OcWorldManager {public static OcWorldManager Inst=new();public bool IsLoading;}
 public class OcUI_Map_Icon:UnityEngine.Object {public UnityEngine.UI.Image iconImage=new();}
 public class OcUI_Map_Icon_FullMap:OcUI_Map_Icon {
  public UnityEngine.GameObject gameObject=new();public UnityEngine.RectTransform Parent;public UnityEngine.Vector2 Position;public UnityEngine.Vector3 Scale;public string Status;
  public void SetIcon(UnityEngine.Sprite s){}public void SetPosition(UnityEngine.Vector2 p)=>Position=p;public void SetSize(UnityEngine.Vector2 s){}
  public void SetMassageTag(string t,string m)=>Status=m;public void SetActiveIfRequired(bool v)=>gameObject.activeInHierarchy=v;public void SetScale(UnityEngine.Vector3 s)=>Scale=s;
 }
 public class OcUI_WorldMap_IconHandler{public OcUI_Map_Icon_FullMap pf_icon=new();public UnityEngine.RectTransform iconFolder=new();}
 public class OcUI_WorldMapMng {public static OcUI_WorldMapMng Inst=new();public UnityEngine.GameObject gameObject=new();public OcUI_WorldMap_IconHandler iconHandler=new();public float CurrentScale=2;}
}
