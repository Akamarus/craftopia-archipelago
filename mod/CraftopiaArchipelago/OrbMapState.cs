using System;
using System.Collections.Generic;
using System.Linq;
namespace CraftopiaArchipelago {
 public enum OrbCompletion { Available, Checked, Collected }
 public sealed class OrbMarker {
  public readonly int Guid; public readonly long Location; public readonly float X,Y,Z;
  public OrbCompletion Completion {get; internal set;}
  public OrbMarker(int guid,long location,float x,float y,float z){Guid=guid;Location=location;X=x;Y=y;Z=z;}
  internal bool Valid => Location>=2000 && Location<=2999 && Finite(X)&&Finite(Y)&&Finite(Z);
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
 public sealed class OrbMapState {
  readonly Dictionary<int,OrbMarker> markers=new Dictionary<int,OrbMarker>();
  public string Identity {get;private set;} public int Revision {get;private set;}
  public void ApplySession(string identity,IEnumerable<OrbMarker> selected){
   if(string.IsNullOrEmpty(identity)){Clear();return;}
   var next=new Dictionary<int,OrbMarker>();var locations=new HashSet<long>();
   foreach(var item in selected ?? Array.Empty<OrbMarker>()){
    if(item==null||!item.Valid||next.ContainsKey(item.Guid)||!locations.Add(item.Location))continue;
    var copy=new OrbMarker(item.Guid,item.Location,item.X,item.Y,item.Z);
    if(identity==Identity&&markers.TryGetValue(item.Guid,out var old)&&old.Location==item.Location)copy.Completion=old.Completion;
    next.Add(copy.Guid,copy);
   }
   markers.Clear();foreach(var pair in next)markers.Add(pair.Key,pair.Value);Identity=identity;Revision++;
  }
  public void ApplyCompleted(string identity,IEnumerable<long> ids){
   if(identity!=Identity||string.IsNullOrEmpty(identity))return;
   var complete=new HashSet<long>(ids??Array.Empty<long>());
   foreach(var m in markers.Values)if(complete.Contains(m.Location)&&m.Completion==OrbCompletion.Available){m.Completion=OrbCompletion.Checked;Revision++;}
  }
  public void MarkCollected(string identity,int guid){
   if(identity==Identity&&!string.IsNullOrEmpty(identity)&&markers.TryGetValue(guid,out var m)&&m.Completion!=OrbCompletion.Collected){m.Completion=OrbCompletion.Collected;Revision++;}
  }
  public IEnumerable<OrbMarker> Visible(bool hideCollected)=>markers.Values.Where(m=>!hideCollected||m.Completion==OrbCompletion.Available);
  public void Clear(){markers.Clear();Identity=null;Revision++;}
 }
}