## 2026-10-19: public documentation refresh

Geezah and Millewind owner playtests reached victory; in-game setup and remembered enrollment were confirmed. README, installation, roadmap, testing and developer instructions now separate that baseline from v1.15.0 map/pause features awaiting live acceptance. No public binary release is advertised.

## 2026-10-18: soul orb map and Field Guide pause v1.15.0

Seven C# programs and 46 focused Python tests passed; Release zero warnings/errors. Independent review completed. Native visual/gameplay acceptance remains pending; see ORB_MAP_AND_PAUSE.md. Current seed and enrollment are preserved.

## 2026-10-18: in-game setup v1.14.0

Installed Field Guide connection settings, portable managed bridge, and explicit fresh-character/world enrollment. 183 client tests and 17 C# programs passed; Release build has zero warnings/errors. 630 installed files matched the verified package. Source review fixes cover profile identity, transient reconnect, repeated settings writes, load-before-session identity publication, and currency-only enrollment. Live menu/enrollment/reload acceptance remains pending. See IN_GAME_SETUP.md.

## October 17 Millewind preparation - v1.13.0

The 161-check cumulative Geezah/Millewind seed is prepared, with matching world, bridge and mod. Victory requires Millewind tower completion AND Old Garden of Undead. See [prepared playtest](MILLEWIND_PLAYTEST.md) for scope, validation and live-test limits. Previous sessions remain unchanged.

Historical status entries follow.

## October 16 Geezah expansion � v1.12.3 / regional r5

Prepared the117-check Geezah profile, up from52:14 additional missions and51 native-region soul orbs. Added Magic and two Shield progression copies. Victory remains tower repair3 AND Ruin of Beginning. Essential progression is excluded from optional orbs because individual routes remain unverified. See [expansion scope and evidence](GEEZAH_EXPANSION.md).

World validation:64 tests including100 real fills; focused bridge validation:68 tests. The expanded seed was generated and archive-checked. This update publishes source and evidence; it does not install the prepared package or switch the active playthrough. The48 presumed legacy items remain parked.

Audited progression r2 and equipment contract r2 from the preceding work are included. Historical status entries below describe earlier snapshots.

Publication verification: all15 C# regression programs passed; Release build has zero warnings/errors. Fixed GrantPipelineTests source/reference inclusion so it compiles the current Field Guide and milestone code rather than an outdated subset. The legacy all-client discovery command stalls; focused suites and isolated module results are used, not a claim that discovery passed.

## October 16 audit integration — v1.12.2 (built, not installed)

Implemented [audited progression data and station mappings](AUDIT_INTEGRATION_20261016.md): 253 item definitions, 30 tower stages, nine age transitions and 77 resistance records. Geezah r3 uses audited tower/age ingredients with existing experimental source assumptions. Added exact generic-bench families for Potion Brewing Stand (Alchemy), Machine Factory and Furniture Workbench (ordinary manual crafting). Unknown stations still deny; later regions remain unavailable.

Verification: full world suite 55 tests passed with workspace temporary storage, including 100 generated regional seeds; after adding the final cycle regression, all six audit tests passed. ProductionTests 40 assertions passed; ConnectorTests lifecycle/cost-isolation/unsupported-IL checks passed; Release build zero warnings/errors. Independent scoped review found no blockers. Initial broad-suite failure was a sandbox temporary-folder access failure, resolved by rerunning with workspace TEMP/TMP.

Prepared `artifacts/audit-integration-20261016` contains the DLL and APWorld. No installation, active seed replacement, save changes or GitHub publication occurred. Live station-class confirmation and later-region acquisition/geometry remain outstanding.

## v1.12.1 prepared — regional runs and first explicit loot tier

New regional profile r2 has 52 Geezah checks and requires tower completion AND
Ruin of Beginning. Smelting/Cooking are restricted to reachable opening checks.
Goblin's Club 6031 has an explicit Sword tier 2 rule only in the new equipment
contract; old seeds and stronger club variants retain their prior rules.
See [regional runs](REGIONAL_RUNS.md) and [loot review](LOOT_EQUIPMENT_REVIEW.md).

Validation: 50 world tests including 100 regional fills, 23 client equipment
tests, 13 equipment exporter/audit tests, and 6,849 C# equipment assertions pass.
Release build has zero warnings/errors. Independent review's malformed-catalog
finding was corrected and covered by regressions. New example seed/APWorld/DLL
are prepared locally; no live game, bridge, save, or seed was replaced.
Runtime equip/reload testing and remaining loot acquisition evidence are pending.

## v1.11.0 Geezah pilot — 2026-10-16

New opt-in 50-check Geezah profile and native milestone observers are prepared for
controlled live testing. See [pilot scope, evidence limits and test sequence](GEEZAH_PILOT.md).
Collector v1.5.2 is installed and live F9 capture verified. Existing CraftopiaOrbs
seed/save/receipt history is preserved; the pilot requires a separate fresh world.
## v1.10.1 experimental live-test repair � 2026-10-15

Installed locally for the existing Soul Orb test session. Two orb checks confirmed;
first vanilla currency suppressed; second AP currency saved but acknowledgment stalled.
Corrected mismatched path normalization and added observer diagnostics. One witnessed
delivery reconciled offline with backups and exact identity/save-hash checks; no game
save edits. Automatic save confirmation still requires another live test.
HUD: compact corner history toggle, friendly single orb-check popup, explicit currency
pending/delivered history and no received popup on duplicate acknowledgments.
Validation: currency 89 assertions, notification tests, 133 bridge tests, six actual-SQLite
probe assertions; build clean. See SOUL_ORB_IMPLEMENTATION.md for evidence limits.

# v1.10.0 experimental source — October 15, 2026

Soul Orb integration is implemented and packaged, not installed or live verified. The installed v1.9.8 DLL remains unchanged. The opt-in expedition pool now contains 125 missions plus 439 observed orb candidates, with 439 matching currency items. Respawning chests remain excluded; Tamer remains vanilla. See [implementation details and live-test checklist](SOUL_ORB_IMPLEMENTATION.md).

Verification: 132 client unittest cases, 36 real-AP/world tests, 86 currency/replacement/binding assertions, and six read-only SQLite fixture assertions pass. All 14 C# executable suites passed; the affected currency and pipeline suites were rerun after final repairs. Release build has zero warnings/errors. The four asynchronous legacy/client scripts that timed out in the sandbox passed with local socket access outside it. Focused review found and resolved session-enrollment races and startup collection bypasses.

Physical orb reachability and the runtime reward/save hooks still require a controlled live test. No existing seed, save, ledger, or installed plugin was changed for this batch.

---
# Current status — v1.9.3 development / v1.8.9 gameplay baseline

## v1.9.4 installed � runtime verification pending

Adds queued/server-confirmed check notices, attempted-action unlock explanations, F8 panel input repair, and quieter continuous permission checks. Includes a separate opt-in 11-mission opening profile with exact routing and Sword dependency; legacy/candidate seeds remain unchanged. See [feedback and live checks](NOTIFICATIONS_v194.md) and [opening profile evidence/limits](EARLY_PROFILE.md).

Verification: all 13 C# executable suites passed; release build has zero warnings/errors. All 12 client scripts and seven world scripts passed individually, plus eight exporter assertions. The opening profile includes actual AP 0.6.8 restrictive fill/beatability checks for 20 seeds. Initial restricted-sandbox asyncio runs timed out; isolated reruns outside that restriction passed. Two outdated test fixtures were corrected (routing metadata and the stub world's actual location API). These are automated results, not new in-game test claims.

Installed while the game was closed, after backing up saves/configuration, the previous DLL and bridge state. Installed DLL SHA-256: `294fe155aa7b5006cb5163178c7952fc9ded4e56cb7e0999a1549c335743841e`. The bridge reconnected to the existing seed/history. The current character/world/seed and optional Tamer installation have not been changed for this update. New overlay readability, action messages and full new-profile completion still need live verification.


