using System;
using System.Linq;
using CraftopiaArchipelago.Diagnostics;

class Program
{
    static int checks;
    internal static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        checks++;
    }
    static void Main()
    {
        var nonce = new Guid("00112233-4455-6677-8899-aabbccddeeff");
        foreach (byte kind in new byte[] { 1, 2 })
        {
            var bytes = ProbeProtocol.Encode(new ProbeFrame(kind, 1, nonce));
            Check(bytes.Length == 23, "fixed size");
            Check(bytes.Take(7).SequenceEqual(new byte[] {67,65,80,68,kind,1,0}), "wire prefix");
            Check(bytes.Skip(7).SequenceEqual(nonce.ToByteArray()), "nonce byte order");
            var copy = bytes.ToArray();
            Check(ProbeProtocol.TryDecode(bytes, out var frame) && frame.Kind == kind && frame.Version == 1 && frame.Nonce == nonce, "round trip");
            Check(copy.SequenceEqual(bytes), "decode does not mutate input");
        }
        Check(!ProbeProtocol.TryDecode(null, out _), "null rejected");
        foreach (int size in new[] { 0, 22, 24, 10000 })
            Check(!ProbeProtocol.TryDecode(new byte[size], out _), "invalid size");
        foreach (int offset in new[] { 0, 1, 2, 3, 4, 5, 6 })
        {
            var bytes = ProbeProtocol.Encode(new ProbeFrame(1, 1, nonce));
            bytes[offset] = 255;
            Check(!ProbeProtocol.TryDecode(bytes, out _), "invalid field");
        }
        var empty = ProbeProtocol.Encode(new ProbeFrame(1, 1, nonce));
        Array.Clear(empty, 7, 16);
        Check(!ProbeProtocol.TryDecode(empty, out _), "empty nonce");
        foreach (var invalid in new[] {new ProbeFrame(0,1,nonce), new ProbeFrame(1,2,nonce), new ProbeFrame(1,1,Guid.Empty)})
        {
            bool rejected = false;
            try { ProbeProtocol.Encode(invalid); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "encoder rejects invalid frame");
        }
        var session = new ProbeSession();
        Check(!session.AcceptEcho(nonce), "unarmed echo");
        session.Arm(nonce);
        Check(!session.AcceptEcho(Guid.NewGuid()), "wrong nonce");
        Check(session.AcceptEcho(nonce), "matching echo");
        Check(!session.AcceptEcho(nonce), "duplicate echo");
        session.Arm(nonce); session.Reset();
        Check(!session.AcceptEcho(nonce), "reset rejects stale echo");
        session.Arm(nonce); var replacement = Guid.NewGuid(); session.Arm(replacement);
        Check(!session.AcceptEcho(nonce) && session.AcceptEcho(replacement), "replacement nonce");
        TransportTests.Run();
        Console.WriteLine($"PASS: {checks} checks");
    }
}
