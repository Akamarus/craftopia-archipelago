using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CraftopiaArchipelago;
using HarmonyLib;

// Headless tests for GrantPipeline, the exact class compiled into the mod.
// Each test prints PASS/FAIL with observed values; the process exits nonzero
// on any failure.
internal static class Program
{
    private static int failures;

    // Fake game world: controllable apply/persist outcomes.
    private sealed class FakeWorld
    {
        public int PointsAdded;
        public int ApplyCalls;
        public int ApplyFailuresRemaining;
        public int PersistFailuresRemaining;
        public int AmbiguousFailuresRemaining;

        public GrantApplyResult TryApply(List<long> indices)
        {
            ApplyCalls++;
            if (ApplyFailuresRemaining > 0) { ApplyFailuresRemaining--; return GrantApplyResult.Retryable; }
            if (AmbiguousFailuresRemaining > 0) { AmbiguousFailuresRemaining--; return GrantApplyResult.Ambiguous; }
            PointsAdded += indices.Count;
            return GrantApplyResult.Confirmed;
        }

        public bool TryPersist()
        {
            if (PersistFailuresRemaining > 0) { PersistFailuresRemaining--; return false; }
            return true;
        }
    }

    private static void Check(bool cond, string label, string detail)
    {
        if (cond) Console.WriteLine("  PASS " + label + "  [" + detail + "]");
        else { Console.WriteLine("  FAIL " + label + "  [" + detail + "]"); failures++; }
    }

    private static string Idx(List<long> l) { return "[" + string.Join(",", l) + "]"; }

    private static int Main()
    {
        TestBasicFlow();
        TestDuplicateBeforeApply();
        TestDuplicateIndicesInMessage();
        TestAckLostResend();
        TestEpochRotation();
        TestApplyFailureRetry();
        TestPersistFailureBlocksAck();
        TestPartialOverlap();
        TestRestore();
        TestNoPlayerSkipsApply();
        TestUnpersistedDuplicateNotAcked();
        TestThreadHandoff();
        TestEpochChangeWhilePending();
        TestHookRegistrationDiscovery();
        TestAssignmentBoundary();
        TestReadinessGating();
        TestStalePlayerRejected();
        TestAmbiguousStopsDelivery();
        TestApplicatorVerifiesBalance();
        TestMutateThenThrowIsAmbiguous();
        TestQuarantinePersistence();
        TestPollReadinessLogsWithoutJobs();

        Console.WriteLine(failures == 0 ? "ALL PIPELINE TESTS PASSED" : failures + " TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void TestAssignmentBoundary()
    {
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var patch = typeof(CraftopiaArchipelago.Patches).GetMethod("TryAssignSkillPointPrefix", flags);
        Check(patch != null, "UI-assignment-prefix-exists", "normal menu bypasses TryLearnSkill");
        if (patch == null) return;
        var attrs = patch.GetCustomAttributes(typeof(HarmonyLib.HarmonyPatch), false);
        var info = ((HarmonyLib.HarmonyPatch)attrs[0]).info;
        Check(info.declaringType == typeof(Oc.Skills.OcSkillManager) && info.methodName == "TryAssignSkillPoint",
            "assignment-target", "shared UI and wrapper boundary");
        Check(info.argumentTypes != null && info.argumentTypes.Length == 3 &&
            info.argumentTypes[0] == typeof(Oc.Skills.OcSkill) && info.argumentTypes[1] == typeof(int) &&
            info.argumentTypes[2] == typeof(bool), "assignment-signature", "exact overload");
        var bound = typeof(Plugin).GetField("sessionBound", flags);
        var hold = typeof(Plugin).GetField("recoveryHold", flags);
        var oldBound = bound.GetValue(null); var oldHold = hold.GetValue(null);
        try
        {
            bound.SetValue(null, false);
            object[] args = { null, true };
            Check((bool)patch.Invoke(null, args), "unbound-assignment-vanilla", "null object not accessed");
            bound.SetValue(null, true); hold.SetValue(null, true);
            args = new object[] { null, true };
            Check(!(bool)patch.Invoke(null, args) && !(bool)args[1], "bound-assignment-error-denies",
                "skip original and return false even with hold/null diagnostics");
        }
        finally { bound.SetValue(null, oldBound); hold.SetValue(null, oldHold); }
    }

    private static void TestBasicFlow()
    {
        Console.WriteLine("TestBasicFlow: grant [0,1] -> apply once, ack once");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        var r = p.OnGrantItems(1, new List<long> { 0, 1 });
        Check(r.Reack.Count == 0 && r.FreshQueued == 2, "receipt", "reack=" + Idx(r.Reack) + " fresh=" + r.FreshQueued);
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 2 && w.ApplyCalls == 1, "applied-once", "points=" + w.PointsAdded + " calls=" + w.ApplyCalls);
        Check(acks.Count == 2 && acks[0] == 0 && acks[1] == 1, "acked", "acks=" + Idx(acks));
        Check(p.QueuedJobCount == 0, "queue-drained", "jobs=" + p.QueuedJobCount);
    }

    private static void TestDuplicateBeforeApply()
    {
        Console.WriteLine("TestDuplicateBeforeApply: same grant twice before any frame");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.OnGrantItems(1, new List<long> { 0, 1 });
        var r2 = p.OnGrantItems(1, new List<long> { 0, 1 });
        Check(r2.Reack.Count == 0 && r2.FreshQueued == 0, "dup-dropped-as-pending", "reack=" + Idx(r2.Reack) + " fresh=" + r2.FreshQueued);
        Check(p.QueuedJobCount == 1, "single-job", "jobs=" + p.QueuedJobCount);
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 2, "applied-once", "points=" + w.PointsAdded);
        Check(acks.Count == 2, "acked-once", "acks=" + Idx(acks));
    }

