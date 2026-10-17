# Material recipes and production gates — 2026-10-16

## Evidence and boundaries

Primary capture: workspace-relative evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json; keys items, craft_bench_map, age_new_items, loaded_stations. Assembly mod/refs/AD__Overcraft.dll, SHA-256 88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea. Methods cited below were directly decompiled read-only with ILSpy. No implementation, game or save changes.

Both supplied Google JSON attachments truncate inside their processed-material registry. Original ab5920af-4426-472c-afd2-6c8061f2563b/Pasted text.txt has 18 complete processed rows ending Titanium Ingot; revised c00629e6-11a4-4a5a-8bc1-da5d5b0628be/Pasted text.txt has 16 ending Steel Ingot. This audits all supplied rows plus crafted tower materials and potion claims; it does not invent missing registry entries. Companion recipes.json preserves 58 individual claims/candidates and 68 recursively reached item records, exact material IDs/counts, map membership, statuses, blueprint indicators and age-list indices.

## What the fields mean

- Oc.Item.UI.OcItemUI_CraftMng.CanCraft(ItemData,int) rejects WorkBenchLv > OcWorldManager.WorldLevel, then checks quantities. ItemData.DisplayWorkBenchLv displays WorkBenchLv+1 with localized age name. This is an **age threshold, not station upgrade level**.
- ItemData.CountPerCraft returns 1 when playerCraftCount <= 0, otherwise that count. Ingredients are per output batch. Water and Steel Pillar produce ten each; raw ingredient totals must not become per-unit costs.
- OcItemDataMng.SetupCraftBenchMap populates appearing IsCraftable items. GetRecipeList additionally filters age, appearance and HasCraftingCost; blueprint handling can remove/add map entries. The enumerable overload also checks empty-island restrictions and admits blueprint entries before age, but CanCraft still checks age. Advanced station maps inherit earlier-family recipes: Workbench 20011 inherits player family 20002.
- IsCraftable excludes NO_CRAFT_FAMILY_ID 20001 and 0. IsAppearIngameWithoutAdmin accepts enabled status 1 or 2; IsAppearIngame permits status 3/4 only with corresponding debug flags. Status 0/5 rows cannot be called active simply because they have names/costs.
- Missing map membership means **not an active listed recipe in this capture**, not impossible to obtain by drops, chests, gathering, rewards, shops, generators or existing stock. Raw workbenchLv on a no-craft item is not its acquisition age.
- age_new_items is not exhaustive. Many ordinary materials are absent despite positive recipe-map evidence. Every positive recipe below has IsBlueprintRequired=false in this capture; that does not prove material availability.

## Every supplied processed-material row

Counts below are per batch. Exact records and original/revised claimed values are in recipes.json.

| Claimed material | Captured correction |
|---|---|
| Old Rags | **Old Rag 3400**: 2 Straw2454, Workbench, displayed Age1. Revised quantity correct; original 3 wrong. |
| Small Placeable Torches | **Small Placeable Torch5123**: Log746×1 + Old Rag3400×1, player family20002, Age1. Neither Coal nor Charcoal. |
| Paper | **2457**: Straw2454×2 + Water811×1, Workbench, Age3. No Wood; claimed Age2 wrong. |
| Bolt | **3468**: Iron Ingot712×1 + Copper Ingot711×1, Workbench20011, Age3. Not Blacksmith/Age2. |
| Spring | **3469**: same two ingots and Workbench/Age3 as Bolt. |
| Cogwheel | **2451**: Iron Ingot712×2, Workbench, Age3. Original quantity correct; revised1 wrong. |
| Gears | No exact localized-name match. Do not invent a second recipe or silently alias Cogwheel. |
| Ceramic | **3470**: Clay3450×1 + Charcoal749×2, Stone Furnace, Age3. Both claimed recipes wrong. |
| Grease | **2458**: Animal Flesh2050×2, Aging Facility20206, Age3. Neither Cooking Hearth nor Animal Fat. |
| Bioethanol | **2452**: Wheat2055×2, Aging Facility, Age3. Neither 10 nor5 Wheat. |
| Battery | **2100**: no item recipe ingredients, no recipe map, no-craft20001. Claimed Monster Prism recipe unsupported; generator route needs separate evidence. |
| High-Capacity Battery | **High Capacity Battery3463**: Gold Ingot714×2 + Battery2100×5, Workbench, Age5. No Silicon. |
| Charcoal | **749**: Log746×2, Stone Furnace, Age2. Neither supplied quantity nor Age1 matches. |
| Elemental Dust | **2460**: no recipe ingredients/map; no-craft20001. Neither claimed Magi Fragment recipe supported. |
| Iron Ingot | **712**: Iron Ore700×3, no Charcoal, Stone Furnace, Age3. Both claimed Age2 recipes wrong. |
| Steel Ingot | No exact name match. Heavy Steel Ingot3485 is different, status5, not a safe alias. |
| Gold Ingot (original only) | **714**: Gold Ore702×3, Stone Furnace, Age5; supplied2 ore/Age3 wrong. |
| Titanium Ingot (original only) | No exact name match; asserted Skilled Blacksmith recipe unverified. |

