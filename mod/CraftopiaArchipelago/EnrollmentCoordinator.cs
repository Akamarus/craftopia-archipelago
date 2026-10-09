using System;
using System.IO;
using System.Text;

namespace CraftopiaArchipelago
{
    // A narrowly scoped first-enrollment flow. No recovery or clearing API.
    public sealed class EnrollmentCoordinator
    {
        readonly EnrollmentState state;
        readonly string receipt, slot;
        readonly int guid;
        readonly long epoch;
        readonly bool approved;
        bool stopped;
        public string Status { get; private set; } = "Awaiting explicit fresh-character approval";
        public EnrollmentCoordinator(EnrollmentState state, string receipt, int guid, long epoch, string slot, bool approved)
        { this.state=state; this.receipt=receipt; this.guid=guid; this.epoch=epoch; this.slot=slot; this.approved=approved; }

        public bool CanWait(ValidationSnapshot s, bool history, bool held)
        {
            try {
                return !stopped && approved && guid != 0 && epoch > 0 && !string.IsNullOrWhiteSpace(slot) &&
                    !history && !held && !File.Exists(receipt) && !Directory.Exists(receipt) &&
                    s != null && s.Player != null && s.LoadEvent > 0 &&
                    s.Input.Identity == IdentityStatus.Verified && s.Input.Guid == guid && s.Input.Epoch == epoch &&
                    state.Read(guid,epoch,slot,false).Status == EntitlementStatus.UncertainHistory;
            } catch { return false; }
        }
        public bool TryEnroll(ValidationSnapshot first, Func<ValidationSnapshot> capture, bool history, bool held)
        {
            if (!CanWait(first,history,held) || !first.Ready) return false;
            stopped = true; // An uncertain write or rejected approval requires review, never an automatic retry.
            try {
                state.Load(); // Never overwrite changes made after startup.
                if (state.Read(guid,epoch,slot,history).Status != EntitlementStatus.UncertainHistory)
                    return Refuse("Enrollment state changed");
                var fresh = capture();
                if (fresh == null || !first.SameBinding(fresh) || !fresh.Ready ||
                    fresh.Input.Identity != IdentityStatus.Verified || !Zero(first) || !Zero(fresh))
                    return Refuse("Character changed or fresh zero-point baseline not proven");
                // CreateNew prevents reuse. Flush before the enrollment write; a crash between
                // these writes leaves a consumed approval and holds, never a reusable attestation.
                byte[] bytes=Encoding.UTF8.GetBytes($"guid={guid};epoch={epoch};slot={slot};approved={DateTime.UtcNow:o}");
                using (var file=new FileStream(receipt,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    file.Write(bytes,0,bytes.Length); file.Flush(true);
                }
                if (!state.Attest(guid,epoch,slot,"Explicitly approved fresh character","one-time coordinator"))
                    return Refuse("Enrollment write failed; approval consumed; review required");
                Status="Fresh-character enrollment committed";
                return true;
            } catch { return Refuse("Enrollment failed; review required"); }
        }
        static bool Zero(ValidationSnapshot s) => s.Input.TotalsKnown && s.Input.Level == 1 &&
            s.Input.K > 0 && s.Input.MissionPoints == 0 && s.Input.AssignedPoints == 0 && s.Input.UnspentPoints == 0;
        bool Refuse(string reason) { Status=reason; return false; }
    }
}
