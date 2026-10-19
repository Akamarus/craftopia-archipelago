using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    // The install directory is disposable. This directory is owned by the player.
    internal static class UserStorage
    {
        internal static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CraftopiaArchipelago");
        const string Marker = "storage-v1.complete";
        const string Format = "Craftopia Archipelago user storage v1";

        internal static string[] LegacyDirectories(string config, string currentPlugin, string plugins)
        {
            var sources = new List<string> { config, currentPlugin, Path.Combine(currentPlugin,"bridge","client") };
            if (Directory.Exists(plugins))
                foreach (string host in Directory.GetFiles(plugins,"connection_host.py",SearchOption.AllDirectories))
                {
                    string client = Path.GetDirectoryName(host);
                    sources.Add(client);
                    var bridge = Directory.GetParent(client);
                    if (Path.GetFileName(client)=="client" && bridge != null && bridge.Name=="bridge" && bridge.Parent != null)
                        sources.Add(bridge.Parent.FullName);
                }
            return sources.ToArray();
        }

        internal static bool IsStateFile(string name) =>
            name == "com.craftopia.archipelago.cfg" ||
            name.StartsWith("craftopia-archipelago-", StringComparison.Ordinal) ||
            name.StartsWith("craftopia-currency-", StringComparison.Ordinal) ||
            name.StartsWith("craftopia-milestone-", StringComparison.Ordinal) ||
            name.StartsWith("craftopia-orb-character-", StringComparison.Ordinal) ||
            name.StartsWith("craftopia-observations-", StringComparison.Ordinal) ||
            name.StartsWith("craftopia_observations.log", StringComparison.Ordinal) ||
            name.StartsWith("craftopia_bridge_ledger_", StringComparison.Ordinal) ||
            name.StartsWith("craftopia_mp_", StringComparison.Ordinal) ||
            name.StartsWith("craftopia_bridge_authority_", StringComparison.Ordinal);

        internal static void Migrate(string root, string[] legacyDirectories)
        {
            root = Path.GetFullPath(root);
            Directory.CreateDirectory(Path.GetDirectoryName(root));
            // Serialize first initialization across multiple game processes.
            using (new FileStream(root + ".migration.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                if (Directory.Exists(root))
                {
                    if (!File.Exists(Path.Combine(root, Marker)) || File.ReadAllText(Path.Combine(root, Marker)) != Format)
                        throw new IOException("Unrecognized AP user-data directory; restore its completion marker or recover it before starting.");
                    return; // Canonical data is authoritative; old sources are deliberately retained backups.
                }
                string staging = root + ".migration-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(staging);
                try
                {
                    foreach (string directory in legacyDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (!Directory.Exists(directory)) continue;
                        foreach (string source in Directory.GetFiles(directory).Where(p => IsStateFile(Path.GetFileName(p))))
                        {
                            // An active old bridge or plugin must finish before migration.
                            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                string destination = Path.Combine(staging, Path.GetFileName(source));
                                using (var data = new MemoryStream())
                                {
                                    input.CopyTo(data);
                                    byte[] bytes = data.ToArray();
                                    if (File.Exists(destination))
                                    {
                                        if (!bytes.SequenceEqual(File.ReadAllBytes(destination)))
                                            throw new IOException("Conflicting AP migration files: " + Path.GetFileName(source));
                                    }
                                    else using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                    { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                                }
                            }
                        }
                    }
                    File.WriteAllText(Path.Combine(staging, Marker), Format);
                    // No partially migrated directory is ever used as live state.
                    Directory.Move(staging, root);
                }
                finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
            }
        }
    }
}
