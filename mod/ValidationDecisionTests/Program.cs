// Headless tests for the pure validation decision function.
// No Unity, no Harmony, no IO. Each test constructs an explicit
// ValidationInput and asserts the outcome.

using System;
using CraftopiaArchipelago;

static class Program
{
    static int pass = 0;
    static int fail = 0;

    static void Check(bool cond, string name)
    {
        if (cond) { pass++; Console.WriteLine("PASS: " + name); }
        else { fail++; Console.WriteLine("FAIL: " + name); }
    }

    // Baseline: disposable test character, 2 AP points, everything verified.
    // vanillaAllowed = (1-1)*2+0 = 0; apAllowed = 0+2 = 2.
    static ValidationInput Base()
    {
        return new ValidationInput
        {
            Level = 1, K = 2, MissionPoints = 0,
            AssignedPoints = 0, UnspentPoints = 2,
            TotalsKnown = true,
            Identity = IdentityStatus.Verified,
            Guid = -123, Epoch = 7,
            Entitlement = EntitlementStatus.Ok,
            EntitlementPoints = 2,
            BoundGuid = -123, BoundEpoch = 7,
        };
    }

    static int Main()
    {
        // 1. AP-aware matching totals -> Pass.
        {
            var r = ValidationDecision.Decide(Base());
            Check(r.Outcome == ValidationOutcome.Pass, "AP match -> Pass");
            Check(r.VanillaAllowed == 0 && r.Actual == 2, "AP match values vanilla=0 actual=2");
            Check(r.ApAllowedValid && r.ApAllowed == 2, "AP match apAllowed=2");
        }

        // 2. AP discrepancy, actual > apAllowed -> Defer.
        {
            var i = Base();
            i.UnspentPoints = 3; // actual=3, apAllowed=2
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, "AP actual>allowed -> Defer");
        }

        // 3. AP discrepancy, actual < apAllowed -> Defer.
        {
            var i = Base();
            i.UnspentPoints = 1; // actual=1, apAllowed=2
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, "AP actual<allowed -> Defer");
        }

        // 4-8. Every uncertain/held status -> Defer, even with matching totals.
        foreach (var s in new[]
        {
            EntitlementStatus.UncertainHistory,
            EntitlementStatus.StoreUnreadable,
            EntitlementStatus.EpochMismatch,
            EntitlementStatus.PendingIntent,
            EntitlementStatus.Unknown,
        })
        {
            var i = Base();
            i.Entitlement = s;
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, s + " -> Defer");
        }

        // 9. Explicitly verified no-history, matching vanilla totals -> RunVanilla.
        {
            var i = Base();
            i.Entitlement = EntitlementStatus.NoRecordConfirmed;
            i.UnspentPoints = 0; i.EntitlementPoints = 0;
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.RunVanilla, "NoRecordConfirmed match -> RunVanilla");
        }

        // 10. NoRecordConfirmed with vanilla mismatch still delegates
        // (vanilla validator owns the reset for vanilla characters).
        {
            var i = Base();
            i.Entitlement = EntitlementStatus.NoRecordConfirmed;
            i.UnspentPoints = 5; // actual=5, vanillaAllowed=0
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.RunVanilla, "NoRecordConfirmed mismatch -> RunVanilla");
        }

        // 11. Identity unknown -> Defer, even with Ok entitlement and matching totals.
        {
            var i = Base();
            i.Identity = IdentityStatus.Unknown;
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, "Identity Unknown -> Defer");
        }

        // 12. Guid binding mismatch -> Defer.
        {
            var i = Base();
            i.Guid = 456; // observed != bound (-123)
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, "Guid binding mismatch -> Defer");
        }

        // 13. Epoch binding mismatch -> Defer.
        {
            var i = Base();
            i.Epoch = 8; // observed != bound (7)
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, "Epoch binding mismatch -> Defer");
        }

        // 14. Negative GUID is a valid verified identity (not filtered).
        {
            var i = Base();
            i.Guid = -123; i.BoundGuid = -123;
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Pass, "Negative GUID -> Pass");
        }

        // 15. Level scaling: level 2, K=2 -> vanillaAllowed=2.
        {
            var i = Base();
            i.Entitlement = EntitlementStatus.NoRecordConfirmed;
            i.Level = 2; i.UnspentPoints = 2; i.EntitlementPoints = 0;
            var r = ValidationDecision.Decide(i);
            Check(r.VanillaAllowed == 2 && r.Actual == 2, "Level2 vanillaAllowed=2 actual=2");
            Check(r.Outcome == ValidationOutcome.RunVanilla, "Level2 vanilla -> RunVanilla");
        }

        // 16. Mission points feed vanillaAllowed.
        {
            var i = Base();
            i.Entitlement = EntitlementStatus.NoRecordConfirmed;
            i.MissionPoints = 1; i.UnspentPoints = 1; i.EntitlementPoints = 0;
            var r = ValidationDecision.Decide(i);
            Check(r.VanillaAllowed == 1, "Mission points vanillaAllowed=1");
            Check(r.Outcome == ValidationOutcome.RunVanilla, "Mission points -> RunVanilla");
        }

        // 17. AP + assigned points: assigned counts toward actual.
        {
            var i = Base();
            i.AssignedPoints = 1; i.UnspentPoints = 1; // actual=2, apAllowed=2
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Pass, "AP assigned+unspent match -> Pass");
        }

        // 18. Zero entitlement with zero points -> Pass (AP char, nothing granted yet).
        {
            var i = Base();
            i.UnspentPoints = 0; i.EntitlementPoints = 0;
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Pass, "AP zero/zero -> Pass");
        }

        // 19. Totals not known -> Defer (do not use substituted defaults).
        {
            var i = Base();
            i.TotalsKnown = false;
            var r = ValidationDecision.Decide(i);
            Check(r.Outcome == ValidationOutcome.Defer, "TotalsKnown=false -> Defer");
        }

        Console.WriteLine("PASS: " + pass + ", FAIL: " + fail);
        return fail == 0 ? 0 : 1;
    }
}
