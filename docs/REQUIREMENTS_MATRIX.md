> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Requirements Matrix v1.3.0

**Date:** 2026-10-07  
**Baseline:** v1.2.1 (Codex repair handoff, verified)

## Implemented and Tested

### Safety Baseline (v1.0.8 → v1.2.1, preserved)
- [x] Strict grant-state parsing, validation gates, invocation-bound validator state
- [x] Load completion bound to observed instance/event
- [x] Delivery requires pipeline epoch == validated epoch
- [x] Missing/malformed history fails closed
- [x] No auto-enrollment, migration, hold clearing, or historical recovery
- [x] Separate journaled writes (not atomic multi-file transactions)
- [x] Tests: 190 C# assertions (50+101+23+16)

### Goal System (v1.2.0 → v1.2.1)
- [x] Goal-event contract with stable IDs (boss/age/objective)
- [x] Victory possession does NOT trigger completion
- [x] Event validation (boss ID vs goal_boss_choice, age positive int, objective non-empty)
- [x] Event persistence to ledger; restore on startup
- [x] Goal evaluation after every check
- [x] Generator rejects unsupported boss/age goals
- [x] Tests: 8 goal-event tests (production-path), 21 delivery/goal tests

### Unlock Delivery (v1.2.1)
- [x] Durable authoritative unlock history survives acks
- [x] Slot/epoch/connection-bound messages
- [x] Main-thread C# mirror with queue
- [x] Pending retries persisted before sending
- [x] Tests: 23 UnlockMirror assertions

### Skill Trees (v1.3.0)
- [x] 8 AP items: Magic, Life, Trick, Tamer, Combat T2-T5 (Basic + Combat T1 are starters)
- [x] `TryLearnSkill` Harmony prefix enforcement hook
- [x] Category×tier permission model (evidence-backed from skill catalog)
- [x] Fail-open when no session; fail-closed when session bound
- [x] Blocked-action messages via PlayerStatusDisplay
- [x] Tests: APWorld item table includes skill trees; generation places them

### Production Groups (v1.3.1, NEW)
- [x] 10 AP items: smelting, cooking, farming, livestock_fishing, alchemy,
  recycling, water_processing, equipment_care, automation, creature_combining
  (G10 power excluded pending runtime confirmation)
- [x] `StartProcessReq` Harmony prefix enforcement hook (blocks before resource consumption)
- [x] `ResolveProductionGroup` maps 29 building types to groups
- [x] Opening set (crafting bench) free; all others gated
- [x] Fail-open when no session or unknown station; fail-closed when bound
- [x] Tests: 4 progression item tests (skill trees, production, filler, ID uniqueness)

### Observation Durability (v1.3.0, NEW)
- [x] Game-side durable outbox (file-backed, atomic writes)
- [x] Replay unsent observations on startup
- [x] Ack removes from outbox after bridge confirms

### Status UI (v1.3.0, NEW)
- [x] F8 toggle overlay (reflection-based, no Unity module refs)
- [x] Shows connection, slot, permissions, recovery holds, blocked actions

## Implemented but Awaiting Runtime Verification

### Skill Enforcement
- [ ] `TryLearnSkill` prefix blocks correctly in-game (hook verified statically, not runtime)
- [ ] Skill ID → (category, tier) mapping (needs runtime dump of `skillList.all`)
- [ ] `IsSessionBound()` correctly detects AP session state

### Observation Outbox
- [ ] File writes work in game's BepInEx folder (permissions)
- [ ] Replay on game restart works correctly
- [ ] No duplicate observations sent

### Status UI
- [ ] Overlay renders correctly in-game (IMGUI via reflection)
- [ ] F8 toggle works (Input via reflection)
- [ ] Text is readable (font size, colors)

## Not Implemented or Blocked

