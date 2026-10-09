using System;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Tracks grant eligibility: bound to player reference, GUID, epoch,
    /// load event, and validation outcome.
    ///
    /// Initially BLOCKED. Invalidated (blocked) on:
    /// - Load start (new player load invalidates previous validation)
    /// - Session change (new epoch/enrollment)
    /// - Validation errors or Defer outcome
    /// - Player reference change (different object)
    /// - GUID change (character switch)
    ///
    /// Only allows grants when:
    /// - A load has completed (loadEventId > 0)
    /// - Player reference matches the validated one
    /// - GUID matches the validated one
    /// - Epoch matches the validated one
    /// - Last validation outcome was confirmed Pass
    /// </summary>
    public class Eligibility
    {
        // The validated state.
        private object validatedPlayerRef;
        private int validatedGuid;
        private long validatedEpoch;
        private long validatedLoadEventId;
        private ValidationOutcome validatedOutcome;

        // Current state (set by load/session events).
        private object currentPlayerRef;
        private int currentGuid;
        private long currentEpoch;
        private long currentLoadEventId;
        private bool loadCompleted; // True only after OnLoadComplete.

        private readonly Action<string> logInfo;
        private readonly Action<string> logWarning;

        public Eligibility(Action<string> logInfo, Action<string> logWarning)
        {
            this.logInfo = logInfo;
            this.logWarning = logWarning;
            // Initially blocked: all fields are default (null/0).
            validatedOutcome = ValidationOutcome.Defer; // safe default
            loadCompleted = false;
        }

        /// <summary>
        /// Called on load start: invalidate eligibility BEFORE logging.
        /// The new load must complete and be validated before grants allowed.
        /// </summary>
        public void OnLoadStart(object playerRef, int guid, long loadEventId)
        {
            // Invalidate first.
            validatedOutcome = ValidationOutcome.Defer;
            validatedPlayerRef = null;
            loadCompleted = false; // Reset completion.

            // Update current state.
            currentPlayerRef = playerRef;
            currentGuid = guid;
            currentLoadEventId = loadEventId;
            // Epoch unchanged (session, not load).

            Info("[AP] eligibility invalidated: load start (event " + loadEventId + ")");
        }

        /// <summary>
        /// Called on load completion: update state, but still blocked until
        /// validation runs and passes.
        /// </summary>
        public void OnLoadComplete(object playerRef, int guid, long loadEventId)
        {
            if (playerRef == null || !ReferenceEquals(playerRef, currentPlayerRef) || loadEventId != currentLoadEventId) { Block("stale completion"); return; }
            currentGuid = guid;
            loadCompleted = true; // Mark as actually completed.
            // Still blocked until validation.
            Info("[AP] load complete (event " + loadEventId + "); awaiting validation");
        }

        /// <summary>
        /// Called on session change: invalidate eligibility.
        /// </summary>
        public void OnSessionChange(long newEpoch)
        {
            // Same-epoch reconnect: do NOT invalidate. The session is the
            // same; eligibility remains valid. This handles bridge reconnects
            // without stranding a loaded character.
            if (newEpoch == currentEpoch && currentEpoch != 0)
            {
                Info("[AP] session reconnect (same epoch " + newEpoch + "); eligibility retained");
                return;
            }
            // New epoch: invalidate.
            validatedOutcome = ValidationOutcome.Defer;
            validatedPlayerRef = null;
            currentEpoch = newEpoch;
            Info("[AP] eligibility invalidated: session change (epoch " + newEpoch + ")");
        }

        /// <summary>
        /// Called after validation runs: record the outcome and bind it to
        /// the current player/guid/epoch/load.
        /// </summary>
        public void OnValidation(ValidationOutcome outcome, object playerRef, int guid, long epoch, long loadEventId)
        {
            validatedOutcome = outcome == ValidationOutcome.Pass ? outcome : ValidationOutcome.Defer;
            validatedPlayerRef = playerRef;
            validatedGuid = guid;
            validatedEpoch = epoch;
            validatedLoadEventId = loadEventId;
            // Update current epoch to match (validation is for current session).
            currentEpoch = epoch;

            if (outcome == ValidationOutcome.Pass)
                Info("[AP] eligibility granted: validation " + outcome);
            else
                Warn("[AP] eligibility blocked: validation " + outcome);
        }

        /// <summary>
        /// Check if grants are currently allowed.
        /// All conditions must hold; otherwise blocked.
        /// </summary>
        public bool IsEligible(object playerRef, int guid, long epoch, long loadEventId)
        {
            // Must have a completed load (not just started).
            if (!loadCompleted)
                return false;

            // Load event must match current (no new load started).
            if (loadEventId != currentLoadEventId)
                return false;

            // Player reference must match validated.
            if (!ReferenceEquals(playerRef, validatedPlayerRef))
                return false;

            // GUID must match.
            if (guid != validatedGuid)
                return false;

            // Epoch must match.
            if (epoch != validatedEpoch)
                return false;

            // Load event must match (no new load since validation).
            if (loadEventId != validatedLoadEventId)
                return false;

            // Only a confirmed Pass authorizes delivery.
            return validatedOutcome == ValidationOutcome.Pass && epoch > 0 && guid != 0 && playerRef != null && ReferenceEquals(playerRef, currentPlayerRef) && guid == currentGuid && epoch == currentEpoch;
        }

        /// <summary>
        /// Force block (e.g., on error). Invalidates the validated outcome.
        /// </summary>
        private void Info(string s) { try { logInfo?.Invoke(s); } catch { } }
        private void Warn(string s) { try { logWarning?.Invoke(s); } catch { } }
        public void Block(string reason)
        {
            validatedOutcome = ValidationOutcome.Defer;
            validatedPlayerRef = null;
            Warn("[AP] eligibility blocked: " + reason);
        }
    }
}


