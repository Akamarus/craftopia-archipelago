using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CraftopiaArchipelago
{
    // Both startup and transaction writes use this exact parser.
    public sealed class PersistedGrantState
    {
        public long Epoch;
        public readonly HashSet<long> Applied = new HashSet<long>();
        public readonly HashSet<long> Quarantined = new HashSet<long>();
        internal static long Integer(object value)
        {
            if (!(value is long)) throw new FormatException("Expected an Int64 JSON integer");
            return (long)value;
        }
        public static PersistedGrantState Parse(string json)
        {
            var d = MiniJson.ParseObject(json);
            var s = new PersistedGrantState { Epoch = Integer(d["epoch"]) };
            if (s.Epoch <= 0) throw new FormatException("Invalid epoch");
            ReadIndices(d["applied"], s.Applied);
            // Pre-quarantine files are intentionally supported; a present field must be valid.
            if (d.ContainsKey("quarantined")) ReadIndices(d["quarantined"], s.Quarantined);
            if (s.Applied.Overlaps(s.Quarantined)) throw new FormatException("Conflicting index states");
            return s;
        }
        private static void ReadIndices(object value, HashSet<long> target)
        {
            if (!(value is List<object> list)) throw new FormatException("Expected index array");
            foreach (var item in list)
            {
                long n = Integer(item);
                if (n < 0 || !target.Add(n)) throw new FormatException("Invalid or duplicate index");
            }
        }
        public static PersistedGrantState Read(string path) => Parse(File.ReadAllText(path));
        public static void MergeApplied(string path, long epoch, IEnumerable<long> indices)
        {
            if (epoch <= 0) throw new FormatException("Invalid epoch");
            var s = File.Exists(path) ? Read(path) : new PersistedGrantState { Epoch = epoch };
            if (s.Epoch != epoch) throw new FormatException("Epoch mismatch");
            foreach (long n in indices)
            {
                if (n < 0 || s.Quarantined.Contains(n)) throw new FormatException("Invalid or quarantined index");
                s.Applied.Add(n);
            }
            DurableJson.Write(path, MiniJson.Stringify(new Dictionary<string, object> {
                ["epoch"] = s.Epoch, ["applied"] = s.Applied, ["quarantined"] = s.Quarantined }));
        }
    }

    internal static class DurableJson
    {
        public static void Write(string path, string text)
        {
            string tmp = path + ".tmp";
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var file = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
    }

    // No automatic clear API. Reconciliation is an explicit offline, reviewed operation.
    public sealed class RecoveryHoldStore
    {
        readonly string path;
        readonly Action<string, string> write;
        readonly Dictionary<string, string> holds = new Dictionary<string, string>();
        bool unreadable, dirty;
        public RecoveryHoldStore(string path, Action<string, string> write = null)
        {
            this.path = path; this.write = write ?? DurableJson.Write;
            try {
                if (!File.Exists(path)) return;
                var d = MiniJson.ParseObject(File.ReadAllText(path));
                if (PersistedGrantState.Integer(d["version"]) != 1) throw new FormatException("Hold version");
                var h = d["holds"] as Dictionary<string, object>;
                if (h == null) throw new FormatException("Missing holds");
                foreach (var kv in h) {
                    if (kv.Key != "*") {
                        var parts = kv.Key.Split(':');
                        if (parts.Length != 2 || !int.TryParse(parts[0], out int guid) || guid == 0 ||
                            !long.TryParse(parts[1], out long epoch) || epoch <= 0 ||
                            kv.Key != Key(guid, epoch)) throw new FormatException("Invalid hold identity");
                    }
                    if (!(kv.Value is string reason) || string.IsNullOrWhiteSpace(reason))
                        throw new FormatException("Invalid hold");
                    holds.Add(kv.Key, reason);
                }
            } catch { unreadable = true; }
        }
        static string Key(int guid, long epoch) => guid.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + epoch.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public bool IsHeld(int guid, long epoch) => unreadable || holds.ContainsKey("*") || holds.ContainsKey(Key(guid, epoch));
        public void Hold(int guid, long epoch, string reason, bool slotWide = false)
        {
            holds[slotWide ? "*" : Key(guid, epoch)] = reason;
            dirty = true; // In-memory block precedes IO.
            RetryWrite();
        }
        public bool RetryWrite()
        {
            if (unreadable) return false; // Never overwrite corrupt evidence.
            if (!dirty) return true;
            try {
                var h = new Dictionary<string, object>();
                foreach (var kv in holds) h.Add(kv.Key, kv.Value);
                write(path, MiniJson.Stringify(new Dictionary<string, object> { ["version"] = 1, ["holds"] = h }));
                dirty = false; return true;
            } catch { return false; }
        }
    }
}
