using System;
using System.IO;
using CraftopiaArchipelago;

static class CoordinatorTests
{
    static int failures, checks;
    static void Check(bool value, string name) { checks++; if (!value) failures++; Console.WriteLine((value ? "PASS " : "FAIL ") + name); }
    static ValidationSnapshot Snapshot(object player = null) => new ValidationSnapshot {
        Player = player ?? new object(), LoadEvent = 1, Ready = true,
        Input = new ValidationInput { Identity = IdentityStatus.Verified, Guid = -7, Epoch = 123,
            TotalsKnown = true, Level = 1, K = 2, Entitlement = EntitlementStatus.UncertainHistory }
    };
    public static int Run()
    {
        var type = typeof(EnrollmentState).Assembly.GetType("CraftopiaArchipelago.EnrollmentCoordinator");
        Check(type != null, "production enrollment coordinator exists");
        if (type == null) return failures;
        foreach (string scenario in new[] { "success", "unapproved", "wrongrequest", "history", "hold", "identity", "epoch", "player", "load", "points", "unknown", "notready", "writefail", "consumed", "corrupt", "receiptfail", "existing" }) {
            string dir = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "enrollment.json"), receipt = Path.Combine(dir, "approval.used");
            if (scenario == "corrupt") File.WriteAllText(path, "broken");
            var state = new EnrollmentState(path, _ => {}, _ => {}); state.Load();
            if (scenario == "writefail") Directory.CreateDirectory(path + ".tmp");
            if (scenario == "consumed") File.WriteAllText(receipt, "previous approval consumed");
            if (scenario == "receiptfail") Directory.CreateDirectory(receipt);
            if (scenario == "existing") state.Attest(-7,123,"Test","already enrolled","test");
            dynamic coordinator = Activator.CreateInstance(type, state, receipt, scenario=="wrongrequest" ? 8 : -7, 123L, "Test", scenario!="unapproved");
            var first = Snapshot(); var second = Snapshot(first.Player);
            if (scenario == "identity") second.Input.Guid = 8;
            if (scenario == "epoch") second.Input.Epoch = 124;
            if (scenario == "player") second.Player = new object();
            if (scenario == "load") second.LoadEvent = 2;
            if (scenario == "unknown") second.Input.TotalsKnown = false;
            if (scenario == "points") first.Input.UnspentPoints = second.Input.UnspentPoints = 1;
            if (scenario == "notready") first.Ready = second.Ready = false;
            bool history = scenario == "history", held = scenario == "hold";
            var holds=new RecoveryHoldStore(Path.Combine(dir,"holds.json"));
            var gate=new ValidationGate(holds);
            first.AwaitingEnrollment=coordinator.CanWait(first,history,held);
            if (scenario == "success") {
                var call=gate.Begin(first,false);
                Check(!call.RunOriginal && gate.Last.Outcome==ValidationOutcome.Defer && gate.CanSoftRetry && !holds.IsHeld(-7,123), "approved load waits without success or recovery hold");
                holds.Hold(-7,123,"test existing hold");
                gate.Begin(first,false);
                Check(!gate.CanSoftRetry && holds.IsHeld(-7,123), "waiting approval never overrides existing hold");
            }
            bool result = coordinator.TryEnroll(first, new Func<ValidationSnapshot>(() => second), history, held);
            Check(result == (scenario == "success"), scenario + " result");
            if (scenario == "success") {
                Check(state.Read(-7,123,"Test",false).Status == EntitlementStatus.NoRecordConfirmed, "fresh enrollment durable");
                Check(File.Exists(receipt), "approval consumed before eligibility");
                File.Delete(path); var restored = new EnrollmentState(path, _=>{}, _=>{}); restored.Load();
                dynamic again = Activator.CreateInstance(type, restored, receipt, -7,123L,"Test",true);
                Check(!again.TryEnroll(first,new Func<ValidationSnapshot>(()=>second),false,false) && !File.Exists(path), "lost store never recreated from retained approval");
            } else {
                Check(!File.Exists(path) || scenario == "corrupt" || scenario == "existing", scenario + " no enrollment write");
                if (scenario == "writefail") Check(File.Exists(receipt), "failed enrollment write leaves consumed approval for review");
            }
        }
        Console.WriteLine($"Coordinator assertions: {checks}, failures: {failures}");
        return failures;
    }
}
