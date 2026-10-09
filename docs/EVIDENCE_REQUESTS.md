> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Game Evidence Requests

Precise list of Craftopia game data/APIs needed for v1.0 completion.
Collected during implementation; independent work continues on other fronts.

## Mission Data (blocks world completion)
1. **Mission ID list**: `Mission.get_ID()` values for all 142 mission subtypes.
   - Status: Extraction in progress (see docs/GAME_DATA_DUMP.md when complete).
   - Fallback: Missions with null game_id are skipped; world generates with verified subset.
2. **Mission titles**: `Mission.get_Title()` for user-facing location names.
   - Can be captured at runtime via the mod's check log.
3. **Goal mission**: Which mission ID should be the victory location?
   - Needs design decision + verification that it's completable.

## Boss/Dungeon Data (excluded from v1.0, needed for v1.1)
4. **OcEmType enum**: int -> boss name mapping (for field boss checks).
5. **OcDungeonID values**: dungeon index -> name (for OnDungeonBossKill).
6. **Tower IDs**: for OcAchievement_RestoreTower.OnAchieve.

## Grant Mechanisms (v1.0 uses skill points only)
7. **Player inventory AddItem**: Non-UI method to add items to player inventory.
   - Status: Hook candidate identified (OcItem_ChestItemList.TryTakeItem is chest-context).
   - Needed for: inventory item rewards (v1.1+).
8. **Gold add method**: TBD.
9. **XP**: `OcPlMaster.AddExp(Int64, Boolean, Boolean)` confirmed; needs testing.

## Validator/Persistence (in progress)
10. **Reset branch runtime confirmation**: Does GameStartCheckSkillPoint's reset
    branch execute in-game? (Zero log matches so far; diagnostic v0.1.1 saw Pass.)
11. **Same-object reload**: Does LoadSaveData get called twice on one object?
12. **Character switch**: GUID/epoch behavior when switching characters.

## Notes
- Do not invent IDs. All game IDs must come from the assembly or runtime.
- Unverified hooks are marked in code with `// UNVERIFIED:` comments and must
  not mutate game state.

