using System;
namespace CraftopiaArchipelago
{
    public sealed class ValidationSnapshot
    {
        public object Player;
        public long LoadEvent;
        public ValidationInput Input;
        public bool Ready;
        public bool AwaitingEnrollment;
        public bool SameBinding(ValidationSnapshot other) => other != null &&
            ReferenceEquals(Player, other.Player) && LoadEvent == other.LoadEvent &&
            Input.Guid == other.Input.Guid && Input.Epoch == other.Input.Epoch;
    }
    public sealed class ValidationInvocation
    {
        internal long Sequence;
        internal ValidationSnapshot Snapshot;
        public bool RunOriginal { get; internal set; }
    }
    // Production policy, also compiled verbatim into the headless regression suite.
    public sealed class ValidationGate
    {
        readonly RecoveryHoldStore holds;
        long sequence;
        public bool CanSoftRetry { get; private set; } = true;
        public ValidationResult Last { get; private set; }
        public ValidationGate(RecoveryHoldStore holds) { this.holds = holds; }
        public void Invalidate(bool transient = true)
        { ++sequence; Last = default(ValidationResult); CanSoftRetry = transient; }
        bool Known(ValidationSnapshot s) => s != null && s.Player != null && s.LoadEvent > 0 &&
            s.Input.Identity == IdentityStatus.Verified && s.Input.Guid != 0 && s.Input.Epoch > 0;
        void Hold(ValidationSnapshot s, string reason, bool slotWide = false)
        {
            Last = new ValidationResult { Outcome = ValidationOutcome.Defer, Reason = reason };
            CanSoftRetry = false;
            if (Known(s)) holds.Hold(s.Input.Guid, s.Input.Epoch, reason, slotWide);
        }
        public ValidationInvocation Begin(ValidationSnapshot s, bool soft)
        {
            Invalidate();
            var call = new ValidationInvocation { Sequence = sequence, Snapshot = s };
            if (!Known(s)) return call;
            if (holds.IsHeld(s.Input.Guid, s.Input.Epoch)) { CanSoftRetry = false; return call; }
            // A specific one-time approval is waiting for load completion. Still Defer:
            // no vanilla validation, success publication, mutation, or acknowledgement.
            if (s.AwaitingEnrollment) return call;
            if (!s.Input.TotalsKnown || (soft && !s.Ready)) return call;
            var result = ValidationDecision.Decide(s.Input);
            if (result.Outcome == ValidationOutcome.Defer) {
                Hold(s, result.Reason, s.Input.Entitlement == EntitlementStatus.StoreUnreadable);
                return call;
            }
            if (result.Outcome == ValidationOutcome.RunVanilla) {
                if (soft) {
                    if (result.Actual != result.VanillaAllowed) { Hold(s, "soft validation mismatch"); return call; }
                    result.Outcome = ValidationOutcome.Pass;
                } else {
                    call.RunOriginal = true;
                    CanSoftRetry = false;
                    return call; // Never publish success before original returns.
                }
            }
            Last = result; CanSoftRetry = false;
            return call;
        }
        public void Complete(ValidationInvocation call, ValidationSnapshot fresh)
        {
            if (call == null || !call.RunOriginal) return;
            if (call.Sequence != sequence) { Invalidate(false); return; }
            if (!Known(fresh) || !call.Snapshot.SameBinding(fresh)) { Invalidate(); return; }
            if (holds.IsHeld(fresh.Input.Guid, fresh.Input.Epoch)) { Invalidate(false); return; }
            var result = ValidationDecision.Decide(fresh.Input);
            bool match = result.Outcome == ValidationOutcome.Pass ||
                (result.Outcome == ValidationOutcome.RunVanilla && result.Actual == result.VanillaAllowed);
            if (!fresh.Input.TotalsKnown || !match) { Hold(fresh, "original validator readback failed"); return; }
            result.Outcome = ValidationOutcome.Pass; Last = result; CanSoftRetry = false;
        }
        public void Failed(ValidationInvocation call)
        {
            Invalidate(false);
            if (call != null) Hold(call.Snapshot, "validator exception");
        }
        public void RetryHoldWrite() { holds.RetryWrite(); }
        public bool IsHeld(int guid, long epoch) => holds.IsHeld(guid, epoch);
    }
}
