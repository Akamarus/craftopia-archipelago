using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Pure item-grant state machine. No Unity, BepInEx, or IO dependencies,
    /// so it can be unit-tested headless. The Plugin wires it to the game
    /// (frame counter, AddSkillPoint, file persistence) and the Bridge wires
    /// it to the websocket (grant_items in, grant_ack out).
    ///
    /// Threading: the network thread MUST only call EnqueueInbound (a
    /// thread-safe queue). Unity's main thread owns everything else:
    /// DrainInbound, OnFrame, Snapshot, Restore. No shared collection is
    /// touched from two threads.
    ///
    /// Delivery contract:
    /// - Item states: pending (queued, not applied) -> dirty (applied in
    ///   memory, not yet durably persisted) -> applied (persisted).
    /// - Every index is APPLIED at most once per epoch (dedup at receipt AND
    ///   a backstop recheck at the point of application, including duplicates
    ///   inside a single message).
    /// - An index is ACKED only after it was applied AND its application was
    ///   durably persisted. Only persisted indices are re-acked: a duplicate
    ///   of an unpersisted index is neither re-applied nor acknowledged.
    /// - Failed applies and failed persists are retried (with backoff); they
    ///   are NEVER acked. No false success.
    /// - A newer epoch means a new seed: applied/dirty/pending/queued state is
    ///   dropped so old indices can't suppress or duplicate new-seed grants.
    /// </summary>
    /// <summary>
    /// Outcome of one grant-application attempt, reported by the applier.
    /// </summary>
    public enum GrantApplyResult
    {
        /// <summary>Points increased by exactly the requested amount: confirmed.</summary>
        Confirmed,
        /// <summary>Not applied (player not ready, or the call failed without
        /// observed mutation): keep queued, retry with backoff, never ack.</summary>
        Retryable,
        /// <summary>The mutation call completed but the balance does not
        /// confirm it: do NOT retry (duplication risk). The pipeline drops the
        /// job without acking and quarantines its indices for manual review.</summary>
        Ambiguous,
    }

    internal sealed class GrantPipeline
    {
        public sealed class Job
        {
            public List<long> Indices;
            public int Attempts;
            public bool AppliedAwaitingPersist;
            public long NotBeforeFrame;
        }

        public sealed class InboundGrant
        {
            public long Epoch;
            public List<long> Indices;
        }

        public sealed class InboundResult
        {
            public readonly List<long> Reack;
            public readonly int FreshQueued;
            public readonly long AckEpoch;
            public InboundResult(List<long> reack, int freshQueued, long ackEpoch)
            {
                Reack = reack;
                FreshQueued = freshQueued;
                AckEpoch = ackEpoch;
            }
        }

        public sealed class PipelineSnapshot
        {
            public readonly long Epoch;
            public readonly List<long> Applied;
            public readonly List<long> Quarantined;
            public PipelineSnapshot(long epoch, List<long> applied, List<long> quarantined)
            {
                Epoch = epoch;
                Applied = applied;
                Quarantined = quarantined;
            }
        }

        // Main-thread-owned state. Only DrainInbound/OnFrame/Snapshot/Restore
        // touch these, and only from the main thread.
        private readonly List<Job> jobs = new List<Job>();
        private readonly HashSet<long> applied = new HashSet<long>();
        private readonly HashSet<long> dirty = new HashSet<long>();
        private readonly HashSet<long> pending = new HashSet<long>();
        private readonly HashSet<long> quarantined = new HashSet<long>();
        private long epoch;
        private bool epochSet;
        // True when quarantined indices were added but their persistence has
        // not been confirmed. Retried independently of grant jobs: no
        // re-apply, no ack, just the write.
        private bool quarantineDirty;
        private long quarantineNotBeforeFrame;

        // Thread-safe boundary: the network thread enqueues here.
        private readonly ConcurrentQueue<InboundGrant> inbound = new ConcurrentQueue<InboundGrant>();

        public long Epoch { get { return epoch; } }
        public int QueuedJobCount { get { return jobs.Count; } }

        /// <summary>
        /// Called from the network thread. Only enqueues; never touches
        /// pipeline state.
        /// </summary>
        public void EnqueueInbound(long msgEpoch, List<long> indices)
        {
            inbound.Enqueue(new InboundGrant { Epoch = msgEpoch, Indices = indices });
        }

        /// <summary>
        /// Called from the main thread. Drains the inbound queue and classifies
        /// each message. Each result carries the epoch in effect for that
        /// message so acks don't read mutable state later.
        /// </summary>
        public List<InboundResult> DrainInbound()
        {
            var results = new List<InboundResult>();
            InboundGrant grant;
            while (inbound.TryDequeue(out grant))
                results.Add(OnGrantItems(grant.Epoch, grant.Indices));
            return results;
        }

        /// <summary>
        /// Classify one grant message (main thread only). Returns what to ack
        /// immediately and how many fresh indices were queued. Already-applied
        /// (persisted) indices are re-acked; dirty/pending ones are dropped
        /// silently — their original job's ack will cover them once persisted.
        /// </summary>
        public InboundResult OnGrantItems(long msgEpoch, List<long> indices)
        {
            if (!epochSet || msgEpoch != epoch)
            {
                epoch = msgEpoch;
                epochSet = true;
                applied.Clear();
                dirty.Clear();
                pending.Clear();
                quarantined.Clear();
                quarantineDirty = false;
                jobs.Clear();
            }

            var distinct = new List<long>();
            foreach (long i in indices)
                if (!distinct.Contains(i)) distinct.Add(i);

            var reack = new List<long>();
            var fresh = new List<long>();
            foreach (long i in distinct)
            {
                if (applied.Contains(i)) reack.Add(i);
                else if (dirty.Contains(i)) { /* applied but unpersisted: no re-ack, no re-apply */ }
                else if (pending.Contains(i)) { /* already queued; covered by that job's ack */ }
                else if (quarantined.Contains(i)) { /* ambiguous earlier: awaiting manual review; never re-apply, never ack */ }
                else fresh.Add(i);
            }

            foreach (long i in fresh) pending.Add(i);
            if (fresh.Count > 0)
                jobs.Add(new Job { Indices = fresh, Attempts = 0, AppliedAwaitingPersist = false, NotBeforeFrame = 0 });

            return new InboundResult(reack, fresh.Count, epoch);
        }

        /// <summary>
        /// Advance one frame (main thread only). tryApply(indices) attempts the
        /// grant and reports Confirmed (balance verified), Retryable (not
        /// applied: keep queued with backoff, never ack), or Ambiguous (may
        /// have applied: drop the job without acking and quarantine the
        /// indices for manual review — never auto-retry). tryApply may be null
        /// when there is no applier: applies are skipped but persist retries
        /// still run. Returns indices to ack (each paired with the current
        /// epoch by the caller).
        /// </summary>
        public List<long> OnFrame(long frame, Func<List<long>, GrantApplyResult> tryApply, Func<bool> tryPersist)
        {
            var acks = new List<long>();
            foreach (Job job in new List<Job>(jobs))
            {
                if (frame < job.NotBeforeFrame) continue;

                if (!job.AppliedAwaitingPersist)
                {
                    if (tryApply == null) continue; // no applier; wait

                    // Backstop recheck at the point of application.
                    var toApply = new List<long>();
                    foreach (long i in job.Indices)
                        if (!applied.Contains(i) && !dirty.Contains(i) && !quarantined.Contains(i)) toApply.Add(i);

                    if (toApply.Count == 0)
                    {
                        // All already applied/quarantined (shouldn't normally happen).
                        var ackable = new List<long>();
                        foreach (long i in job.Indices)
                        {
                            pending.Remove(i);
                            if (!quarantined.Contains(i)) ackable.Add(i);
                        }
                        jobs.Remove(job);
                        acks.AddRange(ackable);
                        continue;
                    }

                    GrantApplyResult res;
                    try { res = tryApply(toApply); }
                    catch
                    {
                        // An unexpected exception escaped the applier: the
                        // mutation call may have been entered, so a retry
                        // could duplicate the grant. Treat as Ambiguous.
                        res = GrantApplyResult.Ambiguous;
                    }

                    if (res == GrantApplyResult.Retryable)
                    {
                        // Not applied (or failed before any mutation was
                        // observed): keep pending, keep queued, NEVER ack.
                        // Backoff: 1s, 2s, ... capped at 10s between attempts.
                        job.Attempts++;
                        job.NotBeforeFrame = frame + 60L * Math.Min(job.Attempts, 10);
                        continue;
                    }

                    if (res == GrantApplyResult.Ambiguous)
                    {
                        // May have applied: do NOT retry (duplication risk).
                        // Drop the job without persisting or acking; quarantine
                        // the indices so a server replay can neither re-apply
                        // nor be falsely acked. Manual review required.
                        // The quarantine persist is attempted below in this
                        // same frame; failures set quarantineDirty for retry
                        // on later frames (write only, never re-apply/ack).
                        foreach (long i in job.Indices)
                        {
                            pending.Remove(i);
                            dirty.Remove(i);
                            quarantined.Add(i);
                        }
                        jobs.Remove(job);
                        quarantineDirty = true;
                        quarantineNotBeforeFrame = 0;
                        continue;
                    }

                    foreach (long i in toApply) dirty.Add(i);
                    job.AppliedAwaitingPersist = true;
                }

                bool saved;
                try { saved = tryPersist(); }
                catch { saved = false; }

                if (!saved)
                {
                    // Applied in memory but not durable: do NOT ack, do NOT
                    // re-apply (dirty set guards the recheck). Retry persist.
                    job.NotBeforeFrame = frame + 60;
                    continue;
                }

                // Durably recorded: now safe to ack and to re-ack later.
                // (The snapshot also carries the quarantine set, so a
                // pending quarantine persist is satisfied too.)
                quarantineDirty = false;
                foreach (long i in job.Indices)
                {
                    pending.Remove(i);
                    dirty.Remove(i);
                    applied.Add(i);
                }
                jobs.Remove(job);
                acks.AddRange(job.Indices);
            }

            // Retry a not-yet-persisted quarantine independently of grant
            // jobs: this only writes state, never re-applies or acks.
            if (quarantineDirty && frame >= quarantineNotBeforeFrame)
            {
                if (TryPersistQuarantine(tryPersist, frame))
                    quarantineDirty = false;
                else
                    quarantineNotBeforeFrame = frame + 60;
            }

            return acks;
        }

        /// <summary>
        /// Attempts one quarantine persist. Returns true when the write
        /// succeeded (or there is nothing to persist). Never throws.
        /// </summary>
        private bool TryPersistQuarantine(Func<bool> tryPersist, long frame)
        {
            bool saved;
            try { saved = tryPersist(); }
            catch { saved = false; }
            return saved;
        }

        /// <summary>
        /// What to write to disk: everything applied-or-dirty, plus the
        /// quarantine set. If the write succeeds, the caller must treat dirty
        /// as applied (it was granted).
        /// </summary>
        public PipelineSnapshot Snapshot()
        {
            var all = new List<long>(applied);
            foreach (long i in dirty)
                if (!all.Contains(i)) all.Add(i);
            return new PipelineSnapshot(epoch, all, new List<long>(quarantined));
        }

        public void Restore(long savedEpoch, List<long> savedApplied, List<long> savedQuarantined = null)
        {
            epoch = savedEpoch;
            epochSet = true;
            applied.Clear();
            foreach (long i in savedApplied) applied.Add(i);
            quarantined.Clear();
            if (savedQuarantined != null)
                foreach (long i in savedQuarantined) quarantined.Add(i);
            dirty.Clear();
            pending.Clear();
            jobs.Clear();
        }

        public int QuarantinedCount { get { return quarantined.Count; } }
    }
}
