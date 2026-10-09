// Skill catalog and enforcement tests.
// Tests the production SkillEnforcement paths:
// - ParseSkillCatalog (the embedded loader's parser)
// - DecideSkillLearn (the hook's decision logic) with injected failures

using System;
using System.Collections.Generic;
using System.IO;
using CraftopiaArchipelago;

class SkillCatalogTests
{
    static int passed = 0;
    static int failed = 0;

    static void Check(bool condition, string name)
    {
        if (condition) { passed++; Console.WriteLine($"PASS {name}"); }
        else { failed++; Console.WriteLine($"FAIL {name}"); }
    }

    static int Main()
    {
        string catalogPath = Path.Combine(
            Environment.GetEnvironmentVariable("SKILL_CATALOG_PATH") ??
            "worlds/craftopia/skill_catalog_20261008_r1.json");
        string json = File.ReadAllText(catalogPath);

        // === ParseSkillCatalog (production parser) ===

        var parsed = SkillEnforcement.ParseSkillCatalog(json, "20261008_r1", m => { });
        Check(parsed != null, "production parser succeeds");
        Check(parsed.Count == 241, $"241 skills via production parser (got {parsed?.Count})");

        // Wrong catalog ID rejected.
        var wrongId = SkillEnforcement.ParseSkillCatalog(json, "wrong_id", m => { });
        Check(wrongId == null, "wrong catalog_id rejected");

        // Invalid JSON rejected.
        var badJson = SkillEnforcement.ParseSkillCatalog("not json", "20261008_r1", m => { });
        Check(badJson == null, "invalid JSON rejected");

        // Missing skills rejected.
        var noSkills = SkillEnforcement.ParseSkillCatalog(
            "{\"catalog_id\":\"20261008_r1\"}", "20261008_r1", m => { });
        Check(noSkills == null, "missing skills rejected");

        // Spot-check via production parser.
        Check(parsed.TryGetValue(840, out var combo) &&
              combo.Category == "Combat" && combo.Tier == 2,
            "production parser: Combo Strike (840) is Combat T2");

        // === DecideSkillLearn (production hook logic) ===

        var perms = new PermissionManager(m => { }, m => { });
        Func<int, SkillEnforcement.SkillInfo> resolve = sid =>
            parsed.TryGetValue(sid, out var si) ? si : null;
        Action<string> log = m => { };
        Action<string> show = m => { };

        // 1. Not bound: allow (fail-open).
        Check(SkillEnforcement.DecideSkillLearn(false, null, resolve, 840, log, show),
            "unbound: allow (even with null permissions)");

        // 2. Bound, permissions null: DENY (fail-closed).
        Check(!SkillEnforcement.DecideSkillLearn(true, null, resolve, 840, log, show),
            "bound + null permissions: deny");

        // 3. Bound, unknown skill ID: DENY.
        Check(!SkillEnforcement.DecideSkillLearn(true, perms, resolve, 999999, log, show),
            "bound + unknown ID: deny");

        // 4. Bound, Basic skill: allow (always unlocked).
        // Find a Basic skill ID from the catalog.
        int basicId = 0;
        foreach (var kv in parsed)
            if (kv.Value.Category == "Basic") { basicId = kv.Key; break; }
        Check(basicId != 0, "found Basic skill in catalog");
        Check(SkillEnforcement.DecideSkillLearn(true, perms, resolve, basicId, log, show),
            "bound + Basic: allow");

        // 5. Bound, Combat T2 without unlock: DENY.
        Check(!SkillEnforcement.DecideSkillLearn(true, perms, resolve, 840, log, show),
            "bound + Combat T2 locked: deny");

        // 6. Bound, Combat T2 with unlock: allow.
        perms.UnlockSkillTree("Skill Tree: Combat Tier 2");
        Check(SkillEnforcement.DecideSkillLearn(true, perms, resolve, 840, log, show),
            "bound + Combat T2 unlocked: allow");

        // 7. Injected failure: resolve throws -> DENY (not allow).
        Func<int, SkillEnforcement.SkillInfo> throwingResolve = sid =>
            throw new InvalidOperationException("injected");
        Check(!SkillEnforcement.DecideSkillLearn(true, perms, throwingResolve, 840, log, show),
            "bound + resolve throws: deny");

        // 8. SafeDiagnostic never throws (even if callbacks throw).
        bool safeOk = true;
        try
        {
            SkillEnforcement.SafeDiagnostic(
                m => throw new InvalidOperationException("injected"),
                m => throw new InvalidOperationException("injected"),
                "test");
        }
        catch { safeOk = false; }
        Check(safeOk, "SafeDiagnostic swallows callback exceptions");

        // 9. Injected failure: logInfo throws -> still DENY (diagnostics non-throwing).
        Action<string> throwingLog = m => throw new InvalidOperationException("injected");
        // Use a locked skill so we hit the diagnostic path.
        var freshPerms = new PermissionManager(m => { }, m => { });
        Check(!SkillEnforcement.DecideSkillLearn(true, freshPerms, resolve, 840, throwingLog, show),
            "bound + log throws: still deny");

        // 10. Injected failure: showBlocked throws -> still DENY.
        Action<string> throwingShow = m => throw new InvalidOperationException("injected");
        Check(!SkillEnforcement.DecideSkillLearn(true, freshPerms, resolve, 840, log, throwingShow),
            "bound + showBlocked throws: still deny");

        // 11. Unbound with throwing resolve: allow (fail-open when unbound).
        Check(SkillEnforcement.DecideSkillLearn(false, perms, throwingResolve, 840, log, show),
            "unbound + resolve throws: allow (vanilla)");

        // === Binding policy (recovery hold must not unbind) ===

        // Bound + recovery hold: still bound for enforcement.
        Check(SkillEnforcement.IsEnforcementBound(true),
            "bound session: enforcement bound");
        // The policy is independent of hold/disconnect state.
        // (Plugin.IsSessionBound implements this; hold only affects delivery.)

        // Delivery eligibility: bound + hold = not eligible.
        Check(!SkillEnforcement.IsDeliveryEligible(true, true),
            "bound + hold: delivery not eligible");
        Check(SkillEnforcement.IsDeliveryEligible(true, false),
            "bound + no hold: delivery eligible");
        Check(!SkillEnforcement.IsDeliveryEligible(false, false),
            "unbound: delivery not eligible");

        // Bound + hold: DecideSkillLearn still enforces (does not go vanilla).
        // Simulate: sessionBound=true (hold does not affect this).
        bool boundDuringHold = SkillEnforcement.IsEnforcementBound(true);
        Check(!SkillEnforcement.DecideSkillLearn(
                boundDuringHold, freshPerms, resolve, 840, log, show),
            "bound + hold: locked skill still denied (not vanilla)");

        // Genuinely unbound: vanilla.
        Check(SkillEnforcement.DecideSkillLearn(
                SkillEnforcement.IsEnforcementBound(false), perms, resolve, 840, log, show),
            "genuinely unbound: vanilla allowed");

        Console.WriteLine($"\nAssertions: {passed + failed}, failures: {failed}");
        return failed == 0 ? 0 : 1;
    }

}
