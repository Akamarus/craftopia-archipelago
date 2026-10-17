# Geezah Milestones Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an isolated Geezah dungeon-victory pilot with tower, age, dungeon and mission-category check support, plus corrected Anubis diagnostics.

**Architecture:** Share a frozen milestone catalog across APWorld, bridge and mod. Observe successful native transitions, persist through the existing outbox/ledger, and derive victory from AP-confirmed selected locations. Keep geographic eligibility separate from runtime detection.

**Tech Stack:** C#/Harmony/BepInEx, Python, Archipelago 0.6.8, existing console test suites and unittest.

**Spec:** `docs/superpowers/specs/2026-10-16-geezah-milestones.md`; regional prerequisites in `../specs/2026-10-15-regional-progression-design.md`.

## Global Constraints

- Preserve existing IDs, current seed, character saves, durable receipts and unrelated working changes.
- Single-player pilot; Tamer logic uses vanilla capabilities only; respawning chests excluded.
- Native milestone rewards remain intact. New families require explicit slot-data opt-in.
- No recurring whole-scene scans; F9 captures and event-local snapshots only.
- No nearest-tower inference of region eligibility, automatic retro-awards or silent baseline resets.
- Anubis checks remain disabled pending successful exchange and persistence evidence.

## Review Focus

1. Reload produces a tower 0-to-1 transition: task 3 must reject it.
2. A category contains an unmodeled late-game mission: task 5 must exclude it, not infer early access.
3. Save/world changes during a native call: task 3 must invalidate its snapshot.
4. AP confirmation arrives after reconnect: task 4 must confirm once and compute victory from selected locations only.
5. Diagnostic callbacks throw or collections truncate: task 1 must preserve native behavior and report incomplete evidence.

## Task 1: Correct diagnostics and export category evidence

**Files:** modify `mod/DiagnosticCollector/WorldEvidenceCapture.cs`, `ObservationHooks.cs`, `DiagnosticCollector.cs`, `DiagnosticCollector.csproj`, `verify-hooks.ps1`; create `MilestoneEvidence.cs`, `mod/CollectorTests/MilestoneEvidenceTests.cs`; update `mod/CollectorTests/CollectorTests.csproj` and `Program.cs`.

**Interfaces:** `MilestoneEvidence.Category(object manager, int categoryId)` and `MilestoneEvidence.Anubis(object page)` return field-only `Dictionary<string,object>` snapshots with explicit missing/truncated markers. F9 sections `mission_categories`, `mission_category_state`, `anubis_exchange_ui`; retain legacy `anubis_ui` section for compatibility.

- [ ] Add failing tests for missing field vs zero, copied mutable counter arrays, inherited-field access, bounded/truncated member arrays and throwing readers. Assert no traversal of inventory/account data.
- [ ] Run `dotnet run --project mod/CollectorTests`; confirm new tests fail before implementation.
- [ ] Add active UI discovery for `Oc.Item.UI.OcUI_StatusUpPageSoulOrb` fields `_exchangeCount`, `_lastExchange`, `_totalRewardCount`. Hook `OnExchangeButtonPressed()`; do not hook its empty `StatusUpConfirm()` as exchange evidence.
- [ ] Capture loaded `SoMissionCategory` fields `id`, `title`, `order`, `skillPoint`, `rewardInventoryID`, `rewardPersonalChest`; capture category membership from mission Category and stable mission IDs, and manager `categoryTookList`. Distinguish loaded catalog from active valid membership.
- [ ] Observe `Oc.Missions.OcMissionManager.TryTakeCategoryReward(System.Int32)` with before/after taken state and successful Boolean return. Observer exceptions never prevent native calls. Keep capped buffers and no new polling scans.
- [ ] Run CollectorTests, collector Release build and exact-signature hook verification against installed assembly. Save provenance and explain missing live category/Anubis evidence in release notes. Install only when the host game is closed, then request one combined F9 capture when convenient.

## Task 2: Frozen catalog and strict contract

**Files:** create `worlds/craftopia/Milestones.py`, `milestones_20261016_r1.json`, `tools/export_milestone_catalog.py`, `tools/test_milestone_catalog.py`, `worlds/craftopia/test/test_milestones.py`; add embedded resource to `mod/CraftopiaArchipelago/CraftopiaArchipelago.csproj`.

