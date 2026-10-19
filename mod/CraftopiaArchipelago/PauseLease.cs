using System;
namespace CraftopiaArchipelago {
 public sealed class PauseLease : IDisposable {
  readonly Func<float> read;readonly Action<float> write;readonly Action acquire,release;
  bool opened,ownsTime;float previous;
  public PauseLease(Func<float> read,Action<float> write,Action acquire,Action release){this.read=read;this.write=write;this.acquire=acquire;this.release=release;}
  public void Open(bool pause){
   if(opened)return;acquire();opened=true;
   try{if(pause){previous=read();if(previous>0){write(0);ownsTime=true;}}}catch{Close();throw;}
  }
  // Native real-time effects may finish while the guide owns the pause.
  // Keep that new rate for restoration, but do not resume gameplay behind the guide.
  public void Maintain(){if(opened&&ownsTime){float current=read();if(current>0){previous=current;write(0);}}}
  public void Close(){
   if(!opened)return;opened=false;
   try{if(ownsTime&&read()==0)write(previous);}finally{ownsTime=false;release();}
  }
  public void Dispose()=>Close();
 }
}