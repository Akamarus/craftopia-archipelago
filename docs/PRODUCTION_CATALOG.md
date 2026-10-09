> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Production Station & Age System Catalog
## Static metadata analysis of `AD__Overcraft.dll` (via dnfile, no execution)

**Method:** Python `dnfile` 0.18.0 metadata inspection of TypeDef/MethodDef/Field tables.
7,701 types enumerated. No IL bodies executed; no game data files read.
**Date:** 2026-10-06

---

## 1. Crafting Stations (verified type hierarchy)

All crafting/production stations derive from **`Oc.OcBuildingBase_Craft`** (itself deriving from `Oc.OcBuildingBase` ← `Oc.OcInstallObj`).

### Direct `OcBuildingBase_Craft` subclasses (8)

| Type | Role (from name + members) |
|---|---|
| `Oc.OcBldg_Aging` | Equipment aging/refinement station (`OpenWindow` only; craft UI) |
| `Oc.OcBldg_BreedPlant` | Plant breeding station |
| `Oc.OcBldg_CraftingBench` | Basic crafting bench (base for Automation) |
| `Oc.OcBldg_Dryer` | Dryer (food/material drying) |
| `Oc.OcBldg_FarmBase` | Farm base (base for Farm, WheatField) |
| `Oc.OcBldg_ItemGenerator` | Base automated item generator (see subtypes) |
| `Oc.OcBldg_ItemModifier` | Base single-item processor (see subtypes) |
| `Oc.OcBldg_RepairWorkshop` | Equipment repair station |

### `OcBldg_ItemGenerator` subtree (automated production)

| Type | Base | Process |
|---|---|---|
| `OcBldg_ItemGenerator_AutoCraft` | ItemGenerator | Auto-craft base (chest pickup, process loop) |
| `OcBldg_ItemGenerator_Alchemy` | AutoCraft | Alchemy |
| `OcBldg_ItemGenerator_CombinedEm` | AutoCraft | Breeding/combining creatures |
| `OcBldg_ItemGenerator_CookingPot` | AutoCraft | Cooking |
| `OcBldg_ItemGenerator_FishPool` | AutoCraft | Fish farming |
| `OcBldg_ItemGenerator_Refiner` | AutoCraft | **Refining/smelting** (the furnace equivalent) |
| `OcBldg_ItemGenerator_WashingMachine` | AutoCraft | Washing |
| `OcBldg_ItemGenerator_CrabPot` | ItemGenerator | Crab pot (passive fishing trap) |
| `OcBldg_ItemGenerator_RecycleMachineBase` | ItemGenerator | Recycle base |
| `OcBldg_ItemGenerator_Composter` | RecycleMachineBase | Composting |
| `OcBldg_ItemGenerator_RecycleMachine` | RecycleMachineBase | Recycling |
| `OcBldg_ItemGenerator_ThermalPowerPlant` | RecycleMachineBase | Power generation |
| `OcBldg_ItemGenerator_StockFarm` | ItemGenerator | Livestock |
| `OcBldg_ItemGenerator_WaterPurification` | ItemGenerator | Water purification |

### `OcBldg_ItemModifier` subtree (single-item processing)

| Type | Process |
|---|---|
| `OcBldg_Campfire` | Cooking/heating |
| `OcBldg_FilletMachine` | Fish filleting |
| `OcBldg_SeedMakingMachine` | Seed production |
| `OcBldg_WeaponDisassembly` | Weapon disassembly (salvage) |

### `OcBldg_CraftingBench` subtree

| Type | Process |
|---|---|
| `OcBldg_Automation` | Automated crafting (has `get_Recipes`, `get_TgtRecipe`, chest linking via `calcNearChest`) |

### `OcBldg_FarmBase` subtree

| Type | Process |
|---|---|
| `OcBldg_Farm` | Crop farming |
| `OcBldg_WheatField` | Wheat field |

### Key `OcBuildingBase_Craft` API (verified method names)
- `get_Recipes` — recipe list for this station
- `StartProcessReq` / `StartProcessInit` / `SyncStartProcess` / `StartProcessAck` — process lifecycle (networked)
- `TakeItem` / `DropItem` / `CheckItemDrop` — product retrieval
- `get_IsProcessing`, `get_ProcessRate`, `CalcTimePass`, `ForceProcessFinish`
- `setAutoCraft`, `getAutoCraftRecipe`, `SelectAutoCraft` — automation
- `get_CraftTimeModify`, `CalcTimeCostPerItem` — time scaling

