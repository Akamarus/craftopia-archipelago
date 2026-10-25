using System;
using System.Linq;
using System.Collections.Generic;
using Mirror;
using CraftopiaArchipelago.Diagnostics;

static class TransportTests
{
    static void Check(bool ok,string label) => Program.Check(ok,label);
    static ushort Id => NetworkMessages.GetId<ProbeMessage>();
    static double now;
    static List<string> logs = new();
    static ProbeTransport Create(bool enabled=true,bool initiate=true) => new ProbeTransport(enabled,initiate,()=>now,logs.Add);
    static void Reset()
    {
        NetworkClient.active=false; NetworkClient.isConnected=false; NetworkClient.isHostClient=false;
        NetworkClient.connection=null; NetworkClient.handlers.Clear(); NetworkServer.handlers.Clear();
        NetworkServer.active=false; NetworkServer.localConnection=null; NetworkServer.connections.Clear();
        Writer<ProbeMessage>.write=null; Reader<ProbeMessage>.read=null; now=0; logs.Clear();
    }
    static NetworkConnection Guest()
    {
        NetworkClient.active=true; NetworkClient.isConnected=true;
        return NetworkClient.connection=new NetworkConnection {connectionId=9,isAuthenticated=true};
    }
    static void ReceiveClient(ProbeFrame frame) => NetworkClient.handlers[Id](NetworkClient.connection,new NetworkReader(ProbeProtocol.Encode(frame)),0);
    internal static void Run()
    {
        Reset(); var c=Guest();
        using(var probe=Create(false)) { probe.Tick(); Check(c.Sent.Count==0 && NetworkClient.handlers.Count==0 && Writer<ProbeMessage>.write==null,"disabled silent"); }
        Reset(); c=Guest();
        using(var probe=Create(true,false)) { probe.Tick(); Check(c.Sent.Count==0,"listener never initiates"); }
        Reset(); c=Guest(); c.isAuthenticated=false;
        using(var probe=Create())
        {
            probe.Tick(); Check(c.Sent.Count==0,"no pre-auth send");
            c.isAuthenticated=true; now=1; probe.Tick(); Check(c.Sent.Count==1,"one hello");
            var sent=(ProbeMessage)c.Sent[0];
            var writer=new NetworkWriter(); Writer<ProbeMessage>.write(writer,sent);
            Check(ProbeProtocol.TryDecode(writer.Data.ToArray(),out var hello) && hello.Kind==1,"hello encoded");
            now=2; probe.Tick(); Check(c.Sent.Count==1,"no retry");
            ReceiveClient(new ProbeFrame(2,1,Guid.NewGuid())); Check(!logs.Any(x=>x.Contains("echo accepted")),"wrong echo ignored");
            ReceiveClient(new ProbeFrame(2,1,hello.Nonce)); ReceiveClient(new ProbeFrame(2,1,hello.Nonce));
            Check(logs.Count(x=>x.Contains("echo accepted"))==1,"echo accepted once");
            var next=Guest(); now=3; probe.Tick(); Check(next.Sent.Count==1,"new connection hello");
            ReceiveClient(new ProbeFrame(2,1,hello.Nonce)); Check(logs.Count(x=>x.Contains("echo accepted"))==1,"old session echo ignored");
            now=14; probe.Tick(); now=15; probe.Tick(); Check(logs.Count(x=>x.Contains("timed out"))==1,"timeout once");
            Writer<ProbeMessage>.write(writer,(ProbeMessage)next.Sent[0]);
            var reader=new NetworkReader(writer.Data.ToArray()); var first=Reader<ProbeMessage>.read(reader);
            Check(reader.Remaining==23,"reader preserves following message"); Reader<ProbeMessage>.read(reader); Check(reader.Remaining==0,"second frame consumed");
            reader=new NetworkReader(new byte[4]); Reader<ProbeMessage>.read(reader); Check(reader.Remaining==0,"short payload drained safely");
        }
        Check(NetworkClient.handlers.Count==0 && NetworkServer.handlers.Count==0 && Writer<ProbeMessage>.write==null,"owned hooks removed");
        Reset(); c=Guest();
        using(var probe=Create())
        {
            probe.Tick(); var writer=new NetworkWriter(); Writer<ProbeMessage>.write(writer,(ProbeMessage)c.Sent[0]);
            ProbeProtocol.TryDecode(writer.Data.ToArray(),out var hello);
            now=10; ReceiveClient(new ProbeFrame(2,1,hello.Nonce));
            Check(!logs.Any(x=>x.Contains("echo accepted")),"late echo rejected before polling");
            probe.Tick(); Check(logs.Count(x=>x.Contains("timed out"))==1,"late echo times out");
        }
        Reset(); c=Guest(); c.ThrowOnSend=true;
        using(var probe=Create()) { probe.Tick(); now=1; probe.Tick(); Check(logs.Count==1 && NetworkClient.handlers.Count==0,"send failure stops without throwing or retry"); }
        Reset(); c=Guest(); Action<NetworkWriter,ProbeMessage> otherWriter=(w,m)=>{}; Writer<ProbeMessage>.write=otherWriter;
        using(var probe=Create()) { probe.Tick(); Check(c.Sent.Count==0 && NetworkClient.handlers.Count==0,"serializer collision prevents install"); }
        Check(ReferenceEquals(Writer<ProbeMessage>.write,otherWriter),"foreign serializer preserved");
        foreach(bool server in new[]{false,true})
        {
            Reset(); c=Guest(); NetworkMessageDelegate foreign=(a,b,d)=>{};
            (server?NetworkServer.handlers:NetworkClient.handlers)[Id]=foreign;
            using(var probe=Create()) { probe.Tick(); Check(c.Sent.Count==0,"collision prevents send"); }
            Check(ReferenceEquals((server?NetworkServer.handlers:NetworkClient.handlers)[Id],foreign),"collision preserved");
        }
        Reset(); c=Guest();
        using(var probe=Create())
        {
            probe.Tick(); NetworkMessageDelegate foreign=(a,b,d)=>{}; NetworkClient.handlers[Id]=foreign;
            now=1; probe.Tick(); probe.Dispose(); Check(ReferenceEquals(NetworkClient.handlers[Id],foreign),"foreign replacement preserved");
        }
        Reset(); c=Guest(); NetworkServer.active=true; NetworkClient.isHostClient=true;
        using(var probe=Create())
        {
            probe.Tick(); Check(c.Sent.Count==0,"host loopback never initiates");
            var remote=new NetworkConnectionToClient{connectionId=4,isAuthenticated=true}; NetworkServer.connections[4]=remote;
            var bytes=ProbeProtocol.Encode(new ProbeFrame(1,1,Guid.NewGuid()));
            void Hello() => NetworkServer.handlers[Id](remote,new NetworkReader(bytes),0);
            Hello(); Hello(); Check(remote.Sent.Count==1,"host reply rate limited");
            now=1; Hello(); Check(remote.Sent.Count==2,"host reply after interval");
            remote.isAuthenticated=false; now=2; Hello(); Check(remote.Sent.Count==2,"host refuses unauthenticated");
            remote.isAuthenticated=true; NetworkServer.localConnection=remote; now=3; Hello(); Check(remote.Sent.Count==2,"host refuses local peer");
            NetworkServer.localConnection=null; NetworkServer.connections.Remove(4); now=4; Hello(); Check(remote.Sent.Count==2,"host refuses disconnected peer");
            NetworkServer.connections[4]=remote;
            NetworkServer.handlers[Id](remote,new NetworkReader(new byte[23]),0); Check(remote.Sent.Count==2,"malformed ignored");
            var wrongVersion=bytes.ToArray(); wrongVersion[5]=2;
            NetworkServer.handlers[Id](remote,new NetworkReader(wrongVersion),0); Check(remote.Sent.Count==2,"wrong version ignored");
            NetworkServer.handlers[Id](remote,new NetworkReader(ProbeProtocol.Encode(new ProbeFrame(2,1,Guid.NewGuid()))),0);
            Check(remote.Sent.Count==2,"host never echoes an echo");
            NetworkClient.handlers.Clear(); NetworkServer.handlers.Clear(); now=5; probe.Tick();
            Check(NetworkServer.handlers.ContainsKey(Id) && NetworkClient.handlers.ContainsKey(Id),"handlers restored after network reset");
        }
        Reset();
    }
}
