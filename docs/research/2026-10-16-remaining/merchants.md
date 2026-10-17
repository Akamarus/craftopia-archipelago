# Merchant placement and access audit — 2026-10-16

## Result

Of the 67 previously shop-backed equipment items, **53 now have a catalog-to-active-open-world-spawner join**. The remaining 14 are configured in ten LimitedDungeon catalogs selected through a random-shop weight list; their actual merchant instantiation/location is still unresolved. This is placement evidence, not a guarantee that a player can safely reach or afford every shop at the start.

The important herb alternative is now concrete: **Spring's farming merchant trades Tomatoes, and the fifth-biome farming merchant trades Pumpkins**, for Snow Mint, Hell Pepper, Bright Red Herb, and Ultramarine Herb. All eight entries require raw world level 0 and have base price 10 crop items each, before the Commercial Mind purchase discount. They are not ten-gold offers.

## Evidence and scope

Installed `sharedassets10.assets` contains nine ordinary equipment catalogs (objects212–220), Bossrush221, and farming catalogs222–231. `sharedassets0.assets` contains ten LimitedDungeon catalogs91291–91300 and `SoRandShopWeight_BossChallageMerchant`91290. Byte-consumption-checked UnityPy decoding resolves managed-reference cashiers, sale entries, and overwrite processes. All equipment sale fields match the preceding capture-backed equipment audit, with zero differences in the68 compared route records.

`globalgamemanagers` BuildSettings index10 identifies `Assets/_Overcraft/Scene/OpenWorld/OcScene_OpenworldMap_Main_Spawner_Convert.unity`. Its `OcSpawner` objects contain `OcSpawnerOverwriteSoShopListProcess._overwriteSo` references directly into sharedassets10. These are actual serialized world placements, not simply NPC prefabs or shop names. A separate ordinary Unity-object pass confirms enabled components and active GameObject ancestry. Two copies under `EnemySpawner/NPC_SpawnTest` are inactive and excluded. All21 ordinary placements below are enabled and have active ancestry.

Companion JSON preserves all67 item routes, prices, required raw world levels, cashiers, all30 catalogs,23 placement records, ancestry, spawner settings, and coordinates. The exact original item names and IDs remain in each equipment row. Internal biome names are retained rather than guessing the nearest tower or localized region.

## Actual ordinary placements

Every row uses level10; listed object IDs identify `OcSpawner`. Catalog suffixes refer to `SoShopList_OW_Overwrite_`. Equipment catalogs01–09 use money with `_costRate=1`, not the class default multiplier3.

| Catalog | Active placement / internal biome | Spawner object |
|---|---|---:|
| 01 | 1st_biome | 55098 |
| 02 | 2nd_biome | 55161 |
| 03 | 3rd_biome / 3rdBiome_1 | 55120 |
| 04 | 3rd_biome / 3rdBiome_2 | 55065 |
| 05 | 4th_biome_Spring | 55051 |
| 06 | 5th_biome | 55152 |
| 07 | 6th_biome | 55143 |
| 08 | 7th_biome_desert | 55109 |
| 09 | 8th_biome_poison; also9th_biome_volcano | 55121;55132 |
| Bossrush | worldmap / BossRush | 55111 |
| farming_01 | 1st_biome; Potato1222 | 55096 |
| farming_02 | 2nd_biome; Rice2052 | 55104 |
| farming_03 | 3rdBiome_1; Cabbage1202 | 55102 |
| farming_04 | 3rdBiome_2; Cabbage1202 | 55032 |
| farming_05 | 4th_biome_Spring; Tomato1204 | 55145 |
| farming_06 | 5th_biome; Pumpkin1234 | 55173 |
| farming_07 | 6th_biome; Corn1208 | 55135 |
| farming_08 | 7th_biome_desert; Eggplant1228 | 55107 |
| farming_09 | 8th_biome_poison; Orange1210 | 55127 |
| farming_10 | 9th_biome_volcano; Pineapple1224 | 55043 |

The Volcano farm object's name ends `_farmingshop_09`, but its actual pointer is catalog_farming_10; do not infer catalog identity from the GameObject name. Likewise both Poison and Volcano ordinary equipment merchants point to catalog09.

Bossrush uses `OcMultiItemCashier`: Rush Silver Coin6681 is one value unit; Rush Gold Coin6682 is ten units. It does not use ordinary gold. Mechanics Cargo for Men2525 and Women2526 appear in farming04 at raw level0 and base50 Cabbages each.

