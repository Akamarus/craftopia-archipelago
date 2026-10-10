# Current status — v1.9.0 development / v1.8.9 gameplay baseline

Updated October 10, 2026. This document supersedes implementation-status claims in older reports in this repository. The table below remains the v1.8.9 baseline. Controlled v1.9.0 testing additionally verified registration of all 22 production hooks, a successful existing-character load, and a locked campfire attempt denied without consuming ingredients. Other production paths and durable observation acknowledgments still need runtime verification. Collector v1.2.0 captured successfully. See [batch results](PROGRESSION_BATCH_20261010.md).

## Controlled runtime results

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

- Production permissions need end-to-end runtime tests; equipment tiers need verified per-item progression data and enforcement.
- Non-mission checks, replacement rewards, and useful material/money delivery are incomplete.
- Multiplayer code/design is present but not validated as a supported end-to-end feature.
- The 125-mission and 241-skill catalogs are observations, not complete prerequisite or accessibility models.
- Boss/age victory options remain rejected by supported generation until locations and logic exist.
- Mission-awarded skill learning may use a different game method from the tested skill-menu path.
- Historical recovery, forced duplicate injection, and live crash-recovery tests were not performed.
- Per-frame blocked-grant logging still needs cleanup. Numeric GUID and Windows authority-lock fixtures were repaired; all client/world suites passed for this development batch.

The companion Tamer gameplay mod is a separate project and is not included or enabled by this integration.
