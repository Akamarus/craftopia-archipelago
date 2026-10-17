using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
class Program
{
 static void Assert(bool yes,string name){if(!yes)throw new Exception(name);}
 static void Main(){
  var t=Assembly.GetExecutingAssembly().GetType("CraftopiaArchipelago.ObservationJournal");
  Assert(t!=null,"durable journal missing");
  string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{
   string path=Path.Combine(dir,"checks.log");
   dynamic j=Activator.CreateInstance(t,path);
   string m="{\"cmd\":\"check\",\"epoch\":7,\"slot\":\"Test\",\"mission_id\":50,\"observation_id\":\"a\"}";
   Assert(j.Append(m),"append persisted");Assert(j.Append(m),"duplicate idempotent");
   Assert(((List<string>)j.Snapshot(7L,"Test")).Count==1,"deduplicated snapshot");
   j=Activator.CreateInstance(t,path);
   Assert(((List<string>)j.Snapshot(8L,"Test")).Count==0,"other epoch not replayed");
   Assert(((List<string>)j.Snapshot(7L,"Other")).Count==0,"other slot not replayed");
   Assert(!j.Ack("a",8L,"Test"),"stale ack rejected");
   Assert(j.Ack("a",7L,"Test"),"matching ack durable");
   j=Activator.CreateInstance(t,path);Assert(((List<string>)j.Snapshot(7L,"Test")).Count==0,"ack survives restart");
   File.WriteAllText(path,"{broken");j=Activator.CreateInstance(t,path);
   Assert(!j.Append(m),"corrupt evidence never overwritten");Assert(File.ReadAllText(path)=="{broken","corrupt bytes preserved");
   j=Activator.CreateInstance(t,dir);Assert(!j.Append(m),"unwritable file prevents send eligibility");
   Console.WriteLine("ObservationTests: persistence, replay isolation, idempotence, ack, corruption and write failure passed");
  }finally{Directory.Delete(dir,true);}
 }
}