## Other tower materials

| Exact item | Batch recipe | Family / displayed recipe age / output |
|---|---|---|
| Bioplastic2459 | Bioethanol2452×2 + Grease2458×2 | 20206 /5 /1 |
| Linen Wool3402 | Old Rag3400×5 + Sheep Wool758×5 | 20011 /5 /1 |
| Engine Parts3464 | Iron Ingot712×5 + Battery2100×5 + Ceramic3470×5 | 20013 /5 /1 |
| Fuel Tank3465 | Iron Ingot712×5 + Rubber2455×5 + Cotton Canvas3401×5 | 20013 /4 /1 |
| Armor Plate3467 | Iron Ingot712×99 | 20013 /6 /1 |
| Steel Pillar5305 | Iron Ingot712×10 + Bolt3468×10 | 20020 /4 /10 |
| Sanctuary Flag5145 | Talisman Flag5141×9 + Silver Ingot713×1 + Elemental Dust2460×1 | 20020 /6 /1 |
| Platinum Ingot715 | Platinum Ore703×3 | 20018 /6 /1 |
| Adamantite Ingot716 | Adamantite Ore704×5 | 20018 /7 /1 |
| Mithril Ingot717 | Mithril Ore705×5 | 20018 /8 /1 |
| Holy Water5565 | Water811×5 | 20012 /6 /1 |

These rows are listed in their respective recipe maps; Mithril is status2, the others status1. Do not replace exact Mithril spelling with a different material ID. Holy Water has duplicate names: status0 row813 is not a safe substitute for active Holy Water5565. Both are retained separately in JSON. Revised Google categorizes Holy Water solely as merchant/chest material; this capture positively establishes a listed five-Water recipe, without disproving alternate obtaining routes. Its recipe age is6, not the asserted acquisition age5.

Unresolved exact names from tower claims: Circuit Board, Fireproof Polymer, Glass Pane, Heat-Resistant Polymer, Solar Cell, Refined Rubber, Advanced Alloy, Chemical Catalyst, Cryo Fluid. No exact name does not prove no related item; aliases require evidence.

## Resistance potions

| Item | Captured ingredients | Raw level / displayed age | Status / map |
|---|---|---|---|
| Heat Resistance Potion5546 | Yellow Herb740×1 + Snow Mint3445×1 + Water811×1 |4 /5|1 /20012|
| Hell Resistance Potion5547 | Yellow Herb740×1 + Hell Pepper3360×1 + Water811×1 |4 /5|3 /absent|
| Freeze Resistance Potion5548 | Yellow Herb740×1 + Hell Pepper3360×1 + Water811×1 |4 /5|1 /20012|

Advanced Freeze Resistance Potion has no exact localized-name match. Row5549 is Japanese placeholder 凍結レジストポーション（仮）, status0, no ingredients/map; it cannot be promoted to the claimed advanced recipe. All four supplied environmental recipes are contradicted or unsupported: no Ice Crystals/Fire Organ/Heated Minerals recipe here, and no Elemental Dust in these potions. Heat/Freeze are Age5, not4. Stored Hell recipe has Age5 but is not actively listed. Water811 is **not** filled Bucket839: one filled bucket produces ten Water at Cooking Pot20015, Age2.

## Recursive dependencies and current AP enforcement

Code references: mod/CraftopiaArchipelago/ProductionEnforcement.cs:Groups, ResolveStationObject, CanStart; ProductionHooks.cs:Targets, Group, VoidPrefix, ConnectorCanCraft; Permissions.cs:IsProductionUnlocked.

1. Hand crafting (_workBench=null) is opening. Workbench item200/family20011 and Blacksmith272/family20017 resolve opening. Station construction and station operation are distinct actions. Production permissions never substitute for vanilla ingredients/age.
2. Loaded Stone Furnace356 is Oc.OcBldg_CraftingBench family20018 and requires **Production: smelting**. Bolt/Spring/Cogwheel operations are opening but normal ingot dependencies require smelting. Copper Ingot711 needs3 Copper Ore699; Iron712 needs3 Iron Ore700. Each Bolt/Spring consumes3 of each ore via this route; Cogwheel consumes6 Iron Ore.
3. Loaded Cooking Pot248 is generic bench family20015 and requires **Production: cooking**. Paper's normal Water route consequently needs cooking. Water's ten-output batch consumes a filled Bucket839; filling is an obtaining route not established by item recipes.
4. Loaded Aging Facility416 is generic bench family20206 and requires **Production: equipment_care**, not cooking/alchemy. Bioethanol/Grease need that permission. Facility itself is Age3, costing Iron Ingot712×5 + Stone698×5 + Bone695×5, so normal construction adds smelting/15 Iron Ore. Bioplastic consumes4 Wheat +4 Animal Flesh recursively before station costs.
5. Ceramic recursively costs1 Clay +4 Log through2 Charcoal. Engine Parts therefore require15 Iron Ore +5 Battery +5 Clay +20 Log per output through normal recipes, before station costs and permissions. Battery remains an unresolved obtaining dependency.
6. High Capacity Battery requires6 Gold Ore through smelting plus5 Battery; its own Workbench action is opening. Armor Plate requires297 Iron Ore through99 ingots. Linen Wool adds10 Straw via5 Old Rag.
7. Steel Pillar produces **ten** per batch; recursion gives20 Iron Ingot +10 Copper Ingot =60 Iron Ore +30 Copper Ore per batch. Do not charge the full batch cost for each pillar.
8. Potion Brewing Stand212/family20012 costs5 Copper Ingot +5 Glass +1 each Red/Blue/Yellow Herb at Workbench, Age2. Its recipes then require herbs and Water, whose normal crafting route implies cooking. Family20012 is **absent** from ResolveStationObject's generic-bench switch. Machine Factory209/family20013 and Furniture Workbench206/family20020 are also absent. The existing alchemy permission maps exact class Oc.OcBldg_ItemGenerator_Alchemy, not arbitrary potion-like names.
9. **If** these stations are generic OcBldg_CraftingBench, bound CraftButtonExecute will fail closed before cost for unknown identity; receiving every production unlock cannot fix unknown mapping. Their live classes are not in this capture's loaded_stations, so this is a code-grounded conditional, not a tested gameplay observation. Potions, Engine Parts/Fuel Tank/Armor Plate and Steel Pillar/Sanctuary Flag are affected candidates.
10. Automation.Update has its own automation gate; crafting connector dispatch additionally resolves the connected station. Opening recipe status cannot erase automation's separate permission. Conversely, loot alternatives need not inherit manufacturing permissions.
11. CanStart permits actions unbound; bound opening/excluded_power are allowed; other recognized groups require corresponding permission; null identity and errors deny. Exact generic Oc.OcBldg_ItemGenerator is not mapped. Its hooked StartProcessReq therefore may block a generic generator. Direct decomp of OcZone_GeneratorRunning.StateLoop.move shows it calls _ItemGenerator.StartProcessReq(); OcBldg_ItemGenerator.OnSetItem reads serialized generateItemID. Generator(Small)2000 itself is Age3, costing1 Iron Ingot +1 Cogwheel +5 Log. Actual generator prefab class/output/duration needs capture before asserting Battery production and AP behavior.

## Unresolved evidence before verified progression

- Capture actual class/item/family for Potion Brewing Stand, Machine Factory, Furniture Workbench and generator. Resolve unknown dispatch deliberately; this report makes no fix.
- Read serialized generator output, cycle duration/mount behavior and sustainable Battery routes. Do not assume Monster Prism is a consumed recipe ingredient.
- Establish actual obtaining sources, tools/combat and renewability for Elemental Dust, Snow Mint, Hell Pepper, Yellow Herb, filled buckets, ores, Clay, Wheat, Animal Flesh and Sheep Wool. No item recipe means source work remains, not impossible access.
- Prove ordinary access to later status2 items; data presence alone does not prove released seamless-world progression.
- Verify potion buff types/duration/environmental levels separately. A recipe cannot establish safe geography or whether resistance is mandatory.
- Model source alternatives explicitly: loot/purchased ingots can bypass furnace operation; craft routes need it. Recursion needs age, station construction, operation permission, blueprint checks and a least-fixed-point accessibility model. Include batch output and stage consumption; this extraction does not prove stock sufficiency or seed beatability.
