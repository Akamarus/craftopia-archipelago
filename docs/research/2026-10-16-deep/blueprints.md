# Seven gun accessory blueprints — deeper acquisition audit, 2026-10-16

**All seven previously unresolved blueprints now have exact installed fixed-chest sources.** The broad equipment scan found the sources in the subworld bundle; this audit independently resolved their managed-reference lottery, active GameObject ancestry and addressable prefab container. This supersedes the previous blueprint-source negative finding. It does not yet establish earliest access to each cave or an AP tier.

| Equipment | Blueprint | Cave prefab suffix | Chest component path ID | Base quantity |
|---|---:|---:|---:|---:|
| 5681 Expansion Magazine II | 6801 | 025 | -7279367487757318234 | 1 |
| 5682 Quickloader II | 6802 | 021 | -671767106356713556 | 1 |
| 5683 Custom Grip II | 6803 | 019 | 2646956119785693737 | 1 |
| 5684 Bipod II | 6804 | 024 | -3610398827600407785 | 1 |
| 5685 Laser Sight II | 6805 | 022 | 3533987161280259921 | 1 |
| 5686 Sling Belt II | 6806 | 023 | 7387218673017324422 | 1 |
| 5687 Resin Stock II | 6807 | 017 | -6161614593865318177 | 1 |

Each chest is an `OcGimmick_TreasureBox` in `subworld_assets_all_a1d7ab9ed17d58a694a149fbd4ecc0b8.bundle`. All seven have `_ForceSpawn=1`, enabled components, active GameObjects and active ancestors up through the subworld prefab. Each `_treasureLottery.rid` resolves to `OcTreasureLottery_Fix` with exactly one target `ItemID` and `Num=1`. Their own `_pickupConditions`, `_RequirOpenGimmicks` are empty and `_NeedsOpenAllDoor=0`; this does not remove spatial or dungeon-route barriers. The Custom Grip II chest belongs to the `oc_pf_dungeon--CaveBoss08_DryadFlower` ancestor. Full names, transforms, object IDs and records are preserved in JSON. Each chest also appears explicitly in its root `OcSubWorld._gimmickList`; this checks that it belongs to the subworld gameplay registration, beyond visual ancestry.

`OcGimmick_TreasureBox.Start` forces spawn when configured; `doComplete` calls the selected lottery. `OcTreasureLottery_Fix.Lottery` constructs the specified item and drops the configured count (with a possible Treasure Hunter extra). This is a concrete award path, not an ingredient, dialogue token or coincidental numeric reference. `OcItemHandler_Blueprint.TryUse` then consumes the blueprint and calls `UnlockRecipe`, exposing the already-established accessory recipe. Previous material/bench/age requirements remain in the JSON and are crafting constraints, not claims of earliest access.

The direct unlock-call audit found blueprint consumption, soul-orb reward/restoration, boss skill unlocks and debug unlock-all. It found no separate ordinary accessory unlock bypass. The blueprint UI merely derives source hints from enemy drops, chest probability and localization; it does not grant recipes.

For historical context, the publisher's [September 2021 patch notes](https://store.steampowered.com/news/posts/?appids=1307550&enddate=1633323907&feed=steam_community_announcements) described rare drops of these seven blueprints from dungeon bosses at level 30 or higher. That legacy-era statement is not used to establish the current installed route; the fixed-chest records are stronger current-build evidence.

Research only. No mod, save, game, workbook, APWorld or original queue changed. No runtime collection was attempted. Entry loading is now verified as detailed below. Exact player traversal and full prerequisite accessibility remain unverified. Reproduction scripts and full intermediate extracts are in `artifacts/deep-blueprints`; source scan came from `artifacts/deep-equipment/sources.json` with full byte-consumption checks.

## Verified scene entrance and load bindings

All seven caves also have active entrance elevator components in installed `level11`. The elevator `_subWorldID` indexes `sharedassets6.assets:42303` (`OcSubWorldSoList`); the selected `OcSubWorldSo.SubWorldPrefab.m_AssetGUID` resolves through the installed addressable catalog to the exact cave prefab containing the chest. `sharedassets6:42416` (`OcSubWorldMng`) points to that same list. These checks link the reward to scene entry objects, beyond merely finding a dormant prefab.

| Blueprint | Subworld ID | level11 elevator path ID | SubWorldSo path ID | Saved restart position (x, y, z) |
|---|---:|---:|---:|---|
| 6801 | 25 | 72284 | 42328 | 1116.025, 56.288, 2899.690 |
| 6802 | 21 | 72246 | 42324 | 3597.117, 151.688, -141.280 |
| 6803 | 19 | 72277 | 42322 | 3038.072, 50.244, 336.720 |
| 6804 | 24 | 72283 | 42327 | 1741.151, 43.740, 2545.324 |
| 6805 | 22 | 72245 | 42325 | 2984.622, 6.897, 1409.762 |
| 6806 | 23 | 72240 | 42326 | 1647.008, 6.290, 1793.955 |
| 6807 | 17 | 72264 | 42320 | 2013.779, 299.012, -830.944 |

The displayed positions are the explicit `OcSubWorldSo.RestartPos` used when the player enters the elevator area, not unverified latitude/longitude or summed local transforms. Elevator `OcPickupEvent.Pickup` calls `TryCreate(_subWorldID)` before starting movement; the manager's `Create_RPC` loads the selected prefab asynchronously and instantiates it. All seven elevator components and ancestor GameObjects are enabled/active in the serialized scene. This establishes an ordinary load/entry path; it does not prove every intervening obstacle, puzzle, regional gate or minimum acquisition age. Readable cave/region labels still require localization/world-map binding.