### Equipment Progression
**Status:** Blocked — needs runtime evidence  
**Missing:** Item dump with per-item `rank`/`rarity`/`workbenchLv` values grouped by `wpCategory`.  
**Evidence:** `/tmp/equipment_tiers.md` — catalog has zero per-item rows; cannot build tier mapping from metadata alone.  
**Collector:** Diagnostic package F9 dump, "equipment" section.

### Production Unlock Groups
**Status:** Partial — 29 station types identified, groups defined in `/tmp/production_groups.md`, but NOT wired to AP items or enforcement hooks.  
**Missing:** `workbenchId` → station-type mapping; actual recipe lists; `StartProcessReq` hook verification.  
**Collector:** Diagnostic package F9 dump, "production" section.

### Equipment/Production Enforcement Hooks
**Status:** Blocked — hook points not yet identified.  
**Missing:** Craft method signature (`TryCraftItem` candidate not confirmed); equip method on `OcPlEquip`; `StartProcessReq` parameters.  
**Collector:** Diagnostic package F9 dump, "hooks" section.

### Check Adapters (non-mission)
**Status:** Not implemented — infrastructure exists, adapters not built.  
**Missing:** Soul orb collection method; chest stable IDs; shop purchase method; Anubis reward method; age milestone detection.  
**Collector:** Diagnostic package F9 dump, "check sources" section.

### Multiplayer (v1.4.0, NEW)
- [x] **Separate slots**: per-seed shared registry with heartbeats; world events
  broadcast to all bridges; each validates against own slot_data; dedup by event_id
- [x] **Shared slot**: multiple mod connections (primary + extras); late-join gets
  full unlock history replay; per-character skill-point enrollment and accounting;
  team-once money/material bundles (never re-sent)
- [x] **Authority**: one bridge per session (lock file, --takeover, stale lock handling)
- [x] **Enrollment**: explicit durable enrollment; refuses under recovery hold;
  --recover-enrollment with typed YES confirmation
- [x] Tests: 17 multiplayer tests, all passing; no regressions in existing suites

### Reward Replacement
**Status:** Not implemented.  
**Missing:** Mission `TryGetReward` currently only notifies; does not replace the vanilla reward. Needs runtime verification of reward structure.

### In-Game Starter Choice
**Status:** Not implemented — uses `--starter-choice` CLI flag.  
**Missing:** In-game UI for player_choice starter selection.

## Test Coverage

| Suite | Assertions | Status |
|-------|------------|--------|
| RepairRegressionTests | 50 | ✅ Pass |
| GrantPipelineTests | 101 | ✅ Pass |
| ValidationDecisionTests | 23 | ✅ Pass |
| TransactionRegressionTests | 16 | ✅ Pass |
| UnlockMirrorTests | 23 | ✅ Pass |
| Bridge tests | 22 | ✅ Pass |
| Goal event tests | 8 | ✅ Pass |
| Delivery repair tests | 21 | ✅ Pass |
| Integration (AP 0.6.8) | 8 | ✅ Pass |
| **Total** | **293** | ✅ |

## Migration Notes (v1.2.1 → v1.3.0)

### APWorld
- Item table expanded: +8 skill trees, +5 filler bundles.
- Item IDs shifted: Victory is now ID 15 (was 3). **Existing seeds are incompatible.**
- `create_items` now places skill trees + filler, not just Skill Points.

### Bridge
- No breaking changes to ledger format.
- New: multiplayer modes (in progress).

### Mod
- New: `TryLearnSkill` prefix (fail-open when no session).
- New: durable observation outbox (`observations.log` in BepInEx folder).
- New: F8 status overlay.
- New: `IsSessionBound()`, `ResolveSkillInfo()`, `SkillInfo` class.
- `Permissions.IsSkillTreeUnlocked(string, int)` overload added.

### What to test in-game
1. Skill learning blocked without unlock (try learning a Magic skill with only Basic+Combat T1).
2. F8 overlay shows correctly.
3. Observations persist across game restart (check `observations.log`).
4. No crashes on skill learn attempts.

