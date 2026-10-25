using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;

namespace CraftopiaArchipelago.Diagnostics
{
    // The full type name is part of Mirror's message ID. Do not rename casually.
    internal struct ProbeMessage : NetworkMessage
    {
        internal ProbeFrame Frame;
    }

    internal sealed class ProbeTransport : IDisposable
    {
        private readonly bool enabled, initiate;
        private readonly Func<double> clock;
        private readonly Action<string> log;
        private readonly ProbeSession session = new ProbeSession();
        private readonly Dictionary<NetworkConnectionToClient,double> replies = new Dictionary<NetworkConnectionToClient,double>();
        private readonly Action<NetworkWriter,ProbeMessage> writer = Write;
        private readonly Func<NetworkReader,ProbeMessage> reader = Read;
        private Dictionary<ushort,NetworkMessageDelegate> clientTable, serverTable;
        private NetworkMessageDelegate clientHandler, serverHandler;
        private NetworkConnection bound;
        private bool serializersOwned, stopped, waiting;
        private double nextPoll, deadline;
        private readonly ushort id = NetworkMessages.GetId<ProbeMessage>();

        public ProbeTransport(bool enabled, bool initiate, Func<double> clock, Action<string> log)
        { this.enabled=enabled; this.initiate=initiate; this.clock=clock; this.log=log; }

        public void Tick()
        {
            if (!enabled || stopped) return;
            double now=clock();
            if (now < nextPoll) return;
            nextPoll=now+.25;
            try
            {
                if (!EnsureHandlers()) { Fail("handler or serializer collision; probe stopped"); return; }
                foreach(var peer in new List<NetworkConnectionToClient>(replies.Keys))
                    if (!IsRemote(peer)) replies.Remove(peer);

                var current=NetworkClient.connection;
                bool eligible=initiate && NetworkClient.isConnected && !NetworkClient.isHostClient &&
                    !NetworkServer.active && current!=null && current.isAuthenticated;
                if (!eligible) { ResetSession(); return; }
                if (!ReferenceEquals(bound,current))
                {
                    ResetSession(); bound=current;
                    Guid nonce=Guid.NewGuid(); session.Arm(nonce); waiting=true; deadline=now+10;
                    current.Send(new ProbeMessage{Frame=new ProbeFrame(1,1,nonce)},Channels.Reliable);
                    log("guest protocol 1: hello sent");
                }
                else if (waiting && now>=deadline)
                {
                    session.Reset(); waiting=false;
                    log("guest protocol 1: timed out; no retry");
                }
            }
            catch { Fail("adapter unavailable; probe stopped"); }
        }

