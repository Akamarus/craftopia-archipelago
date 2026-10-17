# Progression implementation batch — October 9, 2026

## Approved intent

Continue the agreed design in one batch: verify production locks, model resource/recipe/age prerequisites, add progressive equipment categories, replacement checks and useful filler, and validate both multiplayer modes. Preserve the tested v1.8.9 character/session and all historical state. Do not invent game IDs, tiers, accessibility, or successful runtime results.

## Execution sequence

- [ ] Production: inspect installed method bodies and callers, repair fail-open paths, verify registration, test production decisions and unlock replay.
- [x] Evidence collection: expand the read-only collector and obtain a schema-3 F9 capture with provenance. Raw bench mappings and base mission fields are collected; station binding and subclass prerequisites remain open.
- [ ] Progression/equipment: use captured recipe/age facts to define tiers and access rules; reject unmodeled requirements rather than assuming reachability. Preserve existing catalog IDs and seed compatibility.
- [ ] Rewards/checks: inspect consumption/reward boundaries, implement only adapters with stable identities and verified mutation semantics; material/money delivery must use durable intent, identity binding, and acknowledgments after persistence.
- [ ] Multiplayer: run existing production-path tests, repair actual protocol failures, distinguish headless coverage from cross-client game tests.
- [x] Build/regression for the current slice: all 11 C# programs, 70 client tests and 14 world tests passed. Gameplay build has zero errors/warnings. Further implementation requires further verification.
- [ ] Runtime: stage a combined test/capture session. Check locked station with unchanged materials, unlock delivery, allowed crafting, restart persistence, then collect missing progression evidence.
- [ ] Update current status and GitHub with tested source and sanitized evidence summaries.

## Boundaries

Existing production, delivery, enrollment, and catalog flows are repaired in place. New progression mappings depend on actual data; an observed mission is not proof of its accessibility. A runtime capture or second multiplayer client may require operator action. Until that evidence exists, incomplete features remain visibly unsupported and excluded from claims of playable seeds. No automatic enrollment recovery, hold clearing, ledger reset, or save edits.

## October 10 checkpoint

Production guards and observation custody are implemented and independently reviewed; gameplay deployment/testing is pending. Collector v1.2.0 captured successfully. See [current batch evidence](PROGRESSION_BATCH_20261010.md) for exact counts, test results, and remaining work. The broad batch is not complete.
