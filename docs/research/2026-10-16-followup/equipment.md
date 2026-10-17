# Equipment source audit — 2026-10-16 follow-up

All **429 review rows** now have an evidence-backed status, exact captured item record, installed asset identity, and explicit shop/chest/gacha results in `equipment.json`. This is research only. It does not assign AP tiers or change the mod, game, save, or original review queue.

| Review group | Rows | Positive source evidence here | Remaining source work |
|---|---:|---:|---:|
| loot_only | 241 | 185 shop/enemy-backed items | 56 |
| unsupported_category | 19 | 12 chest-pool items | 7 |
| unavailable_metadata | 169 | 0 normally eligible sources | All169 excluded; four positive enemy slots are rejected by the normal item filter |

## Sources and validation

Runtime capture: `evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json`, `items` and `world_evidence.shop_catalog.rows`. Assembly `mod/refs/AD__Overcraft.dll`, SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.

Installed `sharedassets0.assets` object **91163**, `So--ItemDataList`, supplies 7,053 item references, fourteen `rarityNChestProbs` arrays, their `rarityChestProbSums`, and weapon-gacha weights. Each array has exactly 7,053 entries. Entries are joined by their actual item asset IDs, not names or guessed rank tiers. All selected item reads used full byte-consumption checking. All **13,299 common scalar fields** across 429 selected assets matched the runtime capture, with zero mismatches. Pool sums match exactly except pool10's approximately 0.00000153 floating-point accumulation difference, preserved in JSON. Hashes are preserved there.

The synthetic generated MonoBehaviour header contains invalid pointers, as in the earlier environmental extraction. Item identities are resolved with `parse_monobehaviour_head` and the known item-list references; synthetic header pointer values are not relied on in conclusions.

## Chest eligibility is now known

`Oc.Item.OcItemDataMng.RandomTreasureData` chooses a pool using `TreasureBoxRarityWeightArray_ItemData[islandLevel,chestRarity,poolIndex]`, adds one to the chosen index, and calls `RandomChestItemData`. `SoItemDataList.GetRankProbs` selects the corresponding serialized array. `RandomItemData` traverses those weights against `GetAll()`; it does not infer membership from the item's nominal rank at draw time. `OcTreasureLottery_General.Lottery` supplies `WorldManager.IslandLevel` and chest rarity. These are pool inputs, **not minimum acquisition age or AP equipment tiers**.

Only twelve queued items have positive weights in these general chest pools:

| Exact pair | IDs | Pool number | Weight per item |
|---|---|---:|---:|
| Iron Gauntlet / Iron Glove | 3270 / 3271 | 3 | 25 |
| Gold Gauntlet / Gold Glove | 3272 / 3273 | 6 | 20 |
| Adamantite Gauntlet / Adamantite Glove | 3274 / 3275 | 8 | 15 |
| Mithril Gauntlet / Mithril Glove | 3276 / 3277 | 9 | 10 |
| High Mithril Gauntlet / High Mithril Glove | 3278 / 3279 | 10 | 5 |
| Orichalcum Gauntlet / Orichalcum Glove | 3280 / 3281 | 11 | 2 |

Each JSON row preserves its pool total and every positive island-level/chest-rarity selection combination. Those twelve remain category-design work (`AssistEquip`) even though their acquisition source is no longer unknown. Every other queued item has zero general-chest weight; all 429 have zero weapon-gacha weight. This excludes those **specific tables**, not fixed chests, enemy drops, quests, custom lotteries, or shops. A zero-width boundary outcome in the cumulative random implementation is not treated as an intentional source route.

## Sixty-seven exact equipment shop matches

Each matched item carries the shop asset name, entry index, raw `_requirWorldLv`, `_overwritePrice`, `_canKillDrop`, captured enchantment data, and cashier type. Required-world-level values remain raw source conditions; no automatic AP tier or minimum age is assigned. A listed shop does not establish that a merchant is placed, reachable, or available in a particular world.

Examples include Zweihander1702, Night Stalker1705, Gram1843, Shield of Aegis1864, Assault Dragon Mail2521/2522, Magic Dragon Mail2523/2524, corresponding Dragon Helms5509–5512, Composite Bow6160, Trident6265, Sage's Staff6395, Muramasa6437, Chicago Typewriter6530 and Gungnir Storm6532. The full list and all overlapping catalog entries are in JSON. These 67 rows can move from blank-source research to shop-access review.

## Enemy-table join

The separate enemy audit decoded 851 enemy-data assets with byte-consumption checks. Joining its positive d00..d09 item IDs, quantities and rates adds exact positive enemy slots to **123 equipment rows**. The normal item filter rejects four of these; **119 rows** have eligible item metadata, overlapping one shop-backed row. Combined evidence covers **185 of 241 loot_only items**, leaving56. Every joined slot preserves enemy-data ID, asset name, asset path ID, quantity and raw rate; see the separate drop report for drop mechanics and spawn-family interpretation. Enemy-table membership does not alone prove that the variant spawns or is reachable.

Four normally excluded items have positive serialized enemy-table entries: **Wirbelwind I–IV, IDs1834–1837**, all status4 and not old data. However, `EnemyDrop.LotteryDropItem` rejects non-`IsAppearIngame` items. Directly verified `ItemData.IsAppearIngame` permits status4 only when `SROptions.Current.Status4CanAppear` is enabled. Consequently these slots **cannot produce the items under normal settings** and are not normal acquisition sources. JSON retains their raw slots and explicit failed eligibility checks; their apparent contradiction is resolved by the item filter. This conclusion concerns this normal drop path, not every conceivable debug or alternate route.

## What unavailable_metadata actually means

The 169 rows divide into status0=53, status3=28, status4=71, status5=8, status99=9. None has status1/2. The earlier verified `ItemData.IsAppearIngameWithoutAdmin` rule admits status1/2; status3/4 require their debug flags in the broader appearance property. These are normal appearance exclusions, not a blanket assertion that every possible explicit drop/legacy stock path is impossible. All 169 have zero weight in the audited general-chest and weapon-gacha tables and no captured exact shop entry.

Fifteen duplicate-name groups are preserved in JSON, including active names that must not be merged with inactive variants:

- Adamantite One-Handed Sword50 status1 versus55 status5; Adamantite Bow98 status1 versus103 status5.
- Excalibur894 status2 versus1728 status0 and1840 status4.
- Amon's Dagger1805 status2 versus3105 status0 and3145 status4.
- Ginnungagap1809 status2 versus3109 status0 and3149 status4.
- Sword of the Dark Knight1815 status1 versus1871 status0.
- Trident6265 status1 versus1908 status0.
- Silver Bow95 and6161 are **both status1**; the same localized name is still not evidence they share acquisition or balance.

## Practical next review

1. Set aside all169 normally excluded rows for normal balancing. Preserve the four Wirbelwind raw table entries with their verified rejection by the normal drop filter. This is review triage for audited channels, not a proof of global unobtainability.
2. Review merchant placement and access for the 67 shop-backed loot rows. The source fields are already filled; there is no need to manually re-research their existence in these catalogs.
3. Decide how to support AssistEquip for the twelve chest-backed gauntlet/glove rows.
4. The enemy-drop join is complete: 185 loot rows now have sources and56 still need special chest/reward or other acquisition research. For joined rows, review actual enemy spawning/access rather than manually rediscovering drop-table membership.
5. Preserve Goblin Club6031's already selected experimental Sword2 treatment outside this report; no rank-based tierguess is used here.

No final AP balancing assignment is claimed. Source membership, reachability, vanilla age conditions, and AP tier choice remain separate decisions.