**Interfaces:** `validate_catalog(data: dict) -> list[dict]`; `resolve_milestone(slot_data: dict, family: str, source_id: int, stage: int) -> int | None`. Slot keys: `milestone_schema=1`, `milestone_catalog="milestones_20261016_r1"`, `milestone_locations` maps canonical observation keys to integer AP IDs, `goal_location_ids` is the exact required AND-list.

- [ ] Test bool/float/string IDs, duplicate IDs/names/keys, unknown family, invalid stages, unfrozen catalog, empty category membership, missing provenance and collisions with mission/orb IDs. Each invalid catalog raises ValueError; unknown slot mappings resolve to None.
- [ ] Run the new tests and confirm failure.
- [ ] Build evidence exporter with separate `research` and `enabled` records. Freeze ID 4100 for Ruin of Beginning/source 1 only after collision check; author remaining enabled IDs explicitly within spec ranges. Require complete membership before enabling categories; never infer tower count or display names from raw array order.
- [ ] Validate exported fixture provenance against recorded capture/assembly hashes. Compare full exported membership with mission catalog; unresolved rows remain disabled with reasons.
- [ ] Run `python -m unittest discover -s tools -p test_milestone_catalog.py` and `python -m unittest discover -s worlds/craftopia/test -p test_milestones.py`.

## Task 3: Native completion observers and durable outbox

**Files:** create `mod/CraftopiaArchipelago/MilestoneDecision.cs`, `MilestoneHooks.cs`, `MilestoneSession.cs`, `mod/MilestoneTests/MilestoneTests.csproj`, `Program.cs`; modify `Plugin.cs` and `Bridge.cs`.

**Interfaces:** `MilestoneDecision.Accept(string family, int before, int after, bool successfulAction, bool loadContext, bool sameWorld) -> bool`; `MilestoneSession.Bind(Dictionary<string,object>)`; `Bridge.SendMilestoneCheck(string family, int sourceId, int stage) -> bool`. Session owns selected keys, matching binding and bounded pending retries.

- [ ] Add failing pure tests: successful single-step tower/age accepted, same/lower/jump rejected, load/reload rejected even with numeric increase, duplicate dungeon/category rejected, failed claim rejected, world-reference change rejected, unbound/unsupported multiplayer denied for submission.
- [ ] Run `dotnet run --project mod/MilestoneTests` and confirm failure.
- [ ] Implement prefix snapshots and postfix validation around paid tower `TryProgressConstruction`, age `EvoAndCloseUI`, dungeon `AddDungeonClear(int)`, and manager `TryTakeCategoryReward(int)`. Use native result/state, not sampled deltas or display messages. Suppress nested duplicate observations; no standalone SetWorldLevel/TrySetProgress award.
- [ ] Bind to existing enrollment checks plus selected-world context. Missing/ambiguous world identity holds new-family readiness. Verify each required Harmony patch is installed before readiness; old seeds remain compatible.
- [ ] Extend existing durable outbox dispatch with canonical milestone keys and full seed/slot/epoch/character context. Test failed write followed by retry, replay, full retry buffer and logging failures. Preserve native actions; expose a visible hold on persistence failure.
- [ ] Run MilestoneTests, ObservationTests, EnrollmentRegressionTests and AP mod Release build. Do not mark live completion verified from unit tests.

## Task 4: Bridge persistence, confirmation and goal

**Files:** create `client/milestone_delivery.py`, `client/test_milestone_delivery.py`; modify `client/craftopia_bridge.py`, `delivery_state.py`, `test_goal_events.py`, `test_observation_ack.py`.

**Interfaces:** `MilestoneDeliveryMixin.milestone_location(family, source_id, stage) -> int | None`; async `report_milestone(msg, token) -> bool`; async `reconcile_milestone_checks()`. Persist `milestone_checks` rows containing canonical key, location ID and enrolled character; default absent field to empty for old ledgers.

