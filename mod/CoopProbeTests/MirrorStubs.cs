// Behavioral test double, not proof of Mirror runtime operation.
using System;
using System.Collections.Generic;
namespace Mirror
{
    public interface NetworkMessage { }
    public delegate void NetworkMessageDelegate(NetworkConnection c, NetworkReader r, int channel);
    public static class Channels { public const int Reliable = 0; }
    public static class NetworkMessages { public static ushort GetId<T>() => 1234; }
    public static class Writer<T> { public static Action<NetworkWriter,T> write; }
    public static class Reader<T> { public static Func<NetworkReader,T> read; }
    public class NetworkWriter { public readonly List<byte> Data = new(); public void WriteByte(byte b) => Data.Add(b); }
    public class NetworkReader
    {
        readonly byte[] data; int pos;
        public NetworkReader(byte[] data) { this.data = data; }
        public int Remaining => data.Length-pos;
        public byte ReadByte() => data[pos++];
    }
    public class NetworkConnection
    {
        public int connectionId;
        public bool isAuthenticated;
        public bool ThrowOnSend;
        public readonly List<object> Sent = new();
        public void Send<T>(T message, int channel) { if(ThrowOnSend)throw new Exception("send failure"); if(channel!=0)throw new Exception("unreliable"); Sent.Add(message); }
    }
    public class NetworkConnectionToClient : NetworkConnection { }
    public static class NetworkClient
    {
        public static bool active, isConnected, isHostClient;
        public static NetworkConnection connection;
        internal static readonly Dictionary<ushort,NetworkMessageDelegate> handlers = new();
        public static void RegisterHandler<T>(Action<T> callback,bool requireAuthentication) where T:struct,NetworkMessage
        {
            if (!requireAuthentication) throw new Exception("authentication required");
            handlers[NetworkMessages.GetId<T>()] = (c,r,ch) => callback(Reader<T>.read(r));
        }
    }
    public static class NetworkServer
    {
        public static bool active;
        public static NetworkConnectionToClient localConnection;
        public static readonly Dictionary<int,NetworkConnectionToClient> connections = new();
        internal static readonly Dictionary<ushort,NetworkMessageDelegate> handlers = new();
        public static void RegisterHandler<T>(Action<NetworkConnectionToClient,T> callback,bool requireAuthentication) where T:struct,NetworkMessage
        {
            if (!requireAuthentication) throw new Exception("authentication required");
            handlers[NetworkMessages.GetId<T>()] = (c,r,ch) => callback((NetworkConnectionToClient)c,Reader<T>.read(r));
        }
    }
}
