# Current status — v1.9.2 development / v1.8.9 gameplay baseline

Updated October 11, 2026. This document supersedes implementation-status claims in older reports in this repository. The table below remains the v1.8.9 baseline. Controlled v1.9.0 testing additionally verified registration of all 22 production hooks, a successful existing-character load, and a locked campfire attempt denied without consuming ingredients. Other production paths and durable observation acknowledgments still need runtime verification. Collector v1.2.0 captured successfully. See [batch results](PROGRESSION_BATCH_20261010.md).

Equipment follow-up: [v1.9.2 game-facing integration](EQUIPMENT_HOOKS_v192.md) adds seven pre-cost/pre-transfer hooks, full-loadout checking and an explicit character/session/catalog test contract. Equipment remains disabled by default and excluded from generated pools. All 12 C# suites, 82 bridge tests, 22 world tests and eight exporter tests pass; build has zero warnings/errors. Independent review and the wrong-character admission fix are complete. Active equipment enforcement is not runtime-verified; the disabled smoke test passed on October 12. The latest runtime-tested gameplay build is v1.9.2 with equipment restrictions disabled.

## Controlled runtime results

October 12: v1.9.2 disabled-equipment smoke test passed. All 38 registrations verified; original-session character validation and unlock replay passed. Tester confirmed normal crafting/equip/unequip, owned furnace operation, and locked Cooking Pot denial with no ingredient consumption. No Error/FATAL/recovery hold appeared in the inspected log. Active equipment restrictions remain untested. A missing bridge ledger was reconciled before loading; see [runtime evidence and recovery limits](EQUIPMENT_HOOKS_v192.md#october-12-runtime-results).

v1.9.2 was installed after a complete test-state backup; its disabled-equipment smoke test has now passed. See [deployment and test scope](EQUIPMENT_HOOKS_v192.md).

v1.9.1 station-family runtime checks passed by tester confirmation: Stone Furnace consumed normal ingredients and produced output with Smelting owned; Cooking Pot denied a recipe without consuming available ingredients while Cooking was unowned; ordinary Workbench crafting succeeded. The current log corroborates missing-Cooking denials and shows no Error/FATAL/Exception entries. Startup verified all 31 hooks, character identity and matching AP-aware totals. Locked furnace, connector operation and other station families remain unverified.

Collector 1.3 captured successfully: ten distinct loaded station class/item/family bindings, including Stone Furnace, Cooking Pot and Workbench sharing `OcBldg_CraftingBench`. The v1.9.1 repair resolves these by family, assigns weapon Refinery to equipment care, and denies unknown generic-bench families. All 11 C# suites passed (28 production assertions); build has zero warnings/errors. The repaired DLL was installed after a full test-state backup while the game was closed. The three runtime checks above subsequently passed. See [station evidence](STATION_EVIDENCE_20261010.json).

Additional v1.9.0 production results: locked campfire cooking preserved ingredients; ordinary crafting produced a stone pickaxe; the actual gliding mission delivered Smelting. Following normal advancement to Agricultural Age, a furnace batch consumed the expected materials and produced the expected output. After saving and reloading, validation passed, Smelting permission restored, and the tester confirmed furnace operation still worked. Other station paths and multiplayer remain unverified.

Tests used a disposable new character and world, a separate native AP 0.6.8 seed, and disabled server release/collection automation. No historical reward recovery was performed.

| Test | Observed result |
| --- | --- |
| Startup | All nine required Harmony registrations verified |
| Enrollment | Verified character identity, ready local player, level 1, zero assigned/unspent/mission points; one-time approval consumed and enrollment persisted |
| Mission 50: punch trees five times | One AP Skill Point; logged balance 0 → 1; committed and acknowledged |
| Full game restart | One unspent point restored; AP-aware validation passed; no second application |
| Bridge restart | Same session and delivered history restored; UI remained at one point |
| Learn Meditation | Normal point spend; mission 99 also completed and delivered a distinct second Skill Point |
| Reload after spending | One assigned + one unspent = two AP points; validation passed |
| Tamer before ownership | Novice Trainer denied with a missing-unlock explanation |
| Mission 53: obtain 30 logs | Tamer item delivered, ownership persisted, mod acknowledgment recorded |
| Learn Novice Trainer | Allowed after unlock; one point spent; zero unspent |
| Restart after Tamer learning | Novice Trainer level 1 preserved; two assigned/zero unspent; validation passed; Tamer permission restored |

These observations verify the tested paths. They do not prove forced lost-ack replay handling, every crash window, multiplayer correctness, all skill acquisition paths, or seed beatability.

## Automated/build results recorded for this baseline

| C# suite | Assertions passed |
| --- | ---: |
| GrantPipeline | 106 |
| RepairRegression | 50 |
| ValidationDecision | 23 |
| TransactionRegression | 16 |
| UnlockMirror | 23 |
| SkillCatalog | 24 |
| EnrollmentRegression | 58 existing + 40 coordinator |
| Total | 340 |

Build: zero warnings/errors. Tested DLL SHA-256: `3ab04b02905881a37ded35adc0945cf9d6800e581e062093da734c883804cfdd` (binary not distributed here).

Publication cleanup replaces personal identifiers with synthetic test values and removes developer-specific paths from comments, documents, and test fixtures. Historical example character IDs are anonymized. The wire-format test now reads `CRAFTOPIA_AP_SOURCE`; the skill catalog test has a repository-relative fallback. This does not add gameplay behavior. Selected publication checks passed: 33 Python delivery/integration/bridge tests, plus the 23-assertion unlock and 24-assertion skill catalog suites; the changed GUID fixtures were checked again separately.

## Enrollment safety model

An exact character GUID, session epoch, and configured slot must be explicitly approved. The coordinator rejects known history, recovery holds, unresolved intents, changed identity/load/player, unknown totals, and nonzero starting points. It checks state again before writing.

A separately created, flushed approval-use receipt precedes the authoritative enrollment write. A crash or write failure between these steps leaves a consumed approval requiring review. No automatic recovery clears it. Retaining configuration alone cannot recreate a lost enrollment file while this receipt survives. Losing all evidence is not something this design can detect reliably; explicit operator freshness attestation remains required.

Validation defers while an exact approved enrollment awaits load completion. This does not authorize grants or acknowledgments. Normal validation and eligibility checks must subsequently pass.

## Remaining work

October 10 follow-up: [equipment/prerequisite foundation and collector 1.3](EQUIPMENT_PROGRESSION_20261010.md) add tested progressive receipt handling and real AP access-rule integration. Equipment items are registered but excluded from playable pools. Verified logic rejects generation without a reviewed profile; none is bundled yet. Station-family mapping is captured and the three v1.9.1 paths above passed; the furnace's positive runtime test still does not verify denial while locked.

- Production permissions need end-to-end runtime tests; equipment tiers need verified per-item progression data and enforcement.
- Non-mission checks, replacement rewards, and useful material/money delivery are incomplete.
- Multiplayer code/design is present but not validated as a supported end-to-end feature.
- The 125-mission and 241-skill catalogs are observations, not complete prerequisite or accessibility models.
- Boss/age victory options remain rejected by supported generation until locations and logic exist.
- Mission-awarded skill learning may use a different game method from the tested skill-menu path.
- Historical recovery, forced duplicate injection, and live crash-recovery tests were not performed.
- Per-frame blocked-grant logging still needs cleanup. Numeric GUID and Windows authority-lock fixtures were repaired; all client/world suites passed for this development batch.

The companion Tamer gameplay mod is a separate project and is not included or enabled by this integration.
