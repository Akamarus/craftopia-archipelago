using System;
using System.Collections.Generic;
using CraftopiaArchipelago;
class Program
{
    static int assertions;
    static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); Console.WriteLine("PASS " + message); }
    static Dictionary<string,object> Message(long epoch=1, string token="token", string station="Refiner")
    { return new Dictionary<string,object> { ["cmd"]="unlock_production", ["epoch"]=epoch, ["slot"]="TestPlayer", ["connection_token"]=token, ["receipt_id"]=0L, ["station_id"]=station }; }
    static void Main()
    {
        var permissions=new PermissionManager(_=>{},_=>{});
        var mirror=new UnlockMirror(permissions);
        var acks=new List<Dictionary<string,object>>();
        Check(!mirror.Accept(Message(),1),"no unlock before negotiated session");
        Check(mirror.BeginSession("TestPlayer",1,"token",1),"bind validated session");
        Check(mirror.Accept(Message(),1),"queue valid unlock");
        mirror.Flush(false,1,acks.Add);
        Check(!permissions.CanUseProduction("Refiner") && acks.Count==0,"hold prevents permission mutation and ack");
        mirror.Flush(true,1,acks.Add);
        Check(permissions.CanUseProduction("Refiner") && acks.Count==1,"eligible player receives permission and ack");
        Check((long)acks[0]["epoch"]==1 && (string)acks[0]["connection_token"]=="token" && (string)acks[0]["slot"]=="TestPlayer","ack carries exact session identity");
        mirror.Flush(false,1,acks.Add);
        Check(!permissions.CanUseProduction("Refiner"),"load or hold clears exposed permissions");
        mirror.Flush(true,1,acks.Add);
        Check(permissions.CanUseProduction("Refiner"),"eligible reload restores mirror from retained authoritative records");
        Check(mirror.Accept(Message(),1),"lost ack replay accepted idempotently"); mirror.Flush(true,1,acks.Add);
        Check(acks.Count==2,"lost ack replay reacked");
        mirror.BeginSession("TestPlayer",2,"next",2);
        Check(!permissions.CanUseProduction("Refiner"),"new session clears old permissions");
        Check(!mirror.Accept(Message(),1),"stale queued connection rejected");
        Check(!mirror.Accept(Message(1,"next"),2),"stale epoch rejected");
        Check(!mirror.Accept(Message(2,"token"),2),"stale token rejected");
        Check(!mirror.Accept(Message(2,"next"," "),2),"blank permission rejected without ack");
        var bad=Message(2,"next"); bad["receipt_id"]=true;
        Check(!mirror.Accept(bad,2),"coerced receipt rejected");
        Check(mirror.Accept(Message(2,"next"),2),"current session accepted");
        mirror.Flush(true,3,acks.Add);
        Check(!permissions.CanUseProduction("Refiner"),"new socket before session prevents stale mirror use");
        mirror.Flush(true,2,_=>{throw new Exception("outbox failed");});
        int before=acks.Count; mirror.Flush(true,2,acks.Add);
        Check(acks.Count==before+1,"failed ack remains retryable");
        Check(!mirror.Accept(Message(2,"next","Changed"),2),"conflicting receipt rejected");
        mirror.Flush(true,2,acks.Add);
        Check(!permissions.CanUseProduction("Refiner") && !permissions.CanUseProduction("Changed"),"conflict holds entire mirror");
        var restarted=new UnlockMirror(new PermissionManager(_=>{},_=>{}));
        Check(restarted.BeginSession("TestPlayer",2,"fresh",4) && restarted.Accept(Message(2,"fresh"),4),"fresh mod accepts bridge history replay");
        restarted.Flush(true,4,acks.Add);
        var noisyPermissions=new PermissionManager(_=>{throw new Exception("logger");},_=>{throw new Exception("logger");});
        var noisy=new UnlockMirror(noisyPermissions); noisy.BeginSession("TestPlayer",1,"token",1); noisy.Accept(Message(),1);
        before=acks.Count; noisy.Flush(true,1,acks.Add);
        Check(noisyPermissions.CanUseProduction("Refiner") && acks.Count==before+1,"diagnostic failures cannot prevent accepted mirror delivery");
        foreach (string tree in new[]{"Magic", "Skill Tree: Magic"}) {
            var starterPermissions=new PermissionManager(_=>{},_=>{});
            var starter=new UnlockMirror(starterPermissions);
            starter.BeginSession("TestPlayer",1,"token",1);
            var msg=Message(); msg["cmd"]="unlock_skill_tree"; msg.Remove("station_id");
            msg["tree_id"]=tree; msg["receipt_id"]=-8L;
            Check(starter.Accept(msg,1),"starter Magic receipt accepted: "+tree);
            starter.Flush(true,1,_=>{});
            Check(starterPermissions.IsSkillTreeUnlocked("Magic",1),"starter enables actual Magic skill gate: "+tree);
            starter.Suspend(); starter.Flush(true,1,_=>{});
            Check(starterPermissions.IsSkillTreeUnlocked("Magic",1),"starter Magic survives reload: "+tree);
        }
        Console.WriteLine("Assertions: "+assertions);
    }
}