### NOT a craft station (verified: derives from `OcBuildingBase`, not `OcBuildingBase_Craft`)
`OcBldg_EnchantTable`, `OcBldg_AltarOfAge`, `OcBldg_Market`, `OcBldg_Bank`, `OcBldg_Gacha`, `OcBldg_Chest`, `OcBldg_PetCenter`, etc. (22 direct `OcBuildingBase` subclasses total; 60 `OcBldg_*` types overall).

---

## 2. Age System (verified)

**"Ages" = World Levels.** The age system is the world's technological era progression.

### Core types

| Type | Role |
|---|---|
| `Oc.SoAgeEvoData` : `ScriptableObject` | Per-age data asset |
| `Oc.Maps.OcWorldManager` | Owns `WorldLevel`; `SetWorldLevel`, `AddWorldLevel`, `OnWorldLevelChange`, `get_MaxWorldLevel` |
| `Oc.OcUI_AgeEvolution` : `OcUI_MenuPage` | Age UI; `TryEvoWorldLevel`, `CanEvo`, `GetAgeEvoData`, `RefreshAgeEvo` |
| `Oc.OcBldg_AltarOfAge` : `OcBuildingBase` | In-world altar for age advancement (`OnInteractStart`, `OpenWindow`, `OnSetItem`) |
| `Oc.SoGameSystemInfo` | `get_MaxWorldLevel` (global cap) |
| `Oc.OcResidentData` | Singleton holding `_ageEvoData` array (`get_AgeEvoData`) |

### `SoAgeEvoData` fields (verified — advancement requirements)

| Field | Meaning |
|---|---|
| `worldLevel` | The age/level number |
| `featuredItems` | Featured items for this age |
| `requireGold` | Gold cost to advance |
| `requireItems` | Item requirements (with `get_RequireItemCounts`) |
| `isEnabled` | Whether this age is enabled |
| `get_NextLevel` | Next age level |
| `get_NewItems` | Items unlocked by this age |

### Age-gated content (verified hooks)
- `Oc.Item.OcItemDataMng.SetupAgeEvoItemList` / `ageEvoNewItemLists` field / `GetWorldEvoNewItem` — items unlocked per age
- `Oc.Item.UI.OcItem_CraftSheet.get_NeedsWorldLevelTerm` — recipes can require a world level
- `Oc.Item.UI.OcItemUI_CraftMng.UpdateWorldLevelLockUI` — craft UI shows age locks
- `Oc.Missions.MissionWorldLevelUpProc.OnWorldLevelChange` — missions react to age-up
- `Oc.Missions.OcMissionManager.CheckWorldLevelMissionUnlock` — age-gated mission unlocks

### Age advancement flow (from method names)
1. Player interacts with `OcBldg_AltarOfAge` → `OpenWindow`
2. `OcUI_AgeEvolution.TryEvoWorldLevel` checks `CanEvo` (gold + items from `SoAgeEvoData`)
3. `OcWorldManager.SetWorldLevel` / `AddWorldLevel` → `OnWorldLevelChange` event

---

## 3. Recipe System (verified)

**Recipes are defined on the product's `ItemData`, not in a separate recipe class.**

### `Oc.Item.ItemData` : `OcCsvScriptableObject` (128 fields; recipe-relevant subset)

| Field | Meaning |
|---|---|
| `familyId` | Craft family (which station category) |
| `NO_CRAFT_FAMILY_ID` | Constant: not craftable |
| `PLAYER_CRAFT_FAMILY_ID` | Constant: hand-craftable |
| `WORKBENCH_CRAFT_FAMILY_ID` | Constant: requires workbench |
| `workbenchId` | Specific station ID required |
| `workbenchLv` | Station level required |
| `playerCraftCount` | Player-hand-craft batch size |
| `carftTimeCost` | Craft time (note: typo in original) |
| `materialA_Id` … `materialE_Id` | Up to 5 material item IDs |
| `materialA_count` … `materialE_count` | Up to 5 material counts |

### Recipe manager: `Oc.Item.OcItemDataMng` (verified methods)
- `get_BlueprintRecipes`, `get_BlueprintToRecipeDic`, `get_RecipeToBlueprintDic`
- `SetupCraftableItems`, `SetupCraftBenchMap`, `GetCraftBenchFamilySorted`
- `SetupAgeEvoItemList` (age-gated items)
- `UnlockRecipe` / `GetRecipeUnlocked` / `IsItemUnlocked` (recipe unlock state)
- `GetRecipeList`, `GetCookRecipeList` (cooking is a separate recipe list)
- `CraftItem` (the actual craft execution)
- `LoadRecipeSaveData` (recipe unlock persistence)

### Craft execution entry points (verified)
- `Oc.Item.OcItemDataMng.CraftItem`
- `Oc.Item.UI.OcItemUI_CraftMng.TryCraftItem`
- `Oc.Item.OcItem_CraftEnchantSelect.TryCraftItem`

