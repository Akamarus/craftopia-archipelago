using System;
using System.Collections.Generic;
using System.Text;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Player-facing status display.
    ///
    /// Shows unlocks, victory progress, and connection status.
    /// Main-thread data model for the transient notices and F8 panel.
    /// </summary>
    public class PlayerStatusDisplay
    {
        private readonly PermissionManager permissions;
        private readonly Action<string> logInfo;

        // Victory progress.
        public bool BossGoalEnabled { get; set; }
        public bool BossDefeated { get; set; }
        public string BossName { get; set; }

        public bool AgeGoalEnabled { get; set; }
        public int AgeTarget { get; set; }
        public int CurrentAge { get; set; }

        public bool ObjectiveGoalEnabled { get; set; }
        public int ObjectiveTarget { get; set; }
        public int ObjectivesCompleted { get; set; }

        // Connection status.
        public bool BridgeConnected { get; set; }
        public bool ServerConnected { get; set; }
        public string LastError { get; set; }

        public PlayerStatusDisplay(PermissionManager permissions, Action<string> logInfo)
        {
            this.permissions = permissions;
            this.logInfo = logInfo;
        }

        /// <summary>
        /// Generate a human-readable status summary.
        /// </summary>
        public string GetStatusSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Archipelago Status ===");
            sb.AppendLine();

            // Victory progress.
            sb.AppendLine("Victory Conditions:");
            if (BossGoalEnabled)
                sb.AppendLine($"  Boss ({BossName}): {(BossDefeated ? "DEFEATED" : "Not yet")}");
            if (AgeGoalEnabled)
                sb.AppendLine($"  Age: {CurrentAge}/{AgeTarget}");
            if (ObjectiveGoalEnabled)
                sb.AppendLine($"  Objectives: {ObjectivesCompleted}/{ObjectiveTarget}");
            if (!BossGoalEnabled && !AgeGoalEnabled && !ObjectiveGoalEnabled)
                sb.AppendLine("  (none configured)");
            sb.AppendLine();

            // Connection.
            sb.AppendLine("Connection:");
            sb.AppendLine($"  Bridge: {(BridgeConnected ? "Connected" : "Disconnected")}");
            sb.AppendLine($"  Server: {(ServerConnected ? "Connected" : "Disconnected")}");
            if (!string.IsNullOrEmpty(LastError))
                sb.AppendLine($"  Last error: {LastError}");
            sb.AppendLine();

            sb.AppendLine("(Detailed unlock lists require in-game UI)");
            return sb.ToString();
        }

        /// <summary>
        /// Log the status summary (for debugging without UI).
        /// </summary>
        /// <summary>
        /// Display a blocked-action message to the player.
        /// Queues the message for the in-game status view.
        /// </summary>
        private readonly Dictionary<string, double> recent = new Dictionary<string, double>();
        private readonly List<Tuple<string, double>> notices = new List<Tuple<string, double>>();
        private readonly Dictionary<string, string> transitions = new Dictionary<string, string>();
        public static double Now => (DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalSeconds;
        public void Notify(string key, string message, double now)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrEmpty(key)) return;
            if (recent.TryGetValue(key, out double until) && until > now) return;
            foreach (var old in new List<string>(recent.Keys)) if (recent[old] <= now) recent.Remove(old);
            if (recent.Count >= 256) return; // Bound flood memory as well as visible lines.
            recent[key] = now + 10;
            var clean = new StringBuilder();
            foreach (char c in message) {
                if (clean.Length >= 180) break;
                if (!char.IsControl(c) && c != '<' && c != '>') clean.Append(c);
            }
            notices.RemoveAll(n => n.Item2 <= now);
            if (notices.Count >= 3) notices.RemoveAt(0);
            notices.Add(Tuple.Create(clean.ToString(), now + 6));
        }
        public string VisibleMessages(double now)
        {
            notices.RemoveAll(n => n.Item2 <= now);
            return string.Join("\n", notices.ConvertAll(n => n.Item1));
        }
        public void Transition(string key, string message)
        {
            if (transitions.TryGetValue(key, out string previous) && previous == message) return;
            if (transitions.Count >= 256 && !transitions.ContainsKey(key)) return;
            transitions[key] = message;
            try { if (!string.IsNullOrEmpty(message)) logInfo?.Invoke(message); } catch { }
        }
        public void ShowBlockedAction(string message)
        {
            try {
                LastError = message;
                Notify("blocked:" + message, message, Now);
                Transition("blocked:" + message, message);
            } catch { } // Presentation cannot change the enforcement result.
        }

        public void LogStatus()
        {
            logInfo(GetStatusSummary());
        }
    }
}