## Herb route and conditions

Spring farming05's spawner origin is approximately **(912.739,71.818,-1342.322)**; farming06's is **(1544.413,257.453,27.442)** in Unity world x/y/z. Ancestor transforms are identity. These are spawner origins; a child spawn transform can offset the actual NPC. Both point to enemy type164, have developer status1, one initial/max NPC, no configured extra spawn conditions, no day/night restriction, no manual-spawn flag, raw island bounds0..99, and a60-second respawn interval. The fixed NPC levels35/40 are enemy levels, not shop purchase-age requirements. All21 active merchant placements share these empty/day-night/manual condition settings.

Each of these two shops offers Hell Pepper3360, Snow Mint3445, Bright Red Herb723, and Ultramarine Herb735 at `_requirWorldLv=0`, `_overwritePrice=10`, `_canKillDrop=true`. `NormalShopSaleInfo.CanSell` requires `ItemData.IsAppearIngame` and `WorldLevel >= _requirWorldLv`. No additional world-level gate is present in these entries. This establishes an alternative to harvesting the herbs in their namesake hostile environments, but a complete opening route still requires obtaining the crop currency and reaching the merchant. The Spring hierarchy itself is not proof of every traversal segment's environmental selection.

`OcItemCashier.CalcCost` applies `CalcReducedPrice(basePrice)*quantity`; `OcPlMaster.CalcReducedPrice(int)` takes the ceiling after the Commercial Mind multiplier. With no discount, one herb costs10 Tomatoes or10 Pumpkins. The cashier checks spendable inventory, so locked crop stacks are excluded from payment. Other sale entries with a negative overwrite price use value-derived prices; fixed herb entries do not.

## Stock, world level, and kill drops

`OcEm_NPC_Merchant.SetShopList` calls `CreateShopList`. On the server it creates inventory entries for the entire sale list and stocks each with `NormalShopSaleInfo.StockNum`, which reads item-family `ShopStorage` or falls back to `ItemData.MaxStack`. At day end it recreates and synchronizes stock. Therefore catalog entries are not one-off unrenewable loot; the precise per-item stock count was not separately extracted here.

`NormalShopSaleInfo.CanSell` is a raw world-level comparison, not recipe/workbench-age filtering. JSON preserves the raw values for each equipment item. `OcMoneyCashier.CalcCost` applies fixed price or item price, the Commercial Mind discount, then the serialized cost-rate multiplier; these ordinary equipment catalogs serialize rate1.

On merchant death, code selects one random eligible item from current stock whose sale entry permits kill drops, unless the merchant is a battle friend. This is not a guarantee of a particular desired item. LimitedDungeon entries set `_canKillDrop=false`, so their purchase gate must not be replaced with an assumed merchant-kill shortcut.

## Remaining14 LimitedDungeon equipment routes

IDs1727,1843,1906,2521–2524,5509–5512,6380,6455,6532 are each listed in one of the ten LimitedDungeon catalogs. Every one requires raw world level9, costs base50 **Boss Challenge Medals (Red)6665**, and forbids kill drops. The weight asset assigns weight10 to each catalog, a total100. `SoRandShopWeightList.GetRandomShopList(seed)` draws using the seed; `OcSpawnerOverwriteRandomShopListProcess` uses the spawned enemy GUID. Thus configured catalog selection is random/seeded when that process is used; these lists are not ten simultaneously guaranteed inventories.

No actual spawner referencing this random weight asset was decoded in the scanned loose scenes/resources/sharedassets. Its addressable prefab/dungeon/event instantiation still needs tracing; this is not evidence that it is absent in gameplay. These14 remain catalog-plus-selection evidence, not completed access proofs.

## Reproduction and limitations

Scratch scripts and raw reads are in `artifacts/remaining-merchants/` at workspace root; `build_report.py` generates JSON. Active ancestry comes from `artifacts/remaining-spawns/active.json`. The scan covered705 loose level files, all sharedassets files, and resources.assets. It retains14 errors from the level/sharedassets pass (12 level10 spawners failed strict byte checks; two objects have missing MonoScripts), plus one missing MonoScript in resources. Failed objects were not used as evidence. Synthetic generated MonoBehaviour header pointers are not relied upon; active flags use ordinary object-head/ancestor reads.

No game files, mod logic, saves, or runtime process were modified. Safe traversal, crop/medal acquisition, other dynamic gating, and live verification remain separate dependencies. Do not turn53 placed item routes into53 unconditional opening items.

