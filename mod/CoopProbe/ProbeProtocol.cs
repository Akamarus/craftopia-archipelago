using System;

namespace CraftopiaArchipelago.Diagnostics
{
    internal readonly struct ProbeFrame
    {
        public readonly byte Kind;
        public readonly ushort Version;
        public readonly Guid Nonce;
        public ProbeFrame(byte kind, ushort version, Guid nonce)
        { Kind = kind; Version = version; Nonce = nonce; }
        public bool IsValid => (Kind == 1 || Kind == 2) && Version == 1 && Nonce != Guid.Empty;
    }

    internal static class ProbeProtocol
    {
        public const int Size = 23;
        public static byte[] Encode(ProbeFrame frame)
        {
            if (!frame.IsValid) throw new ArgumentException("Invalid probe frame.");
            var bytes = new byte[Size];
            bytes[0] = 67; bytes[1] = 65; bytes[2] = 80; bytes[3] = 68;
            bytes[4] = frame.Kind; bytes[5] = 1; bytes[6] = 0;
            Array.Copy(frame.Nonce.ToByteArray(), 0, bytes, 7, 16);
            return bytes;
        }

        public static bool TryDecode(byte[] bytes, out ProbeFrame frame)
        {
            frame = default;
            if (bytes == null || bytes.Length != Size || bytes[0] != 67 || bytes[1] != 65 ||
                bytes[2] != 80 || bytes[3] != 68 || (bytes[4] != 1 && bytes[4] != 2) ||
                bytes[5] != 1 || bytes[6] != 0) return false;
            var nonce = new Guid(new ReadOnlySpan<byte>(bytes, 7, 16));
            if (nonce == Guid.Empty) return false;
            frame = new ProbeFrame(bytes[4], 1, nonce);
            return true;
        }
    }
}
