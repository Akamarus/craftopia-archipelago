> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# M0 — Hook Inventory (FINAL)

**Status:** verified against the actual game assemblies (dump 2026-10-05, current Steam build).
Game code lives in `AD__Overcraft.dll` (8.7 MB; "Overcraft" = Pocketpair's internal
project name). `Assembly-CSharp.dll` is third-party asset code, not game logic.

## Checks (locations) — all confirmed in this build

### 1. Missions (primary check source)
- Base class: `Oc.Missions.Mission` — 142 concrete subtypes
  (`BossKillAscension`, `DiscoverDungeonMission`, `EnemyKillMission`, `ItemCraftMission`, …)
- `Int32 get_ID()` — stable mission ID → use as the AP check identity
- `Boolean TryGetReward()` — **patch point** (Harmony postfix; already proven by a public mod)
- `Boolean get_IsRewardTaken()`, `Int32 get_Category()`, `String get_Title()`
- `MissionSaveData get_msData()` — per-mission save state
- Manager: `Oc.Missions.OcMissionManager` (singleton `Inst` per public mod usage)
  - `Mission[] GetMissionLists()`, `Mission GetMission(Int32 id)`
  - `IEnumerable GetClearedMissions()` — **offline-check recovery**: enumerate on connect
  - `Void add_MissionClearCallback(Action value)` — event subscription alternative to patching
  - `MissionSaveData[] ConvertToSaveData()` / `Void LoadSaveData()`

### 2. Dungeon boss kills
- `Oc.Missions.OcMissionManager.OnDungeonBossKill(Int32 dungeonIdx)` — the game calls
  this itself when a dungeon boss dies. Postfix patch = reliable detection with dungeon index.
- `Oc.OcDungeonID`: `Int32 get_ID()`, `String get_DungeonName()`
- `Oc.Achievements.OcAchievement_DungeonClear` — dungeon-clear achievement (alt. signal)

### 3. Boss kills (field/world bosses)
- `Oc.Missions.OcMissionManager.OnMissionBossKill(OcEmType enemyType, Int32 enemyLevel)`
- `Boolean IsMissionBoss(OcEmType enemyType, Int32 enemyLevel)`
- `Oc.OcEmType` is an enum — **follow-up needed**: int→boss-name mapping requires a
  fields dump (this dump listed methods only) or dnSpy.
- Fallback: `Oc.OcEnemyHeader.get_Dead()`, `Oc.Em.AsEm_Death` action states.

### 4. Tower repair
- `Oc.Achievements.OcAchievement_RestoreTower.OnAchieve()`
- Manager: `Oc.Achievements.OcAchievementManager` — `GetAchievement(Int32 id)`,
  `TryClearAchievement`, `LoadSaveData()`/`Save()` — achievements are a full check
  source with persistence (17 achievement types in this build).

### 5. Boss Rush
- `Oc.OcBossRushDungeonRoom` exists (`MoveNext()`, `WarpToStartPoint()`); floor-clear
  detection TBD in M4 via `OnDungeonBossKill` or room state.

## Item grants — confirmed

### 1. Skill points (M1 grant mechanism)
- `Oc.OcPl.get_SkillCtrl()` → `Oc.OcPlSkillCtrl.AddSkillPoint(Int32 value)` ✓
  (matches public mod usage `OcPlMaster.Inst.SkillCtrl.AddSkillPoint(1)`)

### 2. Inventory items (M4)
- `Oc.Item.OcItem_ChestItemList.TryTakeItem(OcItem addItem, Int32 amount)` — chest context.
- Player-inventory add method **TBD in M4** (UI classes expose `AddItem(ItemData)`;
  the player inventory manager needs identifying).
- `Oc.Item.InventorySaveData`, `ClientInventorySaveData`, `InventoryBoxSaveData` exist.

### 3. Misc
- `OcPlMaster.AddExp(Int64, Boolean, Boolean)` — XP grants. Gold add **TBD**.

## Save format
- `PocketpairSave.PocketpairSaveManager` (+ `SaveDataConnection`, `Entity`)
- Per-system save/load on the managers above (`OcMissionManager`, `OcAchievementManager`).
- Offline-check recovery strategy: on connect, enumerate `GetClearedMissions()` +
  achievement states and send any checks the server hasn't seen. Save-binary parsing
  is a fallback, not the primary plan.

## M0 exit status
- [x] Engine backend confirmed: Mono → BepInEx 5 (already installed on the operator's machine)
- [x] Every check/item type has a named class+method (or a documented fallback)
- [~] Save format: managers + per-system save data identified; binary format not dissected (not needed — manager APIs cover our use)
- [x] Dev env: repo scaffolded; Archipelago source cloned for reference; C# build will happen here via dotnet SDK (the operator sends reference DLLs; no build tools needed on his machine)

## Follow-ups (M4, not blocking M1)
1. `OcEmType` int→boss-name mapping (fields dump or dnSpy).
2. Player inventory `AddItem` (non-UI) + gold add method.
3. Confirm singleton accessors (`Inst`) at mod compile time against the real DLLs.

