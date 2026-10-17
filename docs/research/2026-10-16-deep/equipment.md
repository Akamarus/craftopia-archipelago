# Expanded equipment source audit — 2026-10-16

This extends the previous 51 ordinary unresolved equipment rows. **Three now have a complete conditional NPC-prefab reward chain; 48 still have no source in the expanded coverage.** The three remain placement-unresolved: neither the prior 5,628 loose spawners nor the merchant audit's181 bundle spawners contain their five NPC enums. They must not be promoted to normally reachable loot solely from these prefab records.

Research only. No mod, save, game, workbook, APWorld or tier change. Full exact source records, every target row, all selected-object identities and errors are in `equipment.json`.

## Three conditional item-offer rewards

The enemy addressable bundle `enemy_assets_all_97ae11cf768437a772d2bee00bb37a5b.bundle`, serialized asset `CAB-2926bf5930a7383f16e0fbc773c71178`, contains the following positive `SoQuestRewardTable` entries. Quantities are base quantity1 before the NPC's requirement/reward multiplier.

| High reward table | Table path ID | Sting1723 weight | Mjolnir1724 weight | Sharp Shooter6162 weight | Total weight |
|---|---:|---:|---:|---:|---:|
| Enchant | -8484730448205700178 | 10 | 4 | 4 | 209 |
| NaturalResource | -7287364807620482223 | — | 3.5 | 3.5 | 199 |
| BossDrop | -6957640219248157622 | — | 2 | 2 | 242 |
| Treasure | -63543000581886444 | — | 5 | 5 | 311 |
| Fish | 8957152279637195340 | — | 5 | 5 | 269 |

Each table has a real local pointer from its matching `SoQuestRequiredItemTable`. The enemy prefabs' `OcEm_NPC_ItemOffer.questRequiredItemTables[2]` points to those required tables. The corresponding prefab components and enums are:

| Prefab | Component path ID | EmType |
|---|---:|---:|
| OcEm_NPC_Quest_Enchant | -3228074180038796058 | 160 |
| OcEm_NPC_Quest_NaturalResource | -6698514605771947550 | 162 |
| OcEm_NPC_Quest_BossDrop | -6625832784316200276 | 159 |
| OcEm_NPC_Quest_Treasure | 537776720688046616 | 163 |
| OcEm_NPC_Quest_Fish | 4745721667408347726 | 161 |

The installed decompile's `OcEm_NPC_ItemOffer.RenewQuest` selects required and reward entries by their weights. Its difficulty-index2 weights first become positive at vanilla **IslandLevel6**, where high and middle have equal weights; IslandLevel7 selects high only. This is a conditional runtime input, not an AP tier or established earliest acquisition age. Quests renew on a three-day cycle and require delivery of the separately selected required item. Full required tables are preserved rather than guessing one fixed payment.

`doEventClear_FromRpc` creates `selectedRewardItem.itemID`, attaches the selected reward enchantment, and passes it with `RewardItemNum` to inventory `TryTakeItem`. The misleading method log mentions `OcEm_NPC_Event`, but the actual declaring class is **OcEm_NPC_ItemOffer**. A raw-byte reference search exposed that distinction; all six offer prefabs were then decoded with full byte-consumption checks. The generic sixth prefab links only the generic table and does not add target rewards.

**Remaining gap:** no matching enum159–163 placement was found in the audited loose/bundle `OcSpawner` sets. Other dynamic summoning, registration and runtime activation must still be traced. Prefab existence and executable award code establish a conditional source chain, not a demonstrated reachable NPC.

## Expanded coverage and seven blueprint leads

The scan successfully decoded **6,882 selected objects** across sharedassets0/6, resources, and enemy/subworld/defaultlocalgroup addressable bundles, including a supplemental event/scriptable-NPC scan. It used the prior UnityPy decoder fixes and `check_read=True`. Revisited sources are included in that total; it is not a count of exclusively new objects.

Coverage includes795 `SoEnemyData`,14 quest required tables,14 quest reward tables,7 ambassador tables,7 ambassador components,314 chest components,19 special lottery tables,134 scriptable-NPC assets,6 item-offer components, one offshore-fishing table, and4,386 pickup-to-drop components. The source manifest and exact class counts are in JSON. Nine primary identification failures are retained explicitly; additional scan errors are likewise retained.

All seven blueprints6801–6807 were found as explicit fixed chest rewards in the subworld bundle. Those exact chest objects and full decoded rows were handed to the blueprint investigation for hierarchy/placement proof; consult its report for the result. This report does not independently claim that their chests are reachable.

Numeric hits in dialogue `messageID` fields and GameObject path IDs were rejected as unrelated coincidences. No target match was found in the supplemental scriptable-NPC/event/fishing/pickup scan. Pointer-number matching alone is not treated as proof of item identity.

## Dynamic paths checked

`OcItemDataMng.GetGachaItem` uses the already-audited general treasure arrays for Christmas presents and its fallback capsule case. Weapon and material capsules use their existing respective arrays. This does not add an independent equipment pool. Treasure Hunter changes the number of rolls from the same general chest lottery, duplicates listed fixed-chest outputs, or awards hard-coded scroll5780 in the dungeon-scroll variant. It does not rescue a zero-weight target in these inspected paths.

Scratch reproduction inputs are `artifacts/deep-equipment/scan.py`, `additional.py`, `rawrefs.py`, `events.py`, `matches.py` and `report.py` in the workspace root. The original game assembly hash is inherited from the prior verified report. The scan did not decode all1,557 static-placement,745 HLOD,8,533 grass or56 terrain bundles, nor every object class. Other sources, world registration, dynamic summoning and gameplay verification remain open. No global unobtainability claim is made.
