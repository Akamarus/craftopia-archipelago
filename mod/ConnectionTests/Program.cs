using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using CraftopiaArchipelago;
using BepInEx.Configuration;
class Program {
 static void Check(bool value,string name) { if(!value)throw new Exception(name); }
 static void Wait(ConnectionController c,Func<bool> condition) { var timer=Stopwatch.StartNew();while(!condition() && timer.ElapsedMilliseconds<10000){Thread.Sleep(20);c.Pump();}Check(condition(),"timed out: "+c.Status); }
 static void Main(string[] args) {
  if(args.Length>0) { Child(args[1]);return; }
  using var c=new ConnectionController(new ConfigFile(),Environment.CurrentDirectory);
  c.Server="";c.Connect();Check(!c.IsRunning && c.Status.Contains("server",StringComparison.OrdinalIgnoreCase),"empty server rejected");
  c.Server="localhost:abc";c.Connect();Check(!c.IsRunning,"invalid port rejected");
  c.Server="localhost:38281";c.Slot="\n";c.Connect();Check(!c.IsRunning,"empty slot rejected");
  c.Slot="Player1";c.Password="secret with spaces";
  var info=ConnectionController.CreateStartInfo("C:/python.exe","C:/bridge/connection_host.py");
  Check(!info.UseShellExecute && info.CreateNoWindow && info.RedirectStandardInput && info.RedirectStandardOutput && info.RedirectStandardError,"hidden redirected process");
  Check(!info.Arguments.Contains(c.Password) && !string.Join(" ",info.ArgumentList).Contains(c.Password),"credentials excluded from arguments");
  Check(!ConnectionController.IsValidProfileKey("../../evil") && ConnectionController.IsValidProfileKey("managed-"+new string('a',64)),"profile path validation");
  var encrypted=PasswordStore.Protect("password");Check(encrypted!="password" && PasswordStore.Unprotect(encrypted)=="password","DPAPI roundtrip");
  Check(PasswordStore.Unprotect("broken ciphertext")=="","bad DPAPI value cleared");
  Plugin.CanChangeConnection=false;c.Connect();Check(!c.IsRunning && c.Status.Contains("menu",StringComparison.OrdinalIgnoreCase),"new profile denied in loaded world");
  Plugin.CanChangeConnection=true;
  var config=new ConfigFile(); config.Bind("Connection","PythonExecutable",Environment.ProcessPath,"");
  config.Bind("Connection","RememberPassword",true,"");var saved=config.Bind("Connection","ProtectedPassword","not-encrypted","");
  string dir=Path.Combine(Path.GetTempPath(),"craftopia-connection-test-"+Guid.NewGuid());Directory.CreateDirectory(Path.Combine(dir,"bridge","client"));
  string script=Path.Combine(dir,"bridge","client","connection_host.py");File.WriteAllText(script,"normal");
  try {
   using var live=new ConnectionController(config,dir);Check(saved.Value=="" && live.Password=="","invalid remembered password erased");
   live.Password="secret";live.RememberPassword=false;int saves=config.Saves;live.RememberPassword=false;live.RememberPassword=false;Check(config.Saves==saves,"unchanged opt out must not write config");live.Connect();Wait(live,()=>live.IsConnected);Check(Plugin.Preparations==1,"prepares before connected");Check(live.Seed=="test","prepared seed exposed");live.StarterChoice="Bow";Check(saved.Value=="","password not retained without opt in");
   live.Disconnect();Check(!live.IsConnected && !live.IsRunning,"disconnect stops child");Check(live.Seed=="test","seed remains after disconnect");using(var reloaded=new ConnectionController(config,dir))Check(reloaded.StarterChoice=="Bow","starter choice persists");
   Plugin.CanChangeConnection=false;live.Connect();Wait(live,()=>live.IsConnected);live.Disconnect();
   File.WriteAllText(script,"different-seed");live.Connect();Wait(live,()=>!live.IsRunning);Check(live.Status.Contains("different seed"),"seed switch rejected in loaded world");
   Plugin.CanChangeConnection=true;File.WriteAllText(script,"bad-key");live.Connect();Wait(live,()=>!live.IsRunning);Check(!live.IsConnected,"unsafe profile key rejected");
   File.WriteAllText(script,"retry");live.Connect();Wait(live,()=>live.IsConnected);Wait(live,()=>!live.IsConnected);Check(live.IsRunning,"transient disconnect retains process");Wait(live,()=>live.IsConnected);live.Disconnect();
   File.WriteAllText(script,"normal");live.Connect();Thread.Sleep(100);live.Disconnect();live.Pump();Check(!live.IsConnected && live.Status=="Disconnected.","stale queued events ignored");
  } finally {Directory.Delete(dir,true);}
  Console.WriteLine("Connection tests passed");
 }
 static void Child(string script) {
  var config=MiniJson.ParseObject(Console.ReadLine());string mode=File.ReadAllText(script);
  Console.WriteLine(MiniJson.Stringify(new Dictionary<string,object>{{"state","prepared"},{"server",MiniJson.GetString(config,"server")},{"slot",MiniJson.GetString(config,"slot")},{"seed","test"},{"session",mode=="bad-key"?"../../escape":"managed-"+new string(mode=="different-seed"?'b':'a',64)}}));
  if(Console.ReadLine()==null)return;
  Console.WriteLine("{\"state\":\"connected\"}");
  if(mode=="retry") { Thread.Sleep(300);Console.WriteLine("{\"state\":\"reconnecting\"}");Thread.Sleep(300);Console.WriteLine("{\"state\":\"connected\"}"); }
  Console.ReadLine();
 }
}
namespace BepInEx.Configuration { public class ConfigEntry<T> { public T Value; } public class ConfigFile { readonly Dictionary<string,object> values=new();public ConfigEntry<T> Bind<T>(string s,string k,T v,string d){if(values.TryGetValue(s+k,out var old))return (ConfigEntry<T>)old;var entry=new ConfigEntry<T>{Value=v};values[s+k]=entry;return entry;} public int Saves; public void Save(){Saves++;} } }
namespace CraftopiaArchipelago { static class Plugin { internal static bool CanChangeConnection=true; internal static string ManagedProfileKey=""; internal static int Preparations;internal static bool PrepareManagedSession(string slot,string key,int port){Preparations++;ManagedProfileKey=key;return true;} } }
