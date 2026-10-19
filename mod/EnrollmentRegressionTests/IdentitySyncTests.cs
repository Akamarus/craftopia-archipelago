using System;
using CraftopiaArchipelago;
static class IdentitySyncTests {
 public static void Run() {
  var sync=new CharacterIdentitySync();int queued=0;
  void Send(int c,int g,long e,string s) => sync.TryQueue(c,g,e,s,()=>queued++);
  void Check(int expected,string why) { if(queued!=expected)throw new Exception(why); }
  Send(0,-7,0,"slot"); Send(1,-7,0,"slot"); Check(0,"load before epoch cannot announce identity");
  Send(1,-7,123,"slot"); Check(1,"session arrival announces loaded identity");
  Send(1,-7,123,"slot"); Check(1,"session echoed after update cannot loop");
  Send(2,-7,123,"slot"); Check(2,"reconnect announces identity once");
  Send(2,-8,123,"slot"); Check(3,"character switch announces identity");
  Send(2,-8,124,"slot"); Check(4,"new epoch announces identity");
  try { sync.TryQueue(3,-8,124,"slot",()=>throw new Exception()); } catch { }
  Send(3,-8,124,"slot"); Check(5,"failed enqueue cannot consume announcement");
  Console.WriteLine("PASS identity synchronization race and reconnect tests");
 }
}
