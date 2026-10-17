# Enemy drops and capture — installed-build audit, 2026-10-16

The installed drop tables substantiate the reported skeleton, mage, goblin, sheep, chicken, phoechick and Reaper material sources. They also show why a generic label such as “all goblins” or “all cows” is too broad. This audit establishes exact table membership and base roll behavior. Geezah/Yarden placement, nighttime spawning, dungeon ordering, and respawn timing remain player-reported access evidence, not conclusions of these tables.

## Evidence and extraction

`drops.json` contains 851 complete `Oc.SoEnemyData` asset records reduced to relevant scalar fields and all nonempty drop slots, 311 enemy-family prefixes, and indexed positive slots for the eight requested items. All 705 installed sharedassets files were scanned. Enemy-data objects occur in sharedassets0,6,10,13; their SHA-256 hashes are preserved. Full enemy-data reads consumed exactly the expected bytes, with zero decoding errors. Family records were deliberately decoded only through `SoEmData_Ex`; they are not full-object validation. Local reference IDs are resolved; external references remain explicit.

The DLL hash is `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`, matching the prior audit and capture. ILSpy methods cited below come from that DLL. UnityPy/TypeTreeGenerator use the same setup described in the prior environment audit. Synthetic generated MonoBehaviour header values were excluded; asset filename/path ID is the provenance key.

## Verified material routes

Numbers below are base per-lottery rates before buffs and eligibility conditions. Rate1 means a 100%-configured eligible slot, not a promise every encounter yields that item. Quantities are the slot quantities, with no damage-threshold bonus for the named living enemies below.

| Item | Actual family/data | Positive slots |
|---|---|---|
| Elemental Dust2460 and Bone695 | Skeleton_1HAxeShield558–561, Skeleton_Bow562–565, Skeleton_Pirate_Captain566–569, Skeleton_Pirate_Sailor570–573 | Each item x1 at1.0, every listed variant |
| Elemental Dust2460 | Wizard554–557 | x1 at1.0 |
| Crystal2453 | Wizard554–556 /557 | x1 at1.0 / x1 at0.2 |
| Dust2460 / Crystal2453 | WizardPoison579–582, Lightning583–586, Ice587–590, Fire591–594 | Dust x1 at0.5; Crystal x1 at0.2 |
| Fang680 | Goblin_Sword534–537, Archer538–541, Nohand542–545 | x1 at1.0 |
| Fang680 | GoblinWarrior11,261–263 | x1 at0.5 |
| Leather683 | Sheep21 | x1 at1.0 |
| Leather683 | Cow22 | x2 at1.0; higher Cow variants differ, see below |
| Feather692 | Chicken20 / BigChicken29 | x3 at1.0 / x1 at1.0 |
| Feather692 | ChickPhoenix372–375 | x1 at1.0; also Dust x1 at0.1/0.2/0.3/0.5 respectively |
| Reaper Spirit Scale3355 | Reaper225–228 | x1 at1.0, slot5 in all four variants |
| Bone695 | Reaper225–228 | slot0 x6 at1.0 plus separate slot1 x6 at0.7/0.8/0.9/0.95 |
| Captured Anubis1502 | NPC_Mob_Anubis -> data121, sharedassets6 path40049 | `capturedItemId=1502`; capture output, not an ordinary loot slot |

Family selection is explicit: `SoEnemy.getEmDataIndex` uses `OcDefine.EM_LV_RANGE=20`, selecting available successive references at levels20,40,60. Missing references stop advancement. `OcEm.levelRevise` also has extra-table branches for special forms/grade3; do not identify every encountered enemy solely by a base family name.

Cow family references are22,218,219,220. Data218/219 contain no positive Leather683 slot; data220 has Leather x1 at0.7. Thus “cows drop leather” is supported for the base cow but is not a uniform all-level rule. Older SkeletonGreen/Orange/Purple assets also differ from the weapon-bearing skeleton families; the latter provide the strong Dust/Bone evidence.

Crystal also has positive slots in static-object data: data58 (sharedassets13 path179) gives Crystal x1 at1.0 with damage threshold100; data70/71/73–79 (sharedassets0 paths80663–80671) gives Crystal x1 at0.2 in slot5 with threshold40 in inspected data70. These are concrete harvesting-table leads. Their association with particular cave rocks, tool requirements, named caves, or boss spawns was not independently resolved here. Do not infer it from numeric IDs alone.

## Drop mechanics and conditions

`EnemyDrop.LotteryDropItem` loops all ten slots independently. A slot needs positive item ID, positive quantity, positive rate, and random float below `base_rate * OcUtility.getDropItem_BonusRate()`. It then creates the item and rejects null, old-data or non-`IsAppearIngame` items. It invokes the drop callback with `Clamp(quantity + addLotteryNum,0,255)`. Zero quantity/rate placeholder slots are preserved in JSON but excluded from the positive-source index.

`OcUtility.getDropItem_BonusRate` defaults1 and selects10,5,2,1.3 for active Anubis_Treasure_100/50/20/13 buffs in that priority order. These are probability multipliers, not extra item rolls. Values above1 saturate the success comparison; a serialized rate5 is not a five-item quantity.

`OcItemDropper.doKilledDropItem` is host-only. It calls the normal lottery with +2 quantity when the table has a positive `DamageThresholdForDropItem`. `checkDamagedDropItem` can roll on cumulative damage thresholds, adding floor(damage/threshold)-1 quantity (capped255) and retaining the remainder. This makes a resource table's rate a per-damage-event rule, not automatically a per-node yield.

Crucial exception: `OcItemDropper.lotteryDropItem` checks the target's Fire condition. If burning and `FireItemId>0`, it tries only the fire replacement item at20%; otherwise it calls the normal table. The inspected primary records20,21,22,121,225,372,558 all have FireItemId0, so that exception does not suppress those records; other families may differ. `OcEm.DropItem` additionally requires not battle-friend, a dropper, and no IgnoreDropItem flag. These guards are why configured rate1 should not be presented as unconditional encounter yield.

## Capture and renewal

`OcItemDropper.doCapturedDropItem` creates one item using the table's CapturedItemId and enemy level; `OcEm.chaptureActEnd` invokes it. Data121 provides the exact Anubis1502 link. This supports the player's prism-capture account but does not independently establish Geezah position, prism recipe/access or every target-capturability guard.

`OcEm.checkCaptureLottery2` bases chance on remaining HP: <=1% gives1, <=10% gives0.9, <=30% gives0.8, <=40% gives0.6, <=50% gives0.4, otherwise0. Prism rank thresholds >6/>8/>10/>12 multiply by1.1/1.2/1.25/1.3. `calcCaptureChanceLevelAndBoss` adds0.1/0.25/0.5 when player exceeds enemy level by at least1/5/10, then multiplies by0.7 for boss/grade2/midboss. External probability is then added and compared against the character random draw. Force-capture and Charm branches return true. This is the lottery method, not a complete proof that any particular NPC accepts a capture attempt.

`OcEm.deactivate` notifies its OwnerSpawner via `notifySpawneeKilled`. That is evidence of a spawner lifecycle, not sufficient evidence of renewal interval or unlimited availability. No per-location respawn schedule or night-only spawn configuration was decoded in this audit. Preserve the user's gameplay observations as separate evidence and test reset/return behavior before encoding a renewable AP supply rule.

No game, save, mod or APWorld code was changed.