Updated October 14, 2026. This document supersedes implementation-status claims in older reports in this repository. The table below remains the v1.8.9 baseline. Controlled v1.9.0 testing additionally verified registration of all 22 production hooks, a successful existing-character load, and a locked campfire attempt denied without consuming ingredients. Other production paths and durable observation acknowledgments still need runtime verification. Collector v1.2.0 captured successfully. See [batch results](PROGRESSION_BATCH_20261010.md).

Equipment follow-up: [v1.9.2 game-facing integration](EQUIPMENT_HOOKS_v192.md) adds seven pre-cost/pre-transfer hooks, full-loadout checking and an explicit character/session/catalog test contract. Equipment remains disabled by default and excluded from generated pools. All 12 C# suites, 82 bridge tests, 22 world tests and eight exporter tests pass; build has zero warnings/errors. Independent review and the wrong-character admission fix are complete. Selected active equipment paths passed a controlled fresh-character test on October 14; the disabled smoke test passed on October 12. Equipment remains experimental and excluded from normal generated pools.

## Controlled runtime results

October 14: an isolated diagnostic seed and explicitly approved fresh character verified Sword-category equip denial before ownership, mission 50 delivering Sword tier 1, allowed Stone Sword crafting/equipping, and ordinary Stone Pickaxe crafting. A full game restart restored Sword ownership and matched the five saved AP points without reapplying those grants. Equipping the sword then completed a distinct mission and legitimately awarded a sixth point. Wooden Shield crafting remained blocked with sufficient materials; the log names the missing Shield tier 1 permission. Locked Sword crafting, denied-equip stack preservation, material preservation, denied loadouts, and higher tiers remain unverified in this round. See [active test details](EQUIPMENT_HOOKS_v192.md#october-14-active-equipment-results).

October 12: v1.9.2 disabled-equipment smoke test passed. All 38 registrations verified; original-session character validation and unlock replay passed. Tester confirmed normal crafting/equip/unequip, owned furnace operation, and locked Cooking Pot denial with no ingredient consumption. No Error/FATAL/recovery hold appeared in the inspected log. Selected active equipment results are recorded below. A missing bridge ledger was reconciled before loading; see [runtime evidence and recovery limits](EQUIPMENT_HOOKS_v192.md#october-12-runtime-results).

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

The companion Tamer gameplay mod is a separate optional plugin, not bundled or required by this integration. It was restored locally on October 14 at the tester's request; startup and basic pet follow/recall/combat checks passed; advanced compatibility remains unverified. See [optional companion setup and limits](OPTIONAL_TAMER.md).

October 14 follow-up: bridge-only reconnect preserved ownership and previous grant history; tester confirmed Sword equip, continued Shield craft denial, and successful saved Sword loadout restoration. Denied loadouts remain untested. Further tier tests retain the same character/world and earned progression.

October 14 Blacksmith repair (v1.9.3, deployed after backup; Copper Sword craft/equip confirmed): Copper Sword correctly became available after the second progressive Sword receipt, but clicking Craft was denied by the separate production hook: station identity unavailable. A fresh collector capture verified Blacksmith item 272 as `Oc.OcBldg_CraftingBench`, family 20017. The exact family now uses the recipe-bench path; unknown benches still deny, and independent equipment/native age/cost checks remain unchanged. The new regression failed before the repair; all 30 production assertions and the equipment suite pass afterward. Release build: zero warnings/errors. Tester subsequently confirmed Copper Sword crafting and equipping succeeded. See [station evidence](STATION_EVIDENCE_20261014.json).

The ongoing diagnostic used explicit server test grants for Smelting and a second Progressive Sword receipt. These are not naturally earned progression evidence: Smelting was placed behind a level-35 enemy mission, exposing an unresolved placement/prerequisite problem. Before the Sword tier-2 grant, Copper Sword crafting was denied with all ingredients available and a logged tier-2 requirement. No character/world reset was used.
