# Dynamic medal merchant resolved — 2026-10-16

## Result

All **14 previously unresolved LimitedDungeon equipment catalogs now have a concrete player-built merchant instantiation route**. The source is **Medal Trader's Table3522**, which summons an ordinary merchant and replaces its shop with a seeded selection from `SoRandShopWeight_BossChallageMerchant`. The catalog names did not identify a fixed merchant inside the challenge dungeon. The dungeon supplies the medal currency; the table supplies the trader.

Together with the earlier53 active open-world placements, all67 shop-backed equipment items now have placement or instantiation evidence. This does not make them opening equipment or establish safe access, affordable purchase, or an AP tier.

## Exact item-to-merchant chain

`OcItemAssetMng` loads `ItemPrefab/oc_pf_ItemPrefab_{id:D5}` with `Resources.Load`. Installed resources GameObject51080 is `oc_pf_ItemPrefab_03522`. Its `OcItemPrefab` explicitly stores `itemID=3522` and `_EquipPrefab→resources:544572`. That equipment component's `_InstalledActiveGameObject` includes GameObject83912, whose enabled component539257 is `OcInstallObj_SummonNPC`.

Component539257's managed-reference `_overwriteProcesses` contains `OcSpawnerOverwriteRandomShopListProcess`, whose `_weightListSo` is fileID2/path91290, resolving to `sharedassets0.assets:91290`. The same component exists in sharedassets0 at101112 beneath root `oc_pf_ItemPrefabParts_00000_01_EquipPrefab--SummonNPC_3522`. Both records passed strict full-byte decoding.

The matching assembly's `OcInstallObj_SummonNPC.itemIDToEnemyTypesDic` maps3522 solely to `NPC_Merchant`, weight10. After installation the phase is `JustPut`; the first host day-end advances to `Preparation` and spawns at the table's position. `OnSetSummonedEmGUID` registers a postponed request, which invokes `OnSpawnComplete`; that method executes every configured overwrite process. The random-shop overwrite calls `SetShopList(_weightListSo.GetRandomShopList(em.GUID))`. The next day-end advances to Return, and the move loop deactivates the NPC when its no-hit check permits, resets the timer/phase, and allows another cycle. This is a recurring summon mechanism, with no promise of a particular catalog on one summon.

The weight asset contains ten catalogs, each weight10, total100. All14 target offers require **raw world level9**, cost base **50 Boss Challenge Medals (Red)6665**, and have kill drops disabled. Commercial Mind can reduce payment. These facts remain unchanged from the prior catalog audit.

| ID | Equipment |
|---:|---|
|1727|Flamberge|
|1843|Gram|
|1906|Spear of Achilles|
|2521–2522|Assault Dragon Mail for Men/Women|
|2523–2524|Magic Dragon Mail for Men/Women|
|5509–5510|Assault Dragon Helm for Men/Women|
|5511–5512|Magic Dragon Helm for Men/Women|
|6380|Necro's Cane|
|6455|Tanukigasaki|
|6532|Gungnir Storm|

## Table and dungeon prerequisites

The verified runtime capture includes3522 in the **live20011 crafting list**, with no blueprint requirement. Its recipe is raw workbench level9, displayed recipe age10, and consumes **Log746×5, Orichalcum Ingot719×1, Iron Ingot712×1**. Thus this is a demonstrated normal recipe, not historical ingredient fields on an uncraftable item. The earlier appearance-status evidence remains status2.

The Boss Challenge Dungeon Gate3831 is also in that live crafting list, no blueprint required, raw workbench level9/displayed age10. Its recipe consumes **Goddess's Rune3341×1, Stone698×100, Orichalcum Ingot719×10**. Resource GameObject129512 is `oc_pf_ItemPrefab_03831`, with explicit itemID3831 and equipment pointer599302. The sharedassets0 gate prefab root `oc_pf_ItemPrefabPart_20301_Core_LimitedBossDungeon_3831` has `OcBldg_LimitedBossDungeon`94640, warp112758 and console94608. Its entrance source90786 (`So--OcDungeonEntrance_090_LimitedBoss`) points to dungeon source90788 (`sourceId90`, `battleDungeonType3`, addressable GUID `4be9b1d0b8ad7b74c974a0733a32a631`). The entrance is not under development.

`OcBldg_LimitedBossDungeon.CanInstall` limits installed copies of the item to one. The console opens challenge selection through `OcDungeonManager.TryOpenDungeonSelectUI`. The inspected selection path permits choosing entries in `SoLimitedBossList`; its manager checks entrance validity and whether an existing dungeon can be closed (no players inside). `OcDungeon_LimitedBoss.CanStart` requires a selected challenge and no ongoing sequence. These inspected methods introduce no extra world-age payment or prior-boss-clear check. This statement is limited to the traced methods, not an assertion that every surrounding building/warp condition has been exhaustively audited.

## Red medal sources and combat conditions

Five of the30 installed challenge records award6665. `OcDungeon_LimitedBoss.State_Win` calls `GiveReward` for the local player in the dungeon (resurrecting that player first if dead); `GiveReward` calls `TryTakeItem` for each configured reward. The complete challenge records are retained in JSON.

| Challenge asset, sharedassets6 | Enemy level | Red medals per victory | Selected additional conditions |
|---|---:|---:|---|
|42072, DragonLv1|80|3|No cold/fire/poison environment damage; player periodic damage disabled|
|42073, DragonLv2|100|5|Fire level4; boss heals1000 every20s; player periodic damage3 every15s|
|42074, DragonLv3|120|10|Fire level4; boss heals10% every60s; player periodic damage30 every10s|
|42047, SatyrosLv3|150|1|Boss heals2000 every20s; player periodic damage40 every5s|
|42056, GriffonLv3|150|1|Cold2 and Poison2; boss heals3% every10s; player periodic damage5% every10s|

Without purchase discounts, DragonLv1 alone would require17 victories to accumulate at least50 red medals. Higher challenges have stronger conditions and weapon-effectiveness restrictions preserved in JSON. This arithmetic is not a recommendation that a particular challenge is safe or quickest. The table recipe, gate recipe, currency wins and raw world-level purchase check all matter; item rank is not being used as the access gate.

## Coverage and limitations

The targeted defaultlocalgroup/subworld scan decoded181 spawners, four LimitedBoss room components and one LimitedBoss dungeon. The dungeon prefab's `_spawnerList` is empty; none of these181 spawners supplied the random-shop process. The decisive follow-up scanned raw MonoBehaviour data in resources, all loose sharedassets and all level files for the process class string and path91290. It found the two summon components above. Other numeric91290 matches were unrelated pointers and were discarded. The12 earlier failed level10 spawners still fail full-byte checks; they are not used as evidence. Eight defaultlocalgroup script references remain unresolved, also excluded.

Artifacts are under workspace `artifacts/dynamic-merchants/`; `build.py` assembles the companion report. Existing remaining reports were not overwritten. No game, save, mod, APWorld, or workbook was modified. No runtime craft/summon/purchase/boss test was performed. Ingredient acquisition, safe combat, alternative routes and final AP balancing remain separate work.

