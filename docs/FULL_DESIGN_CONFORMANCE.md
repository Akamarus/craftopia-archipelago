> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Full Design Conformance Matrix

**Date:** 2026-10-06
**Baseline:** v1.0.8 (Codex repairs integrated)

## Design Section Conformance

### 1. Intended Experience
| Requirement | Status | Notes |
|-------------|--------|-------|
| Progression randomizer | **Partial** | Skill points + missions; equipment/production blocked |
| Several-session default | **Proposed** | No playtesting yet; balancing TBD |
| Safe opening | **Designed** | Starting combat options; implementation pending catalogs |

### 2. Agreed Decisions
| Decision | Status | Notes |
|----------|--------|-------|
| Hybrid reward structure | **Designed** | Permission model scaffolded; catalogs pending |
| Separate combat categories | **Designed** | Options exist; categories pending catalog |
| Progressive tiers | **Designed** | `UnlockEquipmentTier()` API exists |
| Craft equipment (not granted) | **Designed** | Permission governs use, not acquisition |
| Category/tier governs crafting AND use | **Designed** | API exists; hooks pending |
| Production as AP unlocks | **Designed** | `UnlockProduction()` API exists |
| Age via normal requirements | **Supported** | No AP age token |
| Skill trees as permanent unlocks | **Designed** | `UnlockSkillTree()` API exists |
| Player-selected starting category | **Implemented** | `StartingCombat` option |
| All categories enabled by default | **Implemented** | Toggle options; missions supported |
| Replace (not add) rewards | **Designed** | Principle documented; implementation pending |
| Useful filler, no placeholders | **Designed** | Bundles pending game-data inspection |
| Configurable victory (boss/age/count) | **Implemented** | Options + generation logic |
| Cooperative world | **Designed** | Protocol documented |
| Separate slots OR shared slot | **Designed** | `MultiplayerMode` option; protocol documented |
| Shared credit | **Designed** | Protocol documented |
| Trading allowed | **Designed** | Principle documented |

### 3. Progression and Grouping
| Requirement | Status | Notes |
|-------------|--------|-------|
| Verified equipment catalog | **Blocked** | Subagents investigating; runtime needed |
| Category/tier permission | **Scaffolded** | `PermissionManager` implemented |
| Block alternate equip paths | **Blocked** | Hooks need runtime verification |
| Production packages | **Blocked** | Station catalog pending |
| Permission at action, not menu | **Designed** | Principle documented |
| Skill tree unlocks | **Scaffolded** | API exists; tree catalog pending |
| Locked-tree effects not bypassed | **Blocked** | Effect gating needs runtime |

### 4. Checks and Source Replacement
| Requirement | Status | Notes |
|-------------|--------|-------|
| Stable per-objective identities | **Partial** | Missions use class keys; instances need runtime |
| 53 classes as candidates | **Documented** | Not verified playable; marked as candidates |
| All categories enabled by default | **Implemented** | Toggles; only missions generate locations |
| Disabled sources retain normal | **Designed** | Principle documented |
| Pool balancing | **Partial** | Basic; needs catalog data |
| Coverage matrix | **Implemented** | `docs/COVERAGE_MATRIX.md` |
| No guessed IDs | **Compliant** | All IDs from verified sources |

### 5. Filler, Shops, Economy
| Requirement | Status | Notes |
|-------------|--------|-------|
| Named fixed bundles | **Blocked** | Contents need game-data inspection |
| No required material as filler | **Designed** | Principle documented |
| Finite one-time shop purchases | **Blocked** | Shop system needs implementation |
| Shared-slot consumables once | **Designed** | Protocol documented |

### 6. Victory
| Requirement | Status | Notes |
|-------------|--------|-------|
| Independent boss/age/count switches | **Implemented** | Options + validation |
| ALL enabled conditions required | **Implemented** | `set_rules()` combines with AND |
| Progress in any order | **Designed** | Bridge tracks events durably |
| Objective count = distinct objectives | **Partial** | Counts locations; bridge tracks distinct |
| Generator/bridge agreement | **Designed** | Slot data includes goal config |
| No Skill Point counting for objectives | **Compliant** | Bridge tracks, not item counts |

### 7. Multiplayer Modes
| Requirement | Status | Notes |
|-------------|--------|-------|
| Separate slots | **Designed** | Protocol in `MULTIPLAYER_DESIGN.md` |
| Shared slot | **Designed** | Protocol documented; implementation pending |
| Shared event → one check per slot | **Designed** | Protocol documented |
| Late join catches up (not consumables) | **Designed** | Protocol documented |
| Roster binding | **Blocked** | World ID needs runtime |
| Per-slot ack tracking | **Designed** | Protocol documented |

### 8. Persistence, Enforcement, Feedback
| Requirement | Status | Notes |
|-------------|--------|-------|
| Preserve v1.0.8 safety fixes | **Compliant** | All retained; 190 assertions pass |
| Generalize beyond skill points | **Blocked** | Each type needs transaction design |
| Durable enrollment | **Designed** | `ENROLLMENT_DESIGN.md`; not implemented |
| Show unlocks/progress | **Scaffolded** | `PlayerStatusDisplay` data model |
| Blocked-action explanations | **Scaffolded** | `GetBlockExplanation()` API |
| Goal tracker | **Scaffolded** | Status display data model |
| No deployment/live saves | **Compliant** | No game interaction |

## Test Summary

| Suite | Assertions | Status |
|-------|------------|--------|
| RepairRegressionTests | 50 | Pass |
| GrantPipelineTests | 101 | Pass |
| ValidationDecisionTests | 23 | Pass |
| TransactionRegressionTests | 16 | Pass |
| **Total** | **190** | **Pass** |
| World generation (basic) | 6 | Pass |
| Bridge | 22 | Pass |
| Integration | 8 | Pass |

## Remaining Blockers

1. **Equipment catalog**: weapon/armor types, tiers, equip/use hooks (subagents running)
2. **Skill tree catalog**: tree IDs, effect gating (subagents running)
3. **Production catalog**: station IDs, use hooks (subagents running)
4. **Boss/age lists**: verified game content needed
5. **Shop system**: purchase hooks, inventory API
6. **World ID**: stable identifier for roster binding
7. **Durable enrollment**: implementation + review
8. **In-game UI**: unlock display, goal tracker
9. **Runtime verification**: all Harmony hooks, multiplayer authority

## Files Delivered

- `worlds/craftopia/`: Full APWorld (options, generation, multiplayer slot data)
- `mod/CraftopiaArchipelago/`: v1.0.8 + `Permissions.cs` + `PlayerStatusDisplay.cs`
- `client/`: Bridge (unchanged from v1.0.8)
- `docs/`: `COVERAGE_MATRIX.md`, `MULTIPLAYER_DESIGN.md`, `ENROLLMENT_DESIGN.md`, this file

