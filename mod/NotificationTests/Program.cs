using System;
using CraftopiaArchipelago;
class Program {
 static void Assert(bool value,string name) { if(!value) throw new Exception(name); }
 static void Main() {
  var d=new PlayerStatusDisplay(null,_=>throw new Exception("logger unavailable"));
  try { d.ShowBlockedAction("Requires Tamer"); } catch { throw new Exception("Blocked notice must contain logger exceptions"); }
  d=new PlayerStatusDisplay(null,null);
  var show=typeof(PlayerStatusDisplay).GetMethod("Notify");
  Assert(show!=null,"Missing transient notification queue");
  show.Invoke(d,new object[]{"check:1","Check queued: <b>Axe</b>",10d});
  show.Invoke(d,new object[]{"check:1","Duplicate",11d});
  var read=typeof(PlayerStatusDisplay).GetMethod("VisibleMessages");
  string result=(string)read.Invoke(d,new object[]{11d});
  Assert(result.Contains("Check queued") && !result.Contains("Duplicate") && !result.Contains("<b>"),"dedup and plain text");
  Assert((string)read.Invoke(d,new object[]{30d})=="","messages expire");
  for(int i=0;i<100;i++) show.Invoke(d,new object[]{"burst"+i,"Message"+i,40d});
  Assert(((string)read.Invoke(d,new object[]{41d})).Split('\n',StringSplitOptions.RemoveEmptyEntries).Length<=3,"bounded visible messages");
  Console.WriteLine("Notification tests PASS");
 }
}