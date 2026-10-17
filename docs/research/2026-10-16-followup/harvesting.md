# Age-material harvesting audit — installed build, 2026-10-16

The installed source definitions do **not** require upgraded pickaxes or axes for the nine sets of age materials. Stone Pickaxe [56] and Stone Axe [68], both attack rank 0, pass the actual rank check on every identified age ore/wood source. Later materials having higher item ranks is not a harvesting requirement. This is a source-configuration and code finding, not a completed gameplay trial or proof of geographic access.

The user's four-material pattern is almost right, but **Suspicious Pumpkin [6595] has an axe-target static source**, not an ordinary pickup spawner in the inspected definitions. The remaining 17 plant/doll materials have ordinary non-enemy item spawners. No tier gate should be added merely from their region or age.

## Source chain and actual comparison

- Assembly `mod/refs/AD__Overcraft.dll` matches installed `Managed/AD__Overcraft.dll`, SHA256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.
- `Oc.OcStaticObj.setup` calls `Health.SetRarity(_SoEnemyData.Rarity)`. `Oc.OcHealth.SetRarity` assigns `_StaticObj_Rank` directly. This is the relevant resource-side rank, not the rank of the harvested item.
- `Oc.OcDamage.solveDmgMsg` handles colliders whose `_CheckDamageType` includes ToolAxe (4) or ToolPickle (8). Matching attack attributes get weakness multiplier 1. An ordinary tool attack bounces only when `objectRank > 0 && objectRank - (attackRank + PlAddWpRank) > 1`. With the normal debug adjustment 0, attack rank must be at least objectRank minus 1 to avoid this bounce. Equality passes.
- The unarmed branch differs: it bounces when objectRank exceeds attackRank and objectRank is greater than 1. Do not generalize the tool formula to all attacks.
- `Oc.Item.OcItem.Rank` returns ItemData.Rank; equipment attack setup copies equipped item Rank into the damage message. Current Stone tool assets have rank0; Copper rank1; Iron rank3; Silver rank4; Gold rank6; Platinum rank7; Adamantite rank8; Mithril rank9; High Mithril rank10; Orichalcum rank11. These ranks are not consecutive age indices. `So--OldItem_*` records were excluded.
- Bounce is not implemented here as “no damage/no drops”: it changes weakness multiplier to 0.1 and sets bounce feedback. Non-tool attacks receive fixed attacker attack5; mismatched attacks can have multiplier0.2. `OcHealth.solveDmgMsg` consumes the fixed attack override; `OcItemDropper.checkDamagedDropItem` checks accumulated HP loss against the source's damage threshold, with no independent tool-rank requirement. Consequently these methods do not justify treating even a higher resource's bounce threshold as absolute impossibility. Alternate attacks and efficiency require separate evaluation.

## All age ore and wood source definitions

Each listed ore has `_StaticObjType=2` (Stone), collider `_CheckDamageType=8` (pickaxe); each wood has type1 (Tree), collider4 (axe). The companion JSON retains all matching source component IDs, hierarchy names, full source drop records and collider settings. All pass rank0 tools.

| Region family | Mine item | Chop item | Source rarity | SoEnemyData IDs | Source asset objects |
|---|---|---|---:|---|---|
| Plateau | Plateau Moonstone6576 | Ironwood6577 | 0 | 597 / 598 | sharedassets13:218 /219 |
| Hills | Emerald of Hills6581 | Ancient Wood6582 | 1 | 605 /606 | sharedassets13:220 /221 |
| Meadow | Alexandrite of Meadow6586 | Petrified Wood6587 | 1 | 610 /611 | sharedassets13:222 /223 |
| Archipelago | Morganite of Archipelago6591 | Bamboo6592 | 1 | 615 /616 | sharedassets13:224 / resources:421194 |
| Valley | Topaz of Valley6596 | Mushroom Wood6597 | 1 | 620 /621 | sharedassets13:225 /226 |
| Iceberg | Aquamarine of Iceberg6601 | Frozen Wood6602 | 1 | 625 /626 | sharedassets13:227 /228 |
| Desert | Rubellite6606 | Baobab6607 | 1 | 630 /631 | sharedassets13:229 /230 |
| Wetlands | Rose Quartz6611 | Mushroom Wood6612 | 1 | 635 /636 | sharedassets13:231 /232 |
| Volcano | Garnet6616 | Lava Wood6617 | 1 | 640 /641 | sharedassets13:233 /234 |

These sources have damageThresholdForDropItem50 and d00_dropRate1. Exact health and quantity fields are in JSON. Do not merge the two distinct Mushroom Wood IDs. Source IDs are the SoEnemyData IDs; they are not item IDs.

## Pickup and pumpkin details

Non-enemy OcSpawner definitions directly reference the following current ItemData records: Snow Mint3445; Clover6575; Plateau Ancient Doll Fragment6578; Mandragora6580; Hills Ancient Doll Fragment6583; Carnivorous Plant6585; Meadow Ancient Doll Fragment6588; Thunder Flower6590; Archipelago Ancient Doll Fragment6593; Valley Ancient Doll Fragment6598; Iceberg Ancient Doll Fragment6603; Man-Eating Cactus6605; Desert Ancient Doll Fragment6608; Haunted Mushroom6610; Wetlands Ancient Doll Fragment6613; Fire Grass6615; Volcano Ancient Doll Fragment6618. Each saved record preserves isEnemy, spawn conditions, counts and intervals; these sources require no mining/chopping tool check in their configuration. Collection distance, live world activation and route availability are distinct questions.

Suspicious Pumpkin6595 instead links to `level13:18691`, hierarchy `StaticObjPlacementMng/PrefabTemplate/oc_pf_StaticObj--Autumn_Pumpkin(Clone)/GameObject`, type Tree1. Its damage collider `level13:21783` has ToolAxe4. It references `sharedassets13:185`, SoEnemyData97, rarity1, HP50, damage threshold25, primary drop6595 quantity5/rate1. Stone Axe passes the rank comparison. This is a verified destruction/harvesting source; it does not exclude a separate pickup route elsewhere. The workbook should not label all four regional categories identically without this exception.

## Provenance, scope and reproduction

The companion harvesting.json includes SHA256 hashes for level13, sharedassets13.assets, resources.assets and sharedassets0.assets, the 36 unique captured age-cost item IDs, all joined source definitions, and 20 current tool records. Names/age requirements are joined by exact item ID to capture `evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json`. These are the current installed files under `G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data`.

UnityPy1.25.3 plus TypeTreeGeneratorAPI0.0.10 generated trees from the installed Managed directory, Unity2022.3.62f3, following the preceding environment.md recipe. Full level13 and selected source reads completed with byte-consumption checks and zero decode errors. The synthetic generated MonoBehaviour script header is not reliable and is excluded from evidence; game-object references were obtained by independently parsing the actual built-in MonoBehaviour header. References below the header were resolved against level13's external file table: fileID2=resources.assets,3=sharedassets13.assets,4=sharedassets0.assets. Collider components were joined through the actual Transform parent chain to each source object, not by nearby names.

Level13's `StaticObjPlacementMng/PrefabTemplate` contains templates. Their counts in JSON are template counts, **not numbers of placed resources**. The inspected ordinary OcSpawner records likewise demonstrate source configuration, not a fully reconstructed world spawn/route graph. Other scene extraction produced 53 decode errors in unrelated candidate components; no claim of an exhaustive all-game alternative-source inventory is made. The decisive level13/source records are complete.

Reproduction scripts and decompilations are retained in workspace `artifacts/harvesting-research/` (`extract.py`, `extract-rest.py`, `extract-resources.py`, `extract13.py`, `build.py`; `all.cs` plus individual types). These read game files and write only research artifacts. No mod, game asset, save, installed plugin, generated seed or AP logic was changed. No runtime gathering was performed. A gameplay check with Stone tools on a late-region ore/wood would validate the code/asset conclusion operationally, but lack of such a test does not make the threshold unknown.