        private static Dictionary<ushort,NetworkMessageDelegate> Table(Type type)
        {
            return type.GetField("handlers",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
                ?.GetValue(null) as Dictionary<ushort,NetworkMessageDelegate>;
        }

        private static bool Available(Dictionary<ushort,NetworkMessageDelegate> table, ushort key, NetworkMessageDelegate owned)
        { return table!=null && (!table.TryGetValue(key,out var found) || ReferenceEquals(found,owned)); }

        private bool EnsureHandlers()
        {
            clientTable=Table(typeof(NetworkClient)); serverTable=Table(typeof(NetworkServer));
            // Check BOTH tables before changing either, including while offline.
            if (!Available(clientTable,id,clientHandler) || !Available(serverTable,id,serverHandler)) return false;
            if (serializersOwned)
            {
                if (!ReferenceEquals(Writer<ProbeMessage>.write,writer) || !ReferenceEquals(Reader<ProbeMessage>.read,reader)) return false;
            }
            else
            {
                if (Writer<ProbeMessage>.write!=null || Reader<ProbeMessage>.read!=null) return false;
                Writer<ProbeMessage>.write=writer; Reader<ProbeMessage>.read=reader; serializersOwned=true;
            }
            if (!clientTable.ContainsKey(id))
            {
                NetworkClient.RegisterHandler<ProbeMessage>(OnEcho,true);
                clientHandler=clientTable[id];
            }
            if (!serverTable.ContainsKey(id))
            {
                NetworkServer.RegisterHandler<ProbeMessage>(OnHello,true);
                serverHandler=serverTable[id];
            }
            return true;
        }

        private bool OwnsHandlers() => enabled && !stopped && serializersOwned &&
            clientTable!=null && serverTable!=null &&
            clientTable.TryGetValue(id,out var ch) && ReferenceEquals(ch,clientHandler) &&
            serverTable.TryGetValue(id,out var sh) && ReferenceEquals(sh,serverHandler) &&
            ReferenceEquals(Writer<ProbeMessage>.write,writer) && ReferenceEquals(Reader<ProbeMessage>.read,reader);

        private static bool IsRemote(NetworkConnectionToClient peer) => NetworkServer.active && peer!=null &&
            peer.isAuthenticated && !ReferenceEquals(peer,NetworkServer.localConnection) &&
            NetworkServer.connections.TryGetValue(peer.connectionId,out var live) && ReferenceEquals(live,peer);

        private void OnHello(NetworkConnectionToClient peer, ProbeMessage message)
        {
            try
            {
                if (!OwnsHandlers() || !IsRemote(peer) || !message.Frame.IsValid || message.Frame.Kind!=1) return;
                double now=clock();
                if(replies.TryGetValue(peer,out double last) && now-last<1) return;
                replies[peer]=now;
                peer.Send(new ProbeMessage{Frame=new ProbeFrame(2,1,message.Frame.Nonce)},Channels.Reliable);
                log("host protocol 1: echo sent to connection "+peer.connectionId);
            }
            catch { Fail("host adapter unavailable; probe stopped"); }
        }

        private void OnEcho(ProbeMessage message)
        {
            try
            {
                if (!OwnsHandlers() || !waiting || !NetworkClient.isConnected || NetworkServer.active || NetworkClient.isHostClient ||
                    bound==null || !ReferenceEquals(bound,NetworkClient.connection) || !bound.isAuthenticated ||
                    !message.Frame.IsValid || message.Frame.Kind!=2 || clock()>=deadline) return;
                if(session.AcceptEcho(message.Frame.Nonce)) { waiting=false; log("guest protocol 1: echo accepted"); }
            }
            catch { Fail("guest adapter unavailable; probe stopped"); }
        }

        private static void Write(NetworkWriter target, ProbeMessage message)
        {
            foreach(byte value in ProbeProtocol.Encode(message.Frame)) target.WriteByte(value);
        }

        private static ProbeMessage Read(NetworkReader source)
        {
            // Mirror's reader can contain subsequent messages: consume only this fixed frame.
            if(source.Remaining<ProbeProtocol.Size)
            {
                while(source.Remaining>0) source.ReadByte();
                return default;
            }
            var bytes=new byte[ProbeProtocol.Size];
            for(int i=0;i<bytes.Length;i++) bytes[i]=source.ReadByte();
            return ProbeProtocol.TryDecode(bytes,out var frame) ? new ProbeMessage{Frame=frame} : default;
        }

        private void ResetSession() { bound=null; waiting=false; session.Reset(); }
        private void Fail(string reason)
        {
            if(stopped) return;
            Dispose();
            // Logging must not allow an exception to escape a network callback.
            try { log(reason); } catch { }
        }
        public void Dispose()
        {
            stopped=true; ResetSession(); replies.Clear();
            RemoveOwned(clientTable,clientHandler); RemoveOwned(serverTable,serverHandler);
            if(serializersOwned)
            {
                if(ReferenceEquals(Writer<ProbeMessage>.write,writer)) Writer<ProbeMessage>.write=null;
                if(ReferenceEquals(Reader<ProbeMessage>.read,reader)) Reader<ProbeMessage>.read=null;
            }
            serializersOwned=false;
        }
        private void RemoveOwned(Dictionary<ushort,NetworkMessageDelegate> table, NetworkMessageDelegate owned)
        {
            if(table!=null && owned!=null && table.TryGetValue(id,out var found) && ReferenceEquals(found,owned)) table.Remove(id);
        }
    }
}
