using System;
using System.Collections.Generic;
using System.IO;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Orchestrates the grant transaction with explicit phases.
    ///
    /// Phases:
    /// 1. INTENT: Write durable intent BEFORE any mutation. If we crash here,
    ///    no mutation happened.
    /// 2. MUTATE: Perform the game mutation ONCE. Track in-memory that mutation
    ///    happened for this intent (by OwnerOpId). Never re-mutate for the same
    ///    intent, even on retry.
    /// 3. PERSIST: Atomically commit entitlement (cumulative) + applied indices.
    ///    This phase may be RETRIED (persist-only, never re-mutate).
    /// 4. CLEAR: Delete the intent file. Only after persist succeeds.
    /// 5. ACK: Return indices for acknowledgment. Only after clear succeeds.
    ///
    /// In-memory operation tracking (by OwnerOpId):
    /// - If mutation happened but persist failed, retry ONLY the persist phase.
    /// - If mutation was Ambiguous, block immediately (do not retry).
    /// - Another batch may NOT overwrite an unresolved intent.
    ///
    /// The applied indices are persisted via the callback, which must write
    /// the ACTUAL applied set (not a snapshot taken before the mutation).
    /// </summary>
    public class GrantTransaction
    {
        private readonly string intentPath;
        private readonly EntitlementStore entitlementStore;
        // Atomic enrollment state (preferred for new enrollments).
        // If set, used instead of entitlementStore for commits.
        private readonly EnrollmentState enrollmentState;
        private readonly string slotName;
        private readonly Func<List<long>, bool> persistApplied;
        private readonly Action<string> logInfo;
        private readonly Action<string> logError;

        // In-memory: OwnerOpId -> operation state.
        // Tracks whether mutation happened, so we never re-mutate.
        private readonly Dictionary<string, OpState> activeOps =
            new Dictionary<string, OpState>();

        // Committed (guid, epoch, index) for dedup. Bound to full identity.
        private readonly HashSet<string> committedKeys = new HashSet<string>();

        private class OpState
        {
            public GrantIntent Intent;
            public bool MutationHappened;
            public bool MutationAmbiguous;
            public List<long> MutatedIndices;
            public bool EntitlementCommitted; // True if Commit succeeded.
            public bool AppliedPersisted;     // True if persistApplied succeeded.
        }

        public GrantTransaction(
            string intentPath,
            EntitlementStore entitlementStore,
            Func<List<long>, bool> persistApplied,
            Action<string> logInfo,
            Action<string> logError,
            EnrollmentState enrollmentState = null,
            string slotName = null)
        {
            this.intentPath = intentPath;
            this.entitlementStore = entitlementStore;
            this.persistApplied = persistApplied;
            this.logInfo = logInfo;
            this.logError = logError;
            this.enrollmentState = enrollmentState;
            this.slotName = slotName;
        }

        public bool CheckForIncompleteIntent(out GrantIntent intent)
        {
            intent = null;
            try
            {
                if (!File.Exists(intentPath))
                    return false;
                string json = File.ReadAllText(intentPath);
                var data = MiniJson.ParseObject(json);
                if (data == null)
                {
                    logError("[AP] intent file corrupt; recovery hold required");
                    return true;
                }
                intent = GrantIntent.FromDict(data);
                // intent may be null if FromDict failed; that's still a hold.
                if (intent == null)
                {
                    logError("[AP] intent file unreadable; recovery hold required");
                    return true;
                }
                logInfo("[AP] incomplete intent found; recovery hold required");
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] intent check failed: " + e.GetType().Name + "; recovery hold");
                return true;
            }
        }

        /// <summary>
        /// Execute the transaction. Returns indices safe to ack.
        /// Never re-mutates for the same intent; persist phase may retry.
        /// </summary>
        public List<long> Execute(
            List<long> indices,
            int guid,
            long epoch,
            int pointsPerIndex,
            Func<List<long>, GrantApplyResult> applicator)
        {
            var toAck = new List<long>();
            if (indices == null || indices.Count == 0)
                return toAck;

            // Dedup: check committed keys (bound to guid+epoch+index).
            var fresh = new List<long>();
            foreach (var idx in indices)
            {
                string key = $"{guid}:{epoch}:{idx}";
                if (!committedKeys.Contains(key))
                    fresh.Add(idx);
            }
            if (fresh.Count == 0)
            {
                // All already committed; safe to re-ack (idempotent).
                toAck.AddRange(indices);
                return toAck;
            }

            // Check for unresolved intent.
            // If the incoming batch matches an active op (same indices, guid,
            // epoch), this is a retry of the persist phase — continue it.
            // Otherwise, block (do not overwrite another batch's intent).
            if (activeOps.Count > 0)
            {
                // Find matching op (same guid, epoch, and indices).
                OpState matchingOp = null;
                foreach (var kv in activeOps)
                {
                    var candidate = kv.Value;
                    if (candidate.Intent.Guid == guid &&
                        candidate.Intent.Epoch == epoch &&
                        SetsEqual(candidate.Intent.Indices, fresh))
                    {
                        matchingOp = candidate;
                        break;
                    }
                }
                if (matchingOp != null)
                {
                    // This is a retry of the same operation. Continue the
                    // persist phase (do NOT re-mutate).
                    logInfo("[AP] retrying persist phase for active operation");
                    return RetryPersist(matchingOp);
                }
                else
                {
                    logError("[AP] unresolved intent in progress; blocking new batch");
                    return toAck; // empty; retry later
                }
            }

            // Phase 1: Write intent.
            var intent = new GrantIntent
            {
                Indices = new List<long>(fresh),
                Guid = guid,
                Epoch = epoch,
                PointsPerIndex = pointsPerIndex,
                Timestamp = DateTime.UtcNow,
                OwnerOpId = Guid.NewGuid().ToString(),
            };
            if (!WriteIntent(intent))
            {
                logError("[AP] intent write failed; aborting, no mutation");
                return toAck;
            }

            var op = new OpState { Intent = intent };
            activeOps[intent.OwnerOpId] = op;

            // Phase 2: Mutate (once).
            GrantApplyResult result;
            try
            {
                result = applicator(fresh);
            }
            catch (Exception e)
            {
                logError("[AP] mutation threw: " + e.GetType().Name);
                // Mutation may have happened; mark ambiguous.
                op.MutationAmbiguous = true;
                // Do NOT clear intent; require recovery.
                // Remove from activeOps to allow recovery, but intent file remains.
                activeOps.Remove(intent.OwnerOpId);
                return toAck;
            }

            if (result == GrantApplyResult.Ambiguous)
            {
                logError("[AP] mutation ambiguous; blocking, intent remains");
                op.MutationAmbiguous = true;
                activeOps.Remove(intent.OwnerOpId);
                // Intent file remains for recovery.
                return toAck;
            }

            if (result != GrantApplyResult.Confirmed)
            {
                // Retryable: mutation did not happen (or failed before mutation).
                // Clear intent, remove op.
                ClearIntent();
                activeOps.Remove(intent.OwnerOpId);
                return toAck;
            }

            // Mutation confirmed.
            op.MutationHappened = true;
            op.MutatedIndices = new List<long>(fresh);

            // Phase 3: Persist (may retry; never re-mutate).
            // The persist callback must write the actual applied state.
            // We call it with the mutated indices; the callback is responsible
            // for merging with existing state and writing atomically.
            int totalPoints = fresh.Count * pointsPerIndex;
            bool entitlementOk = false;
            try
            {
                // Prefer atomic enrollment state (with per-index dedup).
                if (enrollmentState != null && slotName != null)
                {
                    entitlementOk = true;
                    foreach (long idx in fresh)
                    {
                        // Each index committed individually; retry is idempotent.
                        if (!enrollmentState.CommitEntitlement(
                                guid, epoch, slotName, pointsPerIndex, idx))
                        {
                            entitlementOk = false;
                            break;
                        }
                    }
                }
                else
                {
                    entitlementOk = entitlementStore.Commit(guid, epoch, totalPoints);
                }
            }
            catch (Exception e)
            {
                logError("[AP] entitlement commit threw: " + e.GetType().Name);
            }

            if (!entitlementOk)
            {
                logError("[AP] entitlement commit failed; will retry persist only");
                // Keep op in activeOps; do NOT re-mutate on retry.
                return toAck;
            }
            op.EntitlementCommitted = true;

            bool appliedOk = false;
            try
            {
                appliedOk = persistApplied(fresh);
            }
            catch (Exception e)
            {
                logError("[AP] applied persist threw: " + e.GetType().Name);
            }

            if (!appliedOk)
            {
                logError("[AP] applied persist failed; will retry persist only");
                // Entitlement was committed; on retry we skip it (idempotent
                // via the EntitlementCommitted flag).
                // Keep op in activeOps for retry.
                return toAck;
            }
            op.AppliedPersisted = true;

            // Phase 4: Clear intent.
            // If clear fails, RETAIN the live op (do not remove from activeOps).
            // The next Execute with matching indices will retry only the clear.
            if (!ClearIntent())
            {
                logError("[AP] intent clear failed; NOT acking; will retry clear only");
                // Keep op in activeOps; do NOT remove.
                return toAck;
            }

            // Phase 5: Mark committed, return acks.
            activeOps.Remove(intent.OwnerOpId);
            foreach (var idx in fresh)
            {
                string key = $"{guid}:{epoch}:{idx}";
                committedKeys.Add(key);
            }
            toAck.AddRange(fresh);
            return toAck;
        }

        /// <summary>
        /// Retry the persist phase for an active operation.
        /// Called when Execute finds an active op for the same indices.
        /// Never re-mutates.
        /// </summary>
        private List<long> RetryPersist(OpState op)
        {
            var toAck = new List<long>();
            var intent = op.Intent;

            // Skip entitlement commit if already done (idempotent).
            if (!op.EntitlementCommitted)
            {
                int totalPoints = intent.Indices.Count * intent.PointsPerIndex;
                bool entitlementOk = false;
                try
                {
                    if (enrollmentState != null && slotName != null)
                    {
                        entitlementOk = true;
                        foreach (long idx in intent.Indices)
                        {
                            if (!enrollmentState.CommitEntitlement(
                                    intent.Guid, intent.Epoch, slotName,
                                    intent.PointsPerIndex, idx))
                            {
                                entitlementOk = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        entitlementOk = entitlementStore.Commit(
                            intent.Guid, intent.Epoch, totalPoints);
                    }
                }
                catch { }

                if (!entitlementOk)
                    return toAck;
                op.EntitlementCommitted = true;
            }

            // Skip applied persist if already done.
            if (!op.AppliedPersisted)
            {
                bool appliedOk = false;
                try
                {
                    appliedOk = persistApplied(intent.Indices);
                }
                catch { }

                if (!appliedOk)
                    return toAck;
                op.AppliedPersisted = true;
            }

            if (!ClearIntent())
                return toAck;

            activeOps.Remove(intent.OwnerOpId);
            foreach (var idx in intent.Indices)
            {
                string key = $"{intent.Guid}:{intent.Epoch}:{idx}";
                committedKeys.Add(key);
            }
            toAck.AddRange(intent.Indices);
            return toAck;
        }

        private bool WriteIntent(GrantIntent intent)
        {
            try
            {
                // Do not overwrite an existing intent file.
                if (File.Exists(intentPath))
                {
                    logError("[AP] intent file already exists; refusing to overwrite");
                    return false;
                }
                string json = MiniJson.Stringify(intent.ToDict());
                string tmp = intentPath + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, intentPath);
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] intent write failed: " + e.GetType().Name);
                return false;
            }
        }

        private bool ClearIntent()
        {
            try
            {
                if (File.Exists(intentPath))
                    File.Delete(intentPath);
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] intent clear failed: " + e.GetType().Name);
                return false; // Do NOT ack if clear fails.
            }
        }

        private static bool SetsEqual(List<long> a, List<long> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            var setA = new HashSet<long>(a);
            var setB = new HashSet<long>(b);
            return setA.SetEquals(setB);
        }

        /// <summary>
        /// Clear committed keys for a new session/character.
        /// Called on session change or character switch.
        /// </summary>
        public void ClearCommittedForNewSession()
        {
            committedKeys.Clear();
            logInfo("[AP] committed dedup cleared for new session");
        }
    }

    public class GrantIntent
    {
        public List<long> Indices;
        public int Guid;
        public long Epoch;
        public int PointsPerIndex;
        public DateTime Timestamp;
        public string OwnerOpId;

        public Dictionary<string, object> ToDict()
        {
            var d = new Dictionary<string, object>();
            d["indices"] = new List<object>(Indices.ConvertAll(x => (object)x));
            d["guid"] = Guid;
            d["epoch"] = Epoch;
            d["pointsPerIndex"] = PointsPerIndex;
            d["timestamp"] = Timestamp.ToString("o");
            d["ownerOpId"] = OwnerOpId;
            return d;
        }

        public static GrantIntent FromDict(Dictionary<string, object> d)
        {
            try
            {
                var intent = new GrantIntent();
                var idxList = d["indices"] as List<object>;
                intent.Indices = new List<long>();
                foreach (var o in idxList)
                    intent.Indices.Add(Convert.ToInt64(o));
                intent.Guid = Convert.ToInt32(d["guid"]);
                intent.Epoch = Convert.ToInt64(d["epoch"]);
                intent.PointsPerIndex = Convert.ToInt32(d["pointsPerIndex"]);
                intent.Timestamp = DateTime.Parse(d["timestamp"].ToString());
                intent.OwnerOpId = d["ownerOpId"].ToString();
                return intent;
            }
            catch
            {
                return null;
            }
        }
    }
}
