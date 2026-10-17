using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CraftopiaArchipelago
{
    public sealed class ObservationJournal
    {
        private readonly string path;
        private readonly object sync = new object();
        private List<string> records = new List<string>();
        private bool failed;
        public ObservationJournal(string path)
        {
            this.path = path;
            try
            {
                if (Directory.Exists(path)) throw new IOException("Journal is a directory");
                if (!File.Exists(path)) return;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (!Valid(line)) throw new InvalidDataException("Invalid observation");
                    records.Add(line);
                }
            }
            catch { failed = true; }
        }
        private static Dictionary<string, object> Parse(string line) => MiniJson.ParseObject(line);
        private static bool Valid(string line)
        {
            var m = Parse(line);
            return m != null && MiniJson.GetString(m, "cmd") == "check" &&
                m.TryGetValue("epoch", out var epoch) && epoch is long n && n > 0 &&
                !string.IsNullOrWhiteSpace(MiniJson.GetString(m, "slot"));
        }
        public bool Append(string line)
        {
            lock (sync)
            {
                if (failed) return false;
                try
                {
                    if (!Valid(line)) return false;
                    if (records.Contains(line)) return true;
                    // Flush to disk before allowing the socket queue to see it.
                    using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        var bytes = Encoding.UTF8.GetBytes(line + "\n");
                        stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                    }
                    records.Add(line); return true;
                }
                catch { failed = true; return false; } // Possible partial append: preserve evidence.
            }
        }
        private static bool Matches(Dictionary<string, object> m, long epoch, string slot)
            => m != null && MiniJson.GetLong(m, "epoch") == epoch && MiniJson.GetString(m, "slot") == slot;
        public List<string> Snapshot(long epoch, string slot)
        {
            lock (sync)
            {
                var result = new List<string>();
                if (!failed) foreach (var line in records)
                    if (Matches(Parse(line), epoch, slot)) result.Add(line);
                return result;
            }
        }
        public bool Ack(string id, long epoch, string slot)
        {
            lock (sync)
            {
                if (failed || string.IsNullOrEmpty(id)) return false;
                try
                {
                    var kept = records.FindAll(line => {
                        var m = Parse(line);
                        return !Matches(m, epoch, slot) || MiniJson.GetString(m, "observation_id") != id;
                    });
                    if (kept.Count == records.Count) return false;
                    string temp = path + ".tmp";
                    using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var bytes = Encoding.UTF8.GetBytes(string.Join("\n", kept) + (kept.Count > 0 ? "\n" : ""));
                        stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                    }
                    File.Replace(temp, path, null);
                    records = kept; return true;
                }
                catch { return false; } // Keep in memory/on disk until a durable removal succeeds.
            }
        }
    }
}
