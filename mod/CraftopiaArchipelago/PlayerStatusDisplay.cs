using System;
using System.Collections.Generic;
using System.Text;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Player-facing status display.
    ///
    /// Shows unlocks, victory progress, and connection status.
    /// This is the data model; actual in-game UI rendering requires
    /// Unity UI hooks (not yet implemented).
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
        private string lastBlockedMessage = null;
        public void ShowBlockedAction(string message)
        {
            LastError = message;
            // Transition-only logging: avoid spamming every frame.
            if (message != lastBlockedMessage)
            {
                lastBlockedMessage = message;
                logInfo?.Invoke(message);
            }
        }

        public void LogStatus()
        {
            logInfo(GetStatusSummary());
        }
    }
}
