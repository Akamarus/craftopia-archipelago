> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Coverage Matrix

**Date:** 2026-10-06
**Baseline:** v1.0.8

## Skill Tree Catalog (NEW — verified 2026-10-06)

**280 skills**: 168 active (`OcPlActiveSkillType`), 112 passive (`OcPlPassiveSkillType`).
**8 categories** (`OcPlSkillCategory`): None, Basic, Combat, Magic, Life, Trick, Tamer, PetOnly.
**5 tiers**, **6 levels**. Manager: `Oc.Skills.OcSkillManager`.
**Prerequisites**: 5 lock types (Skill, Boss, Item, Mission, Quest).
Per-skill assignments need runtime asset dump.
Full catalog: `docs/SKILL_CATALOG.md`.

## Equipment Catalog (NEW — verified 2026-10-06)

**10 equipment slots** (`Oc.OcEquipSlot`): WpMain, WpSub, WpTwoHand, WpDual, EqHead, EqBody, FlightUnit, Ammo, Accessory, Accessory_Dummy01.
**46 weapon categories** (`Oc.OcWpCategory`): combat-relevant are Sword(5), Bow(6), Shield(7), Lance(10), SwordTwoHand(28), Gun_TH(35), Katana(36), MagicStaff(37), VacuumGun(43).
**Attack styles** (`Oc.OcAttackWeaponAttr` bitflags): 9 styles matching per-weapon damage fields.
**No `*Armor*` type** — armor is only EqHead/EqBody slots.
**Tiers**: No `level`/`tier` on `ItemData`; tier signals are `rank`, `rarity`, `_Refine_MaxLv`, `workbenchLv`. Instances have `_level`.
Full catalog: `docs/EQUIPMENT_CATALOG.md`.

## Production Stations (NEW — verified 2026-10-06)

**29 production-capable building types** verified via `OcBuildingBase_Craft` inheritance.
Full catalog: `docs/PRODUCTION_CATALOG.md`.

| Station Group | Types | Status |
|---------------|-------|--------|
| Basic crafting | CraftingBench, RepairWorkshop, Aging | **Cataloged**; hooks pending |
| Automated | Alchemy, CookingPot, Refiner (smelter), Composter, CrabPot, FishPool, StockFarm, WashingMachine, WaterPurification, ThermalPowerPlant, Automation | **Cataloged**; hooks pending |
| Processing | Campfire, FilletMachine, SeedMakingMachine, WeaponDisassembly, Dryer, BreedPlant | **Cataloged**; hooks pending |
| Farming | Farm, WheatField, FarmBase | **Cataloged**; hooks pending |

**Age system**: "Ages" = World Levels. `SoAgeEvoData` (ScriptableObject), `OcWorldManager.SetWorldLevel`, `OcBldg_AltarOfAge` advancement altar. Actual age count/names require asset dump (runtime).

**Recipe system**: Recipes on `ItemData` (`familyId`, `workbenchId`, `workbenchLv`, 5 materials). Manager: `OcItemDataMng`.

## Check Categories

| Category | Status | Evidence | Notes |
|----------|--------|----------|-------|
| Missions | **Supported** | `Mission.TryGetReward()` postfix verified; 53 classes from IL | Class-based, not instance-based; reachability unverified for all |
| Age advancement | **Experimental** | No hook verified | Requires runtime evidence of age advancement method |
| Soul orbs | **Experimental** | No hook verified | Requires orb collection detection |
| Overworld chests | **Experimental** | No hook verified | Requires chest open detection; respawn handling unknown |
| Challenges | **Experimental** | No hook verified | Requires challenge reward detection |
| Anubis awards | **Experimental** | `TalkToAnubisMission` class exists | Award vs talk distinction needs runtime |
| Shops | **Blocked** | No hook verified | Requires new shop UI; finite purchases need implementation |
| Boss milestones | **Experimental** | `OnDungeonBossKill` is private (per dump) | Hook target needs verification |

## Reward Types

| Reward | Status | Evidence | Notes |
|--------|--------|----------|-------|
| Skill points | **Supported** | `OcPlSkillCtrl.AddSkillPoint()` verified | Transactional delivery via v1.0.8 |
| Equipment tiers | **Blocked** | Permission model scaffolded | No catalog; no equip/use hooks verified |
| Production unlocks | **Blocked** | Permission model scaffolded | No station catalog; no use hooks verified |
| Skill trees | **Blocked** | Permission model scaffolded | No tree catalog; no effect gating verified |
| Money | **Blocked** | No grant mechanism verified | Requires inventory/money API |
| Materials | **Blocked** | No grant mechanism verified | Requires inventory API |

## Victory Conditions

| Condition | Status | Evidence | Notes |
|-----------|--------|----------|-------|
| Boss | **Experimental** | Goal option exists | Boss list unverified; detection hook needed |
| Age | **Experimental** | Goal option exists | Age targets unverified; advancement hook needed |
| Objective count | **Supported** | Mission checks counted | Counts locations, not distinct objectives (bridge tracks) |

## Multiplayer

| Feature | Status | Evidence | Notes |
|---------|--------|----------|-------|
| Separate slots | **Experimental** | Protocol designed | Requires multi-bridge testing |
| Shared slot | **Blocked** | Protocol designed | Requires roster binding implementation |
| Roster binding | **Blocked** | Design only | World ID extraction needs runtime |
| Offline catch-up | **Experimental** | Bridge has offline queue | Team catch-up needs implementation |

## Persistence

| Feature | Status | Evidence | Notes |
|---------|--------|----------|-------|
| Skill point transactions | **Supported** | v1.0.8 verified | 190 assertions passing |
| Durable enrollment | **Blocked** | Design only | Requires implementation + review |
| Generalized rewards | **Blocked** | Skill points only | Each reward type needs transaction design |

## Player Feedback

| Feature | Status | Evidence | Notes |
|---------|--------|----------|-------|
| Unlock status display | **Blocked** | Not implemented | Requires in-game UI |
| Blocked-action explanations | **Experimental** | `GetBlockExplanation()` API exists | No UI hook |
| Goal tracker | **Blocked** | Not implemented | Requires in-game UI |
| Connection status | **Supported** | Bridge logs | Basic logging only |

## Runtime Evidence Still Needed

1. Equipment catalog: weapon/armor types, tiers, equip hooks
2. Skill tree catalog: tree IDs, skill definitions, effect gating
3. Production catalog: station IDs, use hooks, age requirements
4. Boss list: verifiable boss identities
5. Age system: advancement method, age list
6. Soul orb locations and collection detection
7. Chest locations and open detection
8. Shop system: purchase hooks, inventory API
9. World ID: stable world identifier for roster binding
10. Multiplayer: host/client authority boundaries