---

## 4. Production Processes (distinct, verified from type hierarchy)

| # | Process | Station type(s) |
|---|---|---|
| 1 | Hand crafting | Player (`PLAYER_CRAFT_FAMILY_ID`) |
| 2 | Workbench crafting | `OcBldg_CraftingBench`, `OcBldg_Automation` |
| 3 | Equipment aging/refinement | `OcBldg_Aging` |
| 4 | Equipment repair | `OcBldg_RepairWorkshop` |
| 5 | Smelting/refining | `OcBldg_ItemGenerator_Refiner` |
| 6 | Cooking | `OcBldg_ItemGenerator_CookingPot`, `OcBldg_Campfire` |
| 7 | Alchemy | `OcBldg_ItemGenerator_Alchemy` |
| 8 | Drying | `OcBldg_Dryer` |
| 9 | Washing | `OcBldg_ItemGenerator_WashingMachine` |
| 10 | Composting | `OcBldg_ItemGenerator_Composter` |
| 11 | Recycling | `OcBldg_ItemGenerator_RecycleMachine` |
| 12 | Disassembly/salvage | `OcBldg_WeaponDisassembly` |
| 13 | Filleting | `OcBldg_FilletMachine` |
| 14 | Seed making | `OcBldg_SeedMakingMachine` |
| 15 | Plant breeding | `OcBldg_BreedPlant` |
| 16 | Creature combining | `OcBldg_ItemGenerator_CombinedEm` |
| 17 | Crop farming | `OcBldg_Farm`, `OcBldg_WheatField` |
| 18 | Livestock | `OcBldg_ItemGenerator_StockFarm` |
| 19 | Fishing (passive) | `OcBldg_ItemGenerator_CrabPot`, `OcBldg_ItemGenerator_FishPool` |
| 20 | Water purification | `OcBldg_ItemGenerator_WaterPurification` |
| 21 | Power generation | `OcBldg_ItemGenerator_ThermalPowerPlant` |
| 22 | Item modification | `OcBldg_ItemModifier` (base) |

**No furnace type exists** — smelting is handled by the Refiner. No "Station" or "Furnace" in type names (verified: 0 hits).

---

## 5. Building Organization

- **60** `OcBldg_*` types total.
- **No building category enum found** in metadata (only `BldgMountType`, `Bldg_PutModeType`, `InstallingGndStateType` — placement-related, not category).
- Buildings have a **Phase** state machine (`ProcessPhaseType`: IDLE → PROCESS_READY → PROCESS_REQ → PROCESS_LOOP_PLAY → PROCESS_FINISH).
- Craft stations: 8 direct + 14 ItemGenerator variants + 4 ItemModifier variants + Automation + 2 Farm variants = **29 production-capable building types**.

---

## 6. Determinable vs NOT Determinable Statically

### DETERMINABLE (verified above)
- Complete station type hierarchy and inheritance
- Station API surface (method/field names)
- `SoAgeEvoData` schema (requirement fields)
- Age advancement flow (method call chain names)
- Recipe data schema (on `ItemData`)
- Recipe manager API
- Process phase state machine
- Age-gating hooks for recipes/missions

### NOT DETERMINABLE statically (requires game data or runtime)
- **Actual age count and names** — `SoAgeEvoData` instances are Unity assets, not in the DLL
- **Actual age requirements** (gold/item amounts) — data-driven
- **`MaxWorldLevel` value** — from `SoGameSystemInfo` asset
- **Complete recipe catalog** — recipes are `ItemData` assets (CSV/ScriptableObject); IDs and material mappings are data
- **`workbenchId` → station mapping** — the ID values are data; the enum/constant values are not in metadata
- **Which items each station can craft** — requires `craftBenchMap` runtime data
- **Age unlock item lists** — `ageEvoNewItemLists` is runtime-populated
- **Station unlock order/progression** — no static dependency graph found
- **Building placement categories** — no category enum in metadata

### Specific runtime evidence needed
1. Dump `SoAgeEvoData` assets: count, names, `worldLevel`, `requireGold`, `requireItems`+counts, `isEnabled`, `newItems` per age
2. Dump `SoGameSystemInfo.MaxWorldLevel`
3. Dump `ItemData` recipe table: for each craftable item, `familyId`, `workbenchId`, `workbenchLv`, materials+counts, `carftTimeCost`
4. Resolve `workbenchId` integer values to station types (via `craftBenchMap` or `GetCraftBenchFamilySorted`)
5. Dump `ageEvoNewItemLists` (which items unlock per age)
6. Verify `OcBldg_AltarOfAge` interaction flow in-game (does it consume the required items?)
7. Confirm `TryEvoWorldLevel` requirements check vs actual deduction