    private static void TestDuplicateIndicesInMessage()
    {
        Console.WriteLine("TestDuplicateIndicesInMessage: grant [5,5,5]");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.OnGrantItems(1, new List<long> { 5, 5, 5 });
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 1, "applied-once", "points=" + w.PointsAdded);
        Check(acks.Count == 1 && acks[0] == 5, "acked-once", "acks=" + Idx(acks));
    }

    private static void TestAckLostResend()
    {
        Console.WriteLine("TestAckLostResend: ack lost, client re-sends");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.OnGrantItems(1, new List<long> { 0, 1 });
        p.OnFrame(0, w.TryApply, w.TryPersist); // applied + acked (ack "lost" on the wire)
        var r = p.OnGrantItems(1, new List<long> { 0, 1 });
        Check(r.Reack.Count == 2 && r.FreshQueued == 0, "re-acked-not-requeued", "reack=" + Idx(r.Reack));
        var acks = p.OnFrame(100, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 2 && acks.Count == 0, "no-double-apply", "points=" + w.PointsAdded);
    }

    private static void TestEpochRotation()
    {
        Console.WriteLine("TestEpochRotation: new epoch clears applied set (new seed)");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.OnGrantItems(7, new List<long> { 0 });
        p.OnFrame(0, w.TryApply, w.TryPersist);
        var r = p.OnGrantItems(8, new List<long> { 0 });
        Check(r.Reack.Count == 0 && r.FreshQueued == 1, "old-index-fresh-under-new-epoch", "reack=" + Idx(r.Reack));
        var acks = p.OnFrame(100, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 2, "applied-again-for-new-seed", "points=" + w.PointsAdded);
        Check(acks.Count == 1 && p.Epoch == 8, "acked", "acks=" + Idx(acks) + " epoch=" + p.Epoch);
    }

    private static void TestApplyFailureRetry()
    {
        Console.WriteLine("TestApplyFailureRetry: apply throws twice, then succeeds; never acked early");
        var p = new GrantPipeline();
        var w = new FakeWorld { ApplyFailuresRemaining = 2 };
        p.OnGrantItems(1, new List<long> { 3 });
        var a0 = p.OnFrame(0, w.TryApply, w.TryPersist);       // attempt 1 fails -> backoff to frame 60
        var a30 = p.OnFrame(30, w.TryApply, w.TryPersist);     // inside backoff: no attempt
        var a60 = p.OnFrame(60, w.TryApply, w.TryPersist);      // attempt 2 fails -> backoff to 180
        var a100 = p.OnFrame(100, w.TryApply, w.TryPersist);   // inside backoff: no attempt
        var a180 = p.OnFrame(180, w.TryApply, w.TryPersist);   // attempt 3 succeeds
        Check(w.ApplyCalls == 3, "three-attempts", "calls=" + w.ApplyCalls);
        Check(a0.Count == 0 && a30.Count == 0 && a60.Count == 0 && a100.Count == 0,
            "no-ack-before-success", "acks=" + a0.Count + "," + a30.Count + "," + a60.Count + "," + a100.Count);
        Check(w.PointsAdded == 1 && a180.Count == 1 && a180[0] == 3,
            "acked-once-after-success", "points=" + w.PointsAdded + " acks=" + Idx(a180));
    }

    private static void TestPersistFailureBlocksAck()
    {
        Console.WriteLine("TestPersistFailureBlocksAck: persist fails -> no ack, no re-apply; recovers later");
        var p = new GrantPipeline();
        var w = new FakeWorld { PersistFailuresRemaining = 2 };
        p.OnGrantItems(1, new List<long> { 4 });
        var a0 = p.OnFrame(0, w.TryApply, w.TryPersist);   // applied, persist fails
        var a60 = p.OnFrame(60, w.TryApply, w.TryPersist); // persist fails again; must NOT re-apply
        Check(w.PointsAdded == 1 && w.ApplyCalls == 1, "applied-exactly-once", "points=" + w.PointsAdded + " calls=" + w.ApplyCalls);
        Check(a0.Count == 0 && a60.Count == 0, "no-ack-while-unpersisted", "");
        var a120 = p.OnFrame(120, w.TryApply, w.TryPersist); // persist succeeds
        Check(a120.Count == 1 && a120[0] == 4, "acked-after-persist", "acks=" + Idx(a120));
        Check(w.PointsAdded == 1, "still-once", "points=" + w.PointsAdded);
    }

    private static void TestPartialOverlap()
    {
        Console.WriteLine("TestPartialOverlap: [0] applied, then grant [0,1]");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.OnGrantItems(1, new List<long> { 0 });
        p.OnFrame(0, w.TryApply, w.TryPersist);
        var r = p.OnGrantItems(1, new List<long> { 0, 1 });
        Check(r.Reack.Count == 1 && r.Reack[0] == 0 && r.FreshQueued == 1, "reack-plus-fresh", "reack=" + Idx(r.Reack));
        var acks = p.OnFrame(100, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 2, "only-new-index-applied", "points=" + w.PointsAdded);
        Check(acks.Count == 1 && acks[0] == 1, "acked-new-only", "acks=" + Idx(acks));
    }

    private static void TestRestore()
    {
        Console.WriteLine("TestRestore: reload persisted state, duplicates re-acked without apply");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.Restore(9, new List<long> { 0, 1 });
        var r = p.OnGrantItems(9, new List<long> { 0, 1, 2 });
        Check(r.Reack.Count == 2 && r.FreshQueued == 1, "reack-old-fresh-new", "reack=" + Idx(r.Reack));
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 1, "only-index-2-applied", "points=" + w.PointsAdded);
        Check(acks.Count == 1 && acks[0] == 2, "acked-2", "acks=" + Idx(acks));
    }

    private static void TestNoPlayerSkipsApply()
    {
        Console.WriteLine("TestNoPlayerSkipsApply: null applier waits; persist retry works without player");
        var p = new GrantPipeline();
        var w = new FakeWorld { PersistFailuresRemaining = 1 };
        p.OnGrantItems(1, new List<long> { 6 });
        var a0 = p.OnFrame(0, null, w.TryPersist); // no player: no attempt at all
        Check(w.ApplyCalls == 0 && a0.Count == 0 && p.QueuedJobCount == 1, "waited", "calls=" + w.ApplyCalls);
        var a1 = p.OnFrame(1, w.TryApply, w.TryPersist); // applied, persist fails
        Check(w.PointsAdded == 1 && a1.Count == 0, "applied-not-acked", "points=" + w.PointsAdded);
        var a2 = p.OnFrame(61, null, w.TryPersist); // still no player, but persist retry runs
        Check(a2.Count == 1 && a2[0] == 6, "persist-retry-without-player-acks", "acks=" + Idx(a2));
        Check(w.ApplyCalls == 1, "no-reapply", "calls=" + w.ApplyCalls);
    }

    private static void TestUnpersistedDuplicateNotAcked()
    {
        // Exact regression from the review: a duplicate of an applied-but-
        // unpersisted index must be NEITHER re-applied NOR acknowledged.
        Console.WriteLine("TestUnpersistedDuplicateNotAcked: dup of unpersisted index is not re-acked");
        var p = new GrantPipeline();
        var w = new FakeWorld { PersistFailuresRemaining = 1 };
        p.OnGrantItems(123, new List<long> { 0 });
        var a0 = p.OnFrame(0, w.TryApply, w.TryPersist); // applied, persist fails
        Check(w.PointsAdded == 1 && a0.Count == 0, "applied-not-acked", "points=" + w.PointsAdded);
        var r = p.OnGrantItems(123, new List<long> { 0 }); // duplicate arrives
        Check(r.Reack.Count == 0 && r.FreshQueued == 0, "no-reack-no-requeue",
            "reack=" + Idx(r.Reack) + " fresh=" + r.FreshQueued);
        var a60 = p.OnFrame(60, w.TryApply, w.TryPersist); // persist succeeds now
        Check(w.PointsAdded == 1, "applied-exactly-once", "points=" + w.PointsAdded);
        Check(a60.Count == 1 && a60[0] == 0, "acked-after-persist", "acks=" + Idx(a60));
        var r2 = p.OnGrantItems(123, new List<long> { 0 }); // now it may be re-acked
        Check(r2.Reack.Count == 1 && r2.FreshQueued == 0, "reack-after-persist", "reack=" + Idx(r2.Reack));
    }

    private static void TestThreadHandoff()
    {
        // The real threading pattern: N threads enqueue concurrently (network
        // thread role), the main thread drains and frames. 200 duplicate
        // grants must collapse to exactly one application and one ack.
        Console.WriteLine("TestThreadHandoff: 4 threads x 50 duplicate enqueues");
        var p = new GrantPipeline();
        var threads = new List<Thread>();
        for (int t = 0; t < 4; t++)
        {
            var th = new Thread(() =>
            {
                for (int i = 0; i < 50; i++)
                    p.EnqueueInbound(1, new List<long> { 0, 1, 2 });
            });
            threads.Add(th);
        }
        foreach (var th in threads) th.Start();
        foreach (var th in threads) th.Join();

        var results = p.DrainInbound();
        int totalFresh = 0, totalReack = 0;
        foreach (var r in results) { totalFresh += r.FreshQueued; totalReack += r.Reack.Count; }
        Check(totalFresh == 3 && totalReack == 0 && p.QueuedJobCount == 1,
            "collapsed-to-one-job", "fresh=" + totalFresh + " reack=" + totalReack + " jobs=" + p.QueuedJobCount);

        var w = new FakeWorld();
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 3 && w.ApplyCalls == 1, "applied-once", "points=" + w.PointsAdded);
        Check(acks.Count == 3, "acked-once", "acks=" + Idx(acks));
    }

    private static void TestEpochChangeWhilePending()
    {
        // A session change arriving while work is pending: the old job is
        // dropped with the rotation, the new epoch's grant is applied once,
        // and the ack carries the NEW epoch.
        Console.WriteLine("TestEpochChangeWhilePending: epoch rotates with a queued job");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        p.EnqueueInbound(1, new List<long> { 0 });
        p.EnqueueInbound(2, new List<long> { 0 });
        var results = p.DrainInbound();
        Check(results.Count == 2, "two-results", "n=" + results.Count);
        Check(results[0].FreshQueued == 1 && results[0].AckEpoch == 1, "first-under-epoch-1",
            "fresh=" + results[0].FreshQueued + " epoch=" + results[0].AckEpoch);
        Check(results[1].FreshQueued == 1 && results[1].AckEpoch == 2, "second-under-epoch-2",
            "fresh=" + results[1].FreshQueued + " epoch=" + results[1].AckEpoch);
        Check(p.Epoch == 2 && p.QueuedJobCount == 1, "rotated", "epoch=" + p.Epoch + " jobs=" + p.QueuedJobCount);
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(w.PointsAdded == 1, "applied-once", "points=" + w.PointsAdded);
        Check(acks.Count == 1, "acked-once", "acks=" + Idx(acks));
    }

    // Harmless dummy target for the hook-registration test. Proves
    // registration behavior of the PatchAll discovery path, not any game hook.
    internal static class DummyTarget
    {
        public static int Calls;
        public static void DummyMethod() { Calls++; }
    }

    // Dummy layouts documenting the two annotation patterns. BrokenHookPatches
    // mirrors the v0.3.3 Patches layout (method-level annotations only);
    // FixedHookPatches mirrors the v0.3.4 correction (class-level added).
    internal static class BrokenHookPatches
    {
        [HarmonyPatch(typeof(DummyTarget), nameof(DummyTarget.DummyMethod))]
        [HarmonyPostfix]
        public static void DummyPostfix() { DummyTarget.Calls += 10; }
    }

    [HarmonyPatch]
    internal static class FixedHookPatches
    {
        [HarmonyPatch(typeof(DummyTarget), nameof(DummyTarget.DummyMethod))]
        [HarmonyPostfix]
        public static void DummyPostfix() { DummyTarget.Calls += 100; }
    }

    private static bool HasAttr(MemberInfo m, string attrName)
    {
        // CustomAttributeData reads metadata without instantiating attributes,
        // so no game assemblies need to resolve. HarmonyLib names its
        // attributes without the "Attribute" suffix (HarmonyPatch, ...).
        return CustomAttributeData.GetCustomAttributes(m)
            .Any(a => a.AttributeType.Name == attrName
                || a.AttributeType.Name == attrName + "Attribute");
    }

    private static CustomAttributeData FindAttr(MemberInfo m, string attrName)
    {
        return CustomAttributeData.GetCustomAttributes(m)
            .First(a => a.AttributeType.Name == attrName
                || a.AttributeType.Name == attrName + "Attribute");
    }

    private static void TestHookRegistrationDiscovery()
    {
        // Guards the structural precondition Harmony's PatchAll assembly scan
        // requires: a patch container type must carry a HarmonyPatch
        // attribute, or the scan skips the class and the hook never registers
        // (the v0.3.3 regression: a mission completed 0/5 -> 5/5 with zero AP
        // check messages). The discovery rule is established from the shipped
        // HarmonyX source (PatchClassProcessor gates on annotated types) and
        // the in-game evidence; Harmony's runtime cannot execute on this
        // Linux .NET 8 rig (MethodInvoker NRE in AccessTools' static init —
        // Unity Mono is its supported runtime), so this test asserts the
        // precondition on the real shipping Patches class instead of
        // re-running Harmony's engine. The plugin's InstallAndVerifyHooks is
        // the runtime proof in-game, and the handoff requires its success line.
        Console.WriteLine("TestHookRegistrationDiscovery: Patches annotations");

        var patches = typeof(CraftopiaArchipelago.Patches);
        Check(HasAttr(patches, "HarmonyPatch"),
            "class-level-harmonypatch-present",
            "without it, PatchAll skips the class (v0.3.3 regression)");

        var method = patches.GetMethod("MissionRewardPostfix",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Check(method != null, "postfix-method-exists", "");
        Check(HasAttr(method, "HarmonyPatch"),
            "method-target-annotation-present", "");
        Check(HasAttr(method, "HarmonyPostfix"),
            "method-postfix-annotation-present", "");

        // The class-level attribute must be parameterless: it only marks the
        // class for scanning and must not alter the method-level target.
        var classCad = FindAttr(patches, "HarmonyPatch");
        Check(classCad.ConstructorArguments.Count == 0,
            "class-annotation-parameterless", "");

        // The two annotation layouts and the discovery precondition each
        // satisfies: method-only is not a container, class-annotated is.
        Check(!HasAttr(typeof(BrokenHookPatches), "HarmonyPatch"),
            "method-only-layout-not-a-container", "");
        Check(HasAttr(typeof(FixedHookPatches), "HarmonyPatch"),
            "class-annotated-layout-is-a-container", "");
    }

    // ---- Player-readiness tests ----
    // These simulate readiness via a fake gateway. They prove the gating and
    // application-decision logic headless; they are NOT Unity validation -
    // the real OcPlayerSkillGateway can only be exercised in-game.

    // Fake player for readiness tests: controllable readiness, instance id,
    // scene, and point balance.
    internal sealed class FakeGateway : IPlayerSkillGateway
    {
        public bool Ready;
        public string WhyNot = "save still loading";
        public int InstanceId = 1001;
        public string Scene = "Field";
        public int Points;
        public int AddCalls;
        public readonly List<int> AppliedToInstances = new List<int>();
        public bool SwallowPoints; // AddSkillPoints runs but the balance doesn't move (simulates overwrite)
        public bool MutateThenThrow; // points increment, then the call throws (realistic AddSkillPoint failure)

        public bool IsReady(out string whyNot) { whyNot = WhyNot; return Ready; }
        public int PlayerInstanceId { get { return InstanceId; } }
        public string SceneName { get { return Scene; } }
        public int GetSkillPoints() { return Points; }
        public void AddSkillPoints(int n)
        {
            AddCalls++;
            AppliedToInstances.Add(InstanceId);
            if (!SwallowPoints) Points += n;
            if (MutateThenThrow) throw new InvalidOperationException("simulated post-mutation failure");
        }
    }

    private static void TestReadinessGating()
    {
        Console.WriteLine("TestReadinessGating: queued while not ready, applied once when ready");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        var gw = new FakeGateway { Ready = false };
        var infos = new List<string>(); var errors = new List<string>();
        var app = new PlayerGrantApplicator(gw, m => infos.Add(m), m => errors.Add(m));

        p.OnGrantItems(1, new List<long> { 10, 11 });

        // Not ready: several frames, no application, no ack, still queued.
        for (long f = 0; f < 5; f++)
        {
            var acks = p.OnFrame(f, ids => app.TryApply(ids), w.TryPersist);
            Check(acks.Count == 0, "no-ack-while-not-ready", "frame=" + f);
        }
        Check(gw.AddCalls == 0, "no-apply-while-not-ready", "calls=" + gw.AddCalls);
        Check(p.QueuedJobCount == 1, "still-queued", "jobs=" + p.QueuedJobCount);
        // Readiness transition logged once, not per frame.
        int trans = 0; foreach (var m in infos) if (m.Contains("readiness ->")) trans++;
        Check(trans == 1, "transition-logged-once", "transitions=" + trans);

        // Become ready: applies exactly once, then acks after persist.
        gw.Ready = true;
        var acks2 = p.OnFrame(1000, ids => app.TryApply(ids), w.TryPersist);
        Check(gw.AddCalls == 1, "applied-once", "calls=" + gw.AddCalls);
        Check(gw.Points == 2, "two-points", "points=" + gw.Points);
        Check(acks2.Count == 2, "acked-after-ready", "acks=" + Idx(acks2));
        Check(p.QueuedJobCount == 0, "queue-drained", "");
        // Detail line records instance, scene, balance before/after.
        bool detail = false;
        foreach (var m in infos)
            if (m.Contains("player 1001") && m.Contains("Field") && m.Contains("0 -> 2")) detail = true;
        Check(detail, "application-detail-logged", "");
    }

    private static void TestStalePlayerRejected()
    {
        Console.WriteLine("TestStalePlayerRejected: instance change is never granted to the old player");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        var gw = new FakeGateway { Ready = true, InstanceId = 1001, Points = 10 };
        var app = new PlayerGrantApplicator(gw, m => { }, m => { });

        p.OnGrantItems(1, new List<long> { 20 });
        p.OnFrame(0, ids => app.TryApply(ids), w.TryPersist);
        Check(gw.Points == 11, "first-applied-to-instance-1", "points=" + gw.Points);

        // World reload: new player instance, different balance.
        gw.InstanceId = 2002;
        gw.Points = 5;
        p.OnGrantItems(1, new List<long> { 21 });
        var acks = p.OnFrame(1000, ids => app.TryApply(ids), w.TryPersist);
        Check(gw.AppliedToInstances.Count == 2 && gw.AppliedToInstances[1] == 2002,
            "second-applied-to-current-instance", "instances=[" + string.Join(",", gw.AppliedToInstances) + "]");
        Check(gw.Points == 6, "new-instance-balance", "points=" + gw.Points);
        Check(acks.Count == 1 && acks[0] == 21, "acked", "acks=" + Idx(acks));
    }

    private static void TestAmbiguousStopsDelivery()
    {
        Console.WriteLine("TestAmbiguousStopsDelivery: ambiguous job dropped, quarantined, replay ignored");
        var p = new GrantPipeline();
        var w = new FakeWorld { AmbiguousFailuresRemaining = 1 };
        p.OnGrantItems(1, new List<long> { 30, 31 });
        var acks = p.OnFrame(0, w.TryApply, w.TryPersist);
        Check(acks.Count == 0, "no-ack-on-ambiguous", "");
        Check(p.QueuedJobCount == 0, "job-dropped", "jobs=" + p.QueuedJobCount);
        Check(p.QuarantinedCount == 2, "quarantined", "q=" + p.QuarantinedCount);

        // Server replay of the same indices: neither re-applied nor acked.
        var r = p.OnGrantItems(1, new List<long> { 30, 31 });
        Check(r.FreshQueued == 0 && r.Reack.Count == 0, "replay-ignored",
            "fresh=" + r.FreshQueued + " reack=" + r.Reack.Count);
        var acks2 = p.OnFrame(1000, w.TryApply, w.TryPersist);
        Check(w.ApplyCalls == 1, "no-retry", "calls=" + w.ApplyCalls);
        Check(acks2.Count == 0, "no-ack-on-replay", "");

        // Quarantine survives save/restore.
        var snap = p.Snapshot();
        var p2 = new GrantPipeline();
        p2.Restore(snap.Epoch, snap.Applied, snap.Quarantined);
        var r2 = p2.OnGrantItems(1, new List<long> { 30 });
        Check(r2.FreshQueued == 0 && r2.Reack.Count == 0, "quarantine-persisted", "");
    }

    private static void TestApplicatorVerifiesBalance()
    {
        Console.WriteLine("TestApplicatorVerifiesBalance: confirmed vs ambiguous application");
        var gw = new FakeGateway { Ready = true, Points = 5 };
        var errors = new List<string>();
        var app = new PlayerGrantApplicator(gw, m => { }, m => errors.Add(m));

        var res = app.TryApply(new List<long> { 40, 41 });
        Check(res == GrantApplyResult.Confirmed, "confirmed-on-exact-increase", "res=" + res);
        Check(gw.Points == 7, "balance", "points=" + gw.Points);

        // Simulate the overwrite bug: AddSkillPoints runs but the balance doesn't move.
        gw.SwallowPoints = true;
        var res2 = app.TryApply(new List<long> { 42 });
        Check(res2 == GrantApplyResult.Ambiguous, "ambiguous-on-unconfirmed", "res=" + res2);
        bool logged = false;
        foreach (var m in errors) if (m.Contains("AMBIGUOUS") && m.Contains("42")) logged = true;
        Check(logged, "ambiguous-logged-clearly", "");
    }


    private static void TestMutateThenThrowIsAmbiguous()
    {
        // Codex blocker 1: AddSkillPoints mutates the balance and THEN throws.
        // Retrying would duplicate the grant. Expect: one attempt, Ambiguous,
        // quarantined, no ack, no automatic retry.
        Console.WriteLine("TestMutateThenThrowIsAmbiguous: mutation+throw is not retried");
        var p = new GrantPipeline();
        var w = new FakeWorld();
        var gw = new FakeGateway { Ready = true, Points = 0, MutateThenThrow = true };
        var errors = new List<string>();
        var app = new PlayerGrantApplicator(gw, m => { }, m => errors.Add(m));

        p.OnGrantItems(1, new List<long> { 50, 51 });
        var acks = p.OnFrame(0, ids => app.TryApply(ids), w.TryPersist);
        Check(gw.AddCalls == 1, "one-application-attempt", "calls=" + gw.AddCalls);
        Check(gw.Points == 2, "mutation-observed", "points=" + gw.Points);
        Check(acks.Count == 0, "no-ack", "");
        Check(p.QueuedJobCount == 0, "job-dropped", "jobs=" + p.QueuedJobCount);
        Check(p.QuarantinedCount == 2, "quarantined", "q=" + p.QuarantinedCount);
        bool logged = false;
        foreach (var m in errors) if (m.Contains("AMBIGUOUS") && m.Contains("AddSkillPoints threw")) logged = true;
        Check(logged, "ambiguous-logged", "");

        // Later frames: no retry, no ack.
        var acks2 = p.OnFrame(1000, ids => app.TryApply(ids), w.TryPersist);
        Check(gw.AddCalls == 1, "no-automatic-retry", "calls=" + gw.AddCalls);
        Check(acks2.Count == 0, "no-ack-2", "");
    }

    private static void TestQuarantinePersistence()
    {
        // Codex blocker 2: quarantine must actually be persisted. Tests the
        // real persistence callback: a failed write is retried without
        // re-applying or acking, then restoration uses exactly what was saved.
        Console.WriteLine("TestQuarantinePersistence: quarantine write attempted, retried, restored");
        var p = new GrantPipeline();
        var w = new FakeWorld { AmbiguousFailuresRemaining = 1 };

        int writes = 0;
        int failWrites = 1;
        List<long> savedQuarantined = null;
        long savedEpoch = -1;
        Func<bool> persist = () =>
        {
            writes++;
            if (failWrites > 0) { failWrites--; return false; }
            var snap = p.Snapshot();
            savedEpoch = snap.Epoch;
            savedQuarantined = new List<long>(snap.Quarantined);
            return true;
        };

        p.OnGrantItems(7, new List<long> { 30 });
        var acks = p.OnFrame(0, w.TryApply, persist);
        Check(acks.Count == 0, "no-ack", "");
        Check(writes == 1, "quarantine-write-attempted", "writes=" + writes);
        Check(savedQuarantined == null, "not-saved-yet", "");
        Check(p.QuarantinedCount == 1, "quarantined", "q=" + p.QuarantinedCount);

        // Next frame: no re-apply, no ack; the write is retried and succeeds.
        var acks2 = p.OnFrame(61, w.TryApply, persist);
        Check(w.ApplyCalls == 1, "no-reapply", "calls=" + w.ApplyCalls);
        Check(acks2.Count == 0, "no-ack-2", "");
        Check(writes == 2, "write-retried", "writes=" + writes);
        Check(savedQuarantined != null && savedQuarantined.Count == 1 && savedQuarantined[0] == 30,
            "quarantine-saved", "");

        // A further frame: nothing left to write.
        p.OnFrame(122, w.TryApply, persist);
        Check(writes == 2, "no-write-when-clean", "writes=" + writes);

        // Restore a fresh pipeline from exactly what the callback saved.
        var p2 = new GrantPipeline();
        p2.Restore(savedEpoch, new List<long>(), savedQuarantined);
        var r = p2.OnGrantItems(7, new List<long> { 30 });
        Check(r.FreshQueued == 0 && r.Reack.Count == 0, "restored-quarantine-ignored", "");
    }

    private static void TestPollReadinessLogsWithoutJobs()
    {
        // The main-menu NOT READY line must be guaranteed even with no queued
        // grants (TryApply never runs then). PollReadiness covers it.
        Console.WriteLine("TestPollReadinessLogsWithoutJobs: readiness logged with empty queue");
        var gw = new FakeGateway { Ready = false, WhyNot = "no player at menu" };
        var infos = new List<string>();
        var app = new PlayerGrantApplicator(gw, m => infos.Add(m), m => { });

        app.PollReadiness();
        app.PollReadiness();
        app.PollReadiness();
        int notReady = 0;
        foreach (var m in infos) if (m.Contains("readiness -> NOT READY")) notReady++;
        Check(notReady == 1, "not-ready-logged-once", "lines=" + notReady);

        gw.Ready = true;
        app.PollReadiness();
        int ready = 0;
        foreach (var m in infos) if (m.Contains("readiness -> READY")) ready++;
        Check(ready == 1, "ready-transition-logged", "");
    }

}

