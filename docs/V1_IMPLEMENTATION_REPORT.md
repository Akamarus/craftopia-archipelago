> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago v1.0 — Implementation / Verification / Limitations

## Implementation

### Native APWorld (`worlds/craftopia/`)
- `__init__.py`: `CraftopiaWorld` (0.6.8 API). Locations from missions,
  items (Skill Point, Victory), regions (data-driven), options, slot_data.
- `Data.py`: Item/location tables; `missions.json` loader. Skips unverified
  (null game_id) missions with a warning.
- `Items.py`: `CraftopiaItem`, `CraftopiaLocation` (with `game_id` field).
- `Locations.py`: (via Data.py) AP IDs sequential; game IDs stored per-location.
- `Regions.py`: Data-driven regions; Menu entrance.
- `Options.py`: Goal (final mission / all missions), SkillPointsPerCheck (1-5),
  EnableMissionChecks.
- `data/missions.json`: 53 missions populated from IL dump
  (docs/GAME_DATA_DUMP.md, SHA 17087f5a...). Keyed by verified class name
  (e.g. `Oc.Missions.ItemCraftMission`); numeric game_ids are data-driven
  (null until captured at runtime). Note: hook inventory cited 142;
  IL shows 53 concrete subclasses (discrepancy unresolved).
- `docs/setup_en.md`: Player setup guide.
- `test/test_generation.py`: 6 tests (JSON valid, fields, uniqueness, skip logic).
- `Craftopia.yaml`: Example player config.

**Status**: Framework complete, tests pass. Blocked on mission game_ids
from the assembly dump. The world generates (with zero locations until
missions.json is populated).

### Mod v1.0 (`mod/CraftopiaArchipelago/`, DLL `dist/CraftopiaArchipelago-v1.0.0.dll`)
- `Patches.cs`: Three hooks —
  - `Mission.TryGetReward` postfix (check detection; verified in PoC).
  - `OcSkillManager.GameStartCheckSkillPoint` prefix (validation gating;
    returns false to skip on Pass/Defer, true to run on RunVanilla).
  - `OcPlMaster.LoadSaveData` prefix/postfix/finalizer (load observation).
- `Validation.cs`: Bridges game state to `ValidationDecision.Decide()`.
  Read-only; never calls `GetCharacterGUID()`.
- `EntitlementStore.cs`: JSON persistence per (guid, epoch). Statuses:
  Ok, NoRecordConfirmed, UncertainHistory, StoreUnreadable, EpochMismatch,
  PendingIntent. Never auto-creates; missing -> UncertainHistory.
- `ValidationDecision.cs`: Pure decision function (22 tests pass).
- `GrantPipeline.cs`, `PlayerReadiness.cs`, `Bridge.cs`, `MiniJson.cs`:
  Preserved from PoC (dedup, retry, readiness gating, local protocol).
- `Plugin.cs`: v1.0.0, GUID `com.craftopia.archipelago`. Verifies all 5
  patch registrations (mission postfix, validator prefix, load
  prefix/postfix/finalizer) via Harmony metadata. `OnSlotData` sets epoch
  and enrollment from bridge. `CommitEntitlement` for pipeline.

### Bridge (`client/craftopia_bridge.py`)
- SHA-256: `4bfd12e80042f9b207c926cbb57efedf4396154b823edb7b3463f4a5101c4e9f`
- Tests: `client/test_bridge.py` (SHA `cdfbd8649d28f8ec1204dbd959e370bb56eac971adda2bc18adb05f82f038a1f`),
  22 assertions, all pass.
- Native `game: "Craftopia"` slot (not APQuest proxy). Reads `slot_data`
  (`game_id_to_location`, `skill_points_per_check`, `goal`).
- Local protocol matches `Bridge.cs` (`"cmd"` keys, not `"type"`).
- Skill points expand to virtual indices (stride 5) for `skill_points_per_check`.
- Victory -> `StatusUpdate(CLIENT_GOAL=30)`, not granted.
- Preserved: ledger, epoch, dedup, reconnect, no auto-reset.
- `--game-id-dump` mode for debugging.
- PoC `craftopia_client.py` untouched.

## Verification

### Headless tests (actual output)
- **ValidationDecision**: 22 tests, `PASS: 22, FAIL: 0`, exit 0.
  Covers: AP match -> Pass; both discrepancy directions -> Defer;
  all 5 uncertain statuses -> Defer; NoRecordConfirmed -> RunVanilla;
  identity unknown -> Defer; guid/epoch binding mismatch -> Defer;
  negative GUID -> Pass; level/mission scaling.
- **GrantPipeline**: All tests pass (existing suite, updated for new files).
- **World data**: 6 tests pass (`python3 -m unittest test.test_generation`).
- **Mod build**: `Build succeeded. 0 Warning(s) 0 Error(s)`.

### Binary verification
- Mod v1.0.0 DLL SHA-256: `9ab370ddd2196e3c11f7bb5ab5d1907929a770793feef78fd17c9bdb162afe03`
- No prohibited references (GetCharacterGUID, set_GUID, SetSkillPoint in
  diagnostic context; validator prefix only reads).

### NOT verified (requires in-game testing)
- Game hooks (mission postfix, validator prefix, load patches) — headless
  HarmonyX cannot run on Linux .NET 8. Codex must review before in-game test.
- Reward persistence across save/load — the validator gating is implemented
  but not runtime-tested.
- Multiworld play — bridge pending; no e2e run yet.
- Mission game_ids — dump in progress.

## Limitations

### Supported (v1.0 scope)
- **Checks**: Mission reward claims via `Mission.TryGetReward()` postfix.
  (142 mission types; IDs from dump.)
- **Rewards**: Skill Points via `AddSkillPoint()` (verified grant mechanism).
- **Persistence**: Validator gating via `ValidationDecision` + entitlement store.
  Pass/Defer/RunVanilla per reviewed design.

### Excluded (no evidence or out of scope)
- Dungeon boss kills (`OnDungeonBossKill` — hook candidate, not implemented).
- Tower restores (`OnAchieve` — hook candidate, not implemented).
- Field boss kills (`OcEmType` mapping needed).
- Inventory item grants (non-UI AddItem TBD).
- XP/gold grants (`AddExp` confirmed but not wired).
- Boss Rush, achievements (beyond missions).

### Known gaps
- `missions.json` has 53 missions (class-keyed); numeric game_ids null
  until runtime capture. World generates 53 locations.
- Bridge `craftopia_bridge.py` pending (delegated).
- No automatic migration/recovery of PoC indices 0/1/2/3 (per constraints).
- Diagnostic DLL remains the active plugin; v1.0 not deployed (per constraints).

## Hashes
- `dist/CraftopiaArchipelago-v1.0.0.dll`: `9ab370ddd2196e3c11f7bb5ab5d1907929a770793feef78fd17c9bdb162afe03`
- `mod/ValidationDecision/ValidationDecision.cs`: `d46dc4379d54a25768144ab23611b988842acf0b1002cb4c2b3fb9f2594812d6`
- `mod/ValidationDecisionTests/Program.cs`: `aef73b44536230b678a2e6e8531f0107f1026cc4253326ece1bbae565ce38607`

## Evidence requests
See `docs/EVIDENCE_REQUESTS.md` for the precise list of missing game data.

