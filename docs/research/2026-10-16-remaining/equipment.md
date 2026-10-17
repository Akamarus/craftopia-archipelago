# Remaining equipment acquisition audit — 2026-10-16

This targets the previous **56 loot-only and seven unsupported-category rows**. It finds **one quest reward and four temporary skill weapons**. The seven unsupported gun accessories have exact **blueprint-gated recipes**, but their blueprint acquisition remains unresolved. **51 ordinary loot-only rows still have no demonstrated source after the targeted checks.** These are bounded negative findings, not declarations that the items can never be obtained.

Research only: no mod, APWorld, game, save, workbook or original review queue was changed. No item rank was converted into an AP tier. `equipment.json` contains every exact ID, the source-object manifest, full positive quest/projectile evidence and explicit remaining uncertainty.

## Newly established source: Demon Sword Sakura-fubuki 1882

Installed `sharedassets6.assets` object **42199**, `so--SoQuestData_s_5_10`, is group **1005**, quest **90**, UID **1005090**. Its `_itemInfos[0]` managed-reference chain resolves to item **1882**, quantity **1**, item level **0**. The quest is not expired, has player-level condition0, references NPC enum382, and requires delivery of **Cherry Blossom Petal3331 ×99**. It requires predecessor object **42198**, group1005 quest89; both complete records are preserved in JSON. The predecessor has player-level condition **33**, is not expired, has no earlier clear-quest pointer, and contains delivery requirements **Iron Ore700 ×999, Sand752 ×999, Ceramic3470 ×999**. These are substantial prerequisite quantities, not just a99-petal acquisition cost.

`Oc.Quest.SoQuestData.GetReward()` passes each reward getter and quantity to `TryTakeItem(..., ItemTakeTrigger.Reward)`. This is an explicit award path, not a same-number dialogue token or ingredient match. NPC placement, access to the predecessor and acquisition of99 petals still require route review before assigning an AP gate or earliest acquisition age.

## Four temporary skill weapons, not a blank loot-source problem

The `IsSkillCreateWeapon(ItemData)` extension and its backing array explicitly classify IDs3300–3303 in `SkillCreateWeapon`. Installed `sharedassets6.assets` `OcCharaData` object43400 links the Unlimited Blade projectile prefabs to the following exact `OcShell` objects. Each shell has `_HitAttach_Use=1`, `_HitAttach_CanItemTake_FromAttachedEm=1`, a real `OcDropItem` component and the indicated `ItemData` pointer.

| Item ID / captured name | Shell path ID | Item asset path ID | First skill level with positive selection |
|---|---:|---:|---:|
| 3300 Fake Inscripted Sword | 44309 | sharedassets0:84966 | 4 |
| 3301 Twitsted Sword | 44305 | sharedassets0:84967 | 3 |
| 3302 Sword of Repulsion | 44304 | sharedassets0:84968 | 2 |
| 3303 Nameless Sword | 44315 | sharedassets0:84969 | 1 |

The captured implemented skill is **917, Unlimited Fake Blade**, Magic/Tier4, max level6. This is its vanilla skill metadata, **not an AP equipment tier**. `OcPlSkillCtrl.SummonSowrd.move()` selects projectileD at level1, addsC at level2, B at level3 and A at level4, with the exact nested random branches in the decompiled method. `OcShell.CheckAttach` sets the corresponding item with quantity1 and activates its pickup component on qualifying collisions, subject to its owner/collider checks. Thus the exact prefab-to-item path is established rather than inferred from the skill's name.

Their handling is materially different from persistent loot. `OcItemUI_InventoryMng.TryTakeItem` redirects skill-created swords to `TryEquipSkillItem`; unequipping clears the item. The inventory floor-drop handler also clears skill-created weapons instead of making an ordinary dropped inventory item. Review them as temporary skill equipment, not ordinary permanent sword rewards. Learning the skill and successful in-game pickup have not been exercised in this read-only audit.

## Seven gun accessories: conditional recipes identified

`sharedassets0.assets` object91163 `So--ItemDataList.bluePrintMap` maps **6801–6807** respectively to **5681–5687**. All seven equipment items are status2 and all seven blueprint items are status1. The capture marks each recipe `IsBlueprintRequired=true` and none was present in its `craft_bench_map`.

| Equipment ID / name | Blueprint ID | Workbench family | Displayed recipe age | Ingredients per output1 |
|---|---:|---:|---:|---|
| 5681 Expansion Magazine II | 6801 | 20027 | 9 | High Mithril Ingot718 ×2, Spring3469 ×1 |
| 5682 Quickloader II | 6802 | 20022 | 7 | Adamantite Ingot716 ×2, Bolt3468 ×1 |
| 5683 Custom Grip II | 6803 | 20022 | 6 | Platinum Ingot715 ×2, Bolt3468 ×1 |
| 5684 Bipod II | 6804 | 20027 | 9 | High Mithril Ingot718 ×2, Spring3469 ×1 |
| 5685 Laser Sight II | 6805 | 20022 | 8 | Mithril Ingot717 ×2, Crystal2453 ×1, Battery2100 ×1 |
| 5686 Sling Belt II | 6806 | 20022 | 8 | Mithril Ingot717 ×2, Cotton Canvas3401 ×1 |
| 5687 Resin Stock II | 6807 | 20022 | 6 | Platinum Ingot715 ×2, Log746 ×1 |

`OcItemHandler_Blueprint.TryUse` consumes the blueprint and calls `OcItemDataMng.UnlockRecipe`. `UnlockRecipe` adds the item to its workbench-family recipe list; normal material and age checks still apply. The displayed recipe ages above equal raw `workbenchLv+1` and describe this crafting path only, not earliest acquisition or AP tiers.

All seven blueprints have **zero weights in all14 general chest arrays and both weapon/material gacha arrays**. No exact blueprint match was found in the captured shop sales, the prior851 decoded enemy tables' positive quantity/rate slots, or this audit's quest, mission, fixed-chest, special-lottery and miscellaneous reward sources. Consequently the recipes are real conditional paths but do not yet establish a normal way to unlock them. Do not silently promote recipe existence into reachable equipment. AssistEquip category support is a separate design question.

## Targeted negative-source coverage

The selected asset scan decoded **1,158 objects with full byte-consumption checks**:812 from sharedassets0/6, resources and all installed level files;313 from the default-local addressable bundle;33 from miscellaneous boss/supply/chara sources. A manifest of every selected object is in JSON. Relevant coverage includes:

- **192 SoQuestData** objects and their managed-reference rewards; only1882 is a new target reward.
- **19 SoItemLotteryTeble** assets, paths91271–91289 in sharedassets0, including the3931–3952 family and6904 table. No target or6801–6807 blueprint reward entries.
- `SoRandTreasureLotteryData` sharedassets6:41595, its complete nested reward tables and linked `OcObj_RandomTreasure`43117. No target equipment or blueprint reward entries.
- **148 scene chest objects**:147 fixed lotteries and one general lottery. The defaultlocalgroup addressable bundle adds **155 prefab chest objects**:107 fixed lotteries and48 random-enchantment-scroll lotteries. No target equipment or6801–6807 blueprint entry in their fixed lists. Prefab evidence is kept separate from placement/reachability.
- Mission reward fields, `SoAlchemyData` sharedassets0:91167, `SoSoulOrbRewardData`, `SoSupplyCorpsData`, and30 `SoLimitedBossDungeon` reward records. No target output entry. Ingredient/input/message/object-pointer numeric coincidences were not counted as awards.
- All63 targets also have **zero material-gacha weights**; the previous follow-up had checked weapon gacha, general chest, enemy slots and shops.
- A targeted literal-ID search of the matching installed assembly's decompile found debug battle-test grants, equipment-stat checks and the temporary-sword logic, but no additional ordinary hard-coded grants. Debug `ItemAdd`/`addBattleTestItem` calls are not normal acquisition sources.

The51 ordinary rows all have **WorkBenchId20001**, `NO_CRAFT_FAMILY_ID`. `ItemData.IsCraftable` excludes that family. Some retain historical ingredient/age fields, which must not be mistaken for live recipes or age gates. The previous normal-status evidence remains valid; status1/2 alone does not prove an acquisition route.

## Still unresolved ordinary loot rows

| ID | Captured name | Category |
|---:|---|---|
| 1704 | Fimbulwinter | SwordTwoHand |
| 1711 | Dragon Killer | SwordTwoHand |
| 1713 | Durandal | SwordTwoHand |
| 1716 | Great Sword of the Demon | SwordTwoHand |
| 1718 | Barbarian's Halberd | SwordTwoHand |
| 1722 | Gold Crush | SwordTwoHand |
| 1723 | Sting | SwordTwoHand |
| 1724 | Mjolnir | SwordTwoHand |
| 1795 | Straw Hat | Head |
| 1803 | Meteor Axe | Sword |
| 1806 | Main Gauche | Sword |
| 1810 | Lasersaber | Sword |
| 1817 | Lumberjack's Sword | Sword |
| 1824 | Dirk | Sword |
| 1865 | Shield of Achilles | Shield |
| 1866 | Escutcheon | Shield |
| 1905 | Spear of Longinus | Lance |
| 2511 | Red Dragon Mail for Men | Body |
| 2512 | Red Dragon Mail for Women | Body |
| 2517 | Blue Dragon Mail for Men | Body |
| 2518 | Blue Dragon Mail for Women | Body |
| 2539 | Red Wizard Cloak for Men | Body |
| 2540 | Red Wizard Cloak for Women | Body |
| 2541 | Blue Wizard Cloak for Men | Body |
| 2542 | Blue Wizard Cloak for Women | Body |
| 2552 | Farmer's Overalls for Men | Body |
| 2553 | Farmer's Overalls for Women | Body |
| 5501 | Red Dragon Helm for Men | Head |
| 5502 | Red Dragon Helm for Women | Head |
| 5505 | Blue Dragon Helm for Men | Head |
| 5506 | Blue Dragon Helm for Women | Head |
| 5515 | Yellow Elemental Petasos | Head |
| 5516 | Green Classical Witch Hat | Head |
| 5518 | Blue Classical Witch Hat | Head |
| 6014 | Handaxe | Sword |
| 6016 | Bardiche | Sword |
| 6017 | Club | Sword |
| 6018 | Battle Mace | Sword |
| 6085 | Greatsword | SwordTwoHand |
| 6161 | Silver Bow | Bow |
| 6162 | Sharp Shooter | Bow |
| 6163 | Wild Rose Bow | Bow |
| 6166 | Moon Piercer | Bow |
| 6167 | Magic Bow Failnaught | Bow |
| 6169 | Longbow | Bow |
| 6175 | Vermilion Bow | Bow |
| 6267 | Partisan | Lance |
| 6375 | Staff of Divine Punishment | MagicStaff |
| 6414 | Sun Staff | MagicStaff |
| 6435 | Fugudoku | Katana |
| 6468 | Sohaya no Tsuruki | Katana |

## Reproduction and limits

Inputs and assembly hash are inherited from the verified `2026-10-16-followup/equipment.json`: installed `AD__Overcraft.dll` SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`. Scratch scripts and complete selected extracts are in workspace `artifacts/remaining-equipment`; the durable report contains positive full records and the scanned-object manifest.

UnityPy1.25.3 plus TypeTreeGeneratorAPI0.0.10 used the installed managed DLLs. Two generator quirks were corrected in the analysis process: align `m_Enabled`, and collapse duplicate inherited `references` registry children into the final child at the end of a type. Without the latter, fixed-chest inheritance is decoded at the wrong offset. Embedded managed-reference classes omit the synthetic MonoBehaviour header. Every included record passed `check_read=True`.

The scan records three null-script MonoBehaviours (resources:421193, level445:6651, level672:16671) and eight unresolved external-script references in the addressable bundle. These are identification failures, not successfully decoded negative evidence. The defaultlocalgroup bundle was inspected; this is not an exhaustive decode of all subworld, enemy, placement and HLOD bundles. Likewise source existence does not settle world placement, prerequisite access, or gameplay viability. No global unobtainability claim, final balancing assignment or runtime pickup/quest test is made.