- [ ] Write failing tests for strict mapping/types, wrong epoch/seed/slot/character/world context, duplicate replay, failed commit withholding acknowledgment, old ledger loading, and reconnect resubmission.
- [ ] Implement explicit milestone routing before mission fallback. Persist before acknowledgment. Reconcile only allowed rows; malformed historical rows hold safely rather than silently dropping history.
- [ ] Test goal `[4100]`: no victory on queued/local clear, victory once after AP confirmation of 4100; future AND goal needs both listed locations; unknown/nonselected/duplicate goal IDs rejected at session validation. Reconnect does not duplicate victory or notices.
- [ ] Derive completed/total HUD counts from actual selected locations across all families. Preserve compact activity expansion and received-item list.
- [ ] Run `python -m unittest discover -s client`; inspect every failure and preserve currency/enrollment semantics.

## Task 5: Geezah profile and generation safeguards

**Files:** create `worlds/craftopia/Regional.py`, `geezah_pilot_20261016_r1.json`, `worlds/craftopia/test/test_regional.py`, `test/geezah_player.yaml`; modify `Options.py`, `__init__.py`, `Progression.py`.

**Interfaces:** `build_regional_profile(data: dict) -> dict` supplies explicit mission/orb/milestone selections, capability requirements and goal location list. Add logic mode `geezah_pilot=4`. Later-region choices remain unavailable until reviewed catalogs exist.

- [ ] Add failing tests that unknown region/member/prerequisite rejects generation, category with any outside member is excluded, profile selects dungeon source 1 only, and no category or stage proves its own prerequisites.
- [ ] Author reviewed Geezah membership from map/capture evidence. Start with verified opening mission identities; individually review orb candidates, omit ambiguous ones. Include only categorized missions with actual local requirements. Explicitly model dungeon access and vanilla completion requirements; if unresolved, label the pilot experimental and disclose the gap instead of declaring proven beatability.
- [ ] Construct all selected locations and corresponding slot maps from one profile. Include only attainable age/tower stages; Millewind diagnostic evidence alone does not make Millewind eligible for the Geezah seed.
- [ ] Ensure required equipment/production items are in the pool; fill remaining slots with existing filler while preserving exact item/location count. Compose same-player opening Smelting placement restriction with exclusions/plando. Test empty eligible union and multiplayer destinations fail appropriately.
- [ ] Generate/fill fixed seeds 0–9 with AP 0.6.8, including orbs off, restrictive exclusions and plando conflicts. Assert no later-region locations, exact pool balance, mandatory unlock reachability, goal membership and identical slot-map locations. Run legacy/opening/expedition regression tests.

## Task 6: Release and controlled live pilot

**Files:** update `docs/STATUS.md`, `README.md`; create `docs/GEEZAH_PILOT.md` and versioned artifacts with SHA-256 manifest.

- [ ] Review the complete diff against the spec, separate pre-existing changes, and run only necessary full regression suites after targeted tests pass. Record exact commands/results, evidence gaps and source/installed version hashes.
- [ ] Prepare new seed in a separate workspace directory; preserve CraftopiaOrbs configuration and receipts. Verify a real host process check before installing DLLs; retain backups and verify installed hashes. No forced close or save edits.
- [ ] Give the user one short sequence: new pilot character/world enrollment, first category claim, reconnect/reload duplicate check, Ruin of Beginning clear, repeat/reload victory check. Native rewards and material costs must remain unchanged.
- [ ] Keep tower/age live probes on the existing diagnostic character's next uncompleted milestones where possible. Do not require cold/heat progression now.
- [ ] Anubis follow-up is optional: once five orbs are naturally available, capture one exchange and reload counter. Until then ship only corrected diagnostics, not enabled Anubis checks.
- [ ] Publish only reviewed source/artifacts under existing repository authorization, excluding local captures, saves, slot secrets and private paths. Do not bundle unrelated dirty changes.

## Self-review

Spec coverage: runtime families (tasks 1–4), regional placement/victory (5), legacy preservation and live validation (6), Anubis limitation (1/6). Five review-focus cases have explicit owner tests. Anubis purchases and later-region presets are deliberately gated by missing evidence; they do not block diagnostic implementation. Category catalog/live transition evidence and precise Geezah orb membership remain discovery tasks, with omission/rejection behavior explicitly defined.
