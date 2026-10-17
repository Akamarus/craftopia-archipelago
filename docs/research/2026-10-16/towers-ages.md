# Tower and age audit — installed build, 2026-10-16

Source: `evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json` (workspace-relative), schema 5 / collector 1.5.2. Paths below are JSON property paths. Names joined by item ID to `items[].display_name`; no guessed IDs. All 10 loaded towers have three untruncated stages. Loaded-only scope does not prove there are no additional unloaded towers.

## All observed tower requirements

Stage columns retain raw indices 0–2; these correspond to the three ordered repair definitions, not the observed current stage. Source `world_evidence.towers.rows[].definition.stages[].costs.rows`.

| Tower item ID / localized name | Raw stage 0 | Raw stage 1 | Raw stage 2 |
|---|---|---|---|
| 3806 Geezah Plateau Tower | Stone [698] ×50; Sand [752] ×10; Log [746] ×50 | Old Rag [3400] ×10; Sheep Wool [758] ×10; Leather [683] ×10 | Copper Ore [699] ×20; Small Placeable Torch [5123] ×5 |
| 3807 Millewind Hill Tower | Paper [2457] ×20; Grease [2458] ×20 | Battery [2100] ×20; Bioethanol [2452] ×20 | Cogwheel [2451] ×20; Bolt [3468] ×20; Spring [3469] ×20; Ceramic [3470] ×20 |
| 3808 Yarden Meadow's Mountain Tower | Silver Ore [701] ×50; Charcoal [749] ×10 | Fang [680] ×10; Bird Feather [692] ×10; Bone [695] ×10 | Crystal [2453] ×20; Sulfur [763] ×20; Elemental Dust [2460] ×20 |
| 3809 Yarden Meadow's Ruin Tower | Silver Ingot [713] ×30; Rubber [2455] ×10 | Scrap Parts [3373] ×10; Glass [769] ×10 | Black Gunpowder [764] ×20; Electronic Circuit [3466] ×20 |
| 3810 Owatatsu Archipelago Tower | Gold Ingot [714] ×20; Linen Wool [3402] ×10 | Bright Red Herb [723] ×5; Ultramarine Herb [735] ×5 | High Capacity Battery [3463] ×20; Engine Parts [3464] ×20 |
| 3811 Brigandine Valley Tower | Bright Red Herb [723] ×10; Ultramarine Herb [735] ×10 | Engine Parts [3464] ×20; Fuel Tank [3465] ×20 | Steel Pillar [5305] ×20; Bioplastic [2459] ×20 |
| 3812 Sherbert Iceberg Tower | Platinum Ingot [715] ×50; Holy Water [5565] ×50 | Rare Leather [684] ×10; Rare Bird Feather [693] ×10; Fluffy Sheep Wool [759] ×10 | Armor Plate [3467] ×10; Sanctuary Flag [5145] ×10 |
| 3813 Unnefell Desert Tower | Adamantite Ingot [716] ×50; Special Cement [3474] ×30 | Light Weight Frame [3477] ×20; Light Weight Steel Frame [3475] ×20 | Felt Cloth [3403] ×20; Reinforced Plastic [3479] ×20 |
| 3814 Miasma Wetlands Tower | Clay [3450] ×50; Bolt [3468] ×100 | Paint [3457] ×30; Chemicals [3471] ×20 | Rare Scrap Parts [3480] ×10; High Performance Electronic Circuit [3481] ×10 |
| 3815 Inferno Volcano Tower | High Mithril Ingot [718] ×100; Hell Pepper [3360] ×50 | Crude Oil [761] ×100; Fortified Glass [771] ×50 | Special Cement [3474] ×150; Reaper's Spirit Scale [3355] ×10; Captured Anubis [1502] ×1 |

## All age transitions

The captured `ages[].worldLevel` is the FROM level. Each row pays for advancing to worldLevel+1. Displayed age number is worldLevel+1. Named age localization is not captured, so names are intentionally not invented. All nine captured transition rows have isEnabled=true.

| Raw world level transition | Displayed age transition | Gold | Required items (exact IDs) |
|---|---|---:|---|
| 0 → 1 | 1 → 2 | 0 | Clover [6575] ×1; Plateau Moonstone [6576] ×1; Ironwood [6577] ×1; Plateau Ancient Doll Fragment [6578] ×1 |
| 1 → 2 | 2 → 3 | 500 | Clover [6575] ×5; Plateau Moonstone [6576] ×20; Ironwood [6577] ×20; Plateau Ancient Doll Fragment [6578] ×5 |
| 2 → 3 | 3 → 4 | 2500 | Mandragora [6580] ×5; Emerald of Hills [6581] ×20; Ancient Wood [6582] ×20; Hills Ancient Doll Fragment [6583] ×5 |
| 3 → 4 | 4 → 5 | 20000 | Carnivorous Plant [6585] ×5; Alexandrite of Meadow [6586] ×20; Petrified Wood [6587] ×20; Meadow Ancient Doll Fragment [6588] ×5 |
| 4 → 5 | 5 → 6 | 50000 | Thunder Flower [6590] ×5; Morganite of Archipelago [6591] ×20; Bamboo [6592] ×20; Archipelago Ancient Doll Fragment [6593] ×5; Suspicious Pumpkin [6595] ×5; Topaz of Valley [6596] ×20; Mushroom Wood [6597] ×20; Valley Ancient Doll Fragment [6598] ×5 |
| 5 → 6 | 6 → 7 | 200000 | Snow Mint [3445] ×10; Aquamarine of Iceberg [6601] ×25; Frozen Wood [6602] ×25; Iceberg Ancient Doll Fragment [6603] ×10 |
| 6 → 7 | 7 → 8 | 1000000 | Man-Eating Cactus [6605] ×15; Rubellite [6606] ×30; Baobab [6607] ×30; Desert Ancient Doll Fragment [6608] ×15 |
| 7 → 8 | 8 → 9 | 2500000 | Haunted Mushroom [6610] ×20; Rose Quartz [6611] ×30; Mushroom Wood [6612] ×30; Wetlands Ancient Doll Fragment [6613] ×20 |
| 8 → 9 | 9 → 10 | 5000000 | Fire Grass [6615] ×30; Garnet [6616] ×30; Lava Wood [6617] ×30; Volcano Ancient Doll Fragment [6618] ×30 |

## Raw new-item lists, fully resolved

These are the captured age_new_items lists, not an assertion that every craftable recipe appears here. featuredItems is a separate UI showcase list, not the complete unlock set.

### World level 0, displayed age 1, 19 listed items

Stone One-Handed Sword [44]; Stone Pickaxe [56]; Stone Axe [68]; Branch Bow [92]; Wooden Shield [116]; Handmade Glider [141]; Stone Ring [152]; Stone Amulet [164]; Branch Arrow [181]; Workbench [200]; Campfire [404]; Market [500]; Sand [752]; Wooden Wall [772]; Wooden Floor [773]; Wooden Slope [774]; Chest [775]; One-Handed Torch [788]; Monster Prism [817]

### World level 1, displayed age 2, 28 listed items

Copper One-Handed Sword [45]; Copper Pickaxe [57]; Copper Axe [69]; Copper Two-Handed Sword [81]; Copper Bow [93]; Copper Shield [117]; Copper Hat [129]; Copper Ring [153]; Copper Amulet [165]; Wooden Arrow [176]; Furniture Workbench [206]; Potion Brewing Stand [212]; Cooking Pot [248]; Blacksmith [272]; Armory [275]; Wheat Field [296]; Planter [314]; Well [332]; Stone Furnace [356]; Billboard [572]; Training Dummy [602]; Copper Ingot [711]; Small Life Potion [725]; Small Mana Potion [737]; Charcoal [749]; Glass [769]; Two-Handed Torch [787]; Water [811]

### World level 2, displayed age 3, 35 listed items

Farming Hoe [31]; Iron One-Handed Sword [46]; Iron Pickaxe [58]; Iron Axe [70]; Iron Two-Handed Sword [82]; Iron Bow [94]; Flintlock Carbine [104]; Iron Shield [118]; Beast Glider [142]; Iron Ring [154]; Iron Amulet [166]; Iron Arrow [177]; Old-model Gun Bullet [188]; Livestock Farm [284]; Enchanting Table [395]; Aging Facility [416]; Refinery [428]; Refining Scrap Bench [432]; Sprinkler Robot [468]; Brainy Sprinker Robot [469]; Reaping Machine [473]; Firecracker [603]; Canon (45 degrees) [632]; Conveyor Belt [644]; Iron Ingot [712]; Black Gunpowder [764]; Steel Chest [776]; Inter-character Cloud Storage [782]; Lantern [789]; Blue Mono Lantern [791]; Green Mono Lantern [792]; Redd Mono Lantern [793]; Yellow Mono Lantern [794]; White Mono Lantern [795]; Purple Mono Lantern [796]

### World level 3, displayed age 4, 25 listed items

Silver One-Handed Sword [47]; Silver Pickaxe [59]; Silver Axe [71]; Silver Two-Handed Sword [83]; Silver Bow [95]; Flintlock Revolver [105]; Silver Shield [119]; Bard Glider [143]; Silver Ring [155]; Silver Amulet [167]; Silver Arrow [178]; Fire Arrow [182]; Machine Factory [209]; Breeding Facility [293]; Crab Pot [335]; Rotating Saws [464]; Seeds Planting Robot [470]; Excavator [476]; Item Frame [576]; Medieval Item Frame A [577]; Medieval Item Frame B [578]; Explosive Barrel [596]; Silver Ingot [713]; Medium Life Potion [726]; Medium Mana Potion [738]

### World level 4, displayed age 5, 30 listed items

Gold One-Handed Sword [48]; Gold Pickaxe [60]; Gold Axe [72]; Gold Two-Handed Sword [84]; Gold Bow [96]; Rifle I [107]; Submachine Gun I [109]; Assault Rifle I [110]; Shotgun I [114]; Gold Shield [120]; Ancient Glider [144]; Gold Ring [156]; Gold Amulet [168]; Gold Arrow [179]; Rifle Bullet [190]; Assault Rifle Bullet [191]; Shotgun Bullet [192]; SMG Bullet [193]; Improved Workbench [201]; Medium Sized Planter [315]; Agricultural Robot [471]; Vigorous Market [501]; Semi-transparent Sticker Type Tiny Item Stand [579]; Wall-mounted Billboard [580]; Punching Bag [599]; Sorting Machine [650]; Distributor [652]; Prototype Rocket [656]; Gold Ingot [714]; Cemented Carbide Chest [777]

### World level 5, displayed age 6, 24 listed items

Platinum One-Handed Sword [49]; Platinum Pickaxe [61]; Platinum Axe [73]; Platinum Two-Handed Sword [85]; Platinum Bow [97]; Platinum Shield [121]; Platinum Ring [157]; Platinum Amulet [169]; Platinum Arrow [180]; Skilled Machine Factory [210]; Skilled Blacksmith [273]; Skilled Armory [276]; Water Purification Facility [333]; Fish Farm [338]; Skilled Furnace [357]; Skilled Refinery [429]; Automated Robot [472]; High-Speed Conveyor Belt [645]; Dashboard [647]; 3-way Sorting Machine [651]; 3-way Distributor [653]; Platinum Ingot [715]; TNT [765]; Cloud Storage [781]

### World level 6, displayed age 7, 17 listed items

Adamantite One-Handed Sword [50]; Adamantite Pickaxe [62]; Adamantite Axe [74]; Adamantite Two-Handed Sword [86]; Adamantite Bow [98]; Adamantite Shield [122]; Type 2 Mechanical Wings [145]; Adamantite Ring [158]; Adamantite Amulet [170]; Adamantite Arrow [183]; Chemistry Lab Table [215]; Brainy Rotating Saws [465]; Massacre Drone [467]; Adamantite Ingot [716]; Adamantite Chest [778]; Jetpack [799]; Jet Fuel [810]

### World level 7, displayed age 8, 24 listed items

Mithril One-Handed Sword [51]; Mithril Pickaxe [63]; Mithril Axe [75]; Mithril Two-Handed Sword [87]; Mithril Bow [99]; Mithril Shield [123]; Mithril Ring [159]; Mithril Amulet [171]; Mithril Arrow [184]; Innovative Workbench [202]; Master Machine Factory [211]; Skilled Potion Brewing Stand [213]; Alchemy Cauldron [224]; Top Grade Cooking Pot [249]; Master Blacksmith [274]; Master Armory [277]; Clone Breeding Facility [294]; Master Furnace [358]; Global Market [502]; Mithril Ingot [717]; Large Life Potion [727]; Large Mana Potion [739]; Fortified Glass [771]; High Performance Jetpack [800]

### World level 8, displayed age 9, 17 listed items

High Mithril One-Handed Sword [52]; High Mithril Pickaxe [64]; High Mithril Axe [76]; High Mithril Two-Handed Sword [88]; High Mithril Bow [100]; High Mithril Shield [124]; High Mithril Ring [160]; High Mithril Amulet [172]; High Mithril Arrow [185]; Landscaping Workbench [221]; Master Refinery [430]; Raid Support Camp [482]; Monster Merging Facility [483]; Supply Group Chest [484]; Guard Machinegun Turret Lv50 [611]; High Mithril Ingot [718]; High Mithril Chest [779]

### World level 9, displayed age 10, 12 listed items

Orichalcum One-Handed Sword [53]; Orichalcum Pickaxe [65]; Orichalcum Axe [77]; Orichalcum Two-Handed Sword [89]; Orichalcum Bow [101]; Orichalcum Shield [125]; Orichalcum Ring [161]; Orichalcum Amulet [173]; Orichalcum Arrow [186]; Energy Bullet [197]; Altar of Creation [481]; Orichalcum Ingot [719]

## Assembly evidence resolving index semantics

Inspected read-only with Mono.Cecil. Repository `mod/refs/AD__Overcraft.dll` and installed `G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data/Managed/AD__Overcraft.dll` both hash to SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`, matching capture.

- `Oc.SoAgeEvoData.get_NextLevel`, IL_0000–0008: returns `worldLevel + 1`.
- `Oc.SoAgeEvoData.get_NewItems`, IL_0000–0010: calls `OcItemDataMng.GetWorldEvoNewItem(NextLevel)`.
- `Oc.Item.OcItemDataMng.SetupAgeEvoItemList`, IL_0001–0012: allocates MaxWorldLevel+1 lists. IL_0065–0073: appends each qualifying item to index `item.WorkBenchLv`.
- `Oc.Item.OcItemDataMng.GetWorldEvoNewItem(int)`, IL_0011–0019: returns the requested list index directly.
- `Oc.OcUI_AgeEvolution.RefreshAgeEvo`, IL_0000–0013: selects current transition data using current `OcWorldManager.WorldLevel`. `GetAgeEvoData(int)` indexes `OcResidentData.AgeEvoData` directly.
- `Oc.OcUI_AgeEvolution.TryEvoWorldLevel`: charges current row requirements and gold, then invokes `EvoAndCloseUI`. `EvoAndCloseUI`, IL_0048–004e: calls `AddWorldLevel(1)` in single-player path.
- `Oc.OcUI_AgeEvolution.Refresh`, IL_0032–0052: replaces displayed world-level label with `(WorldLevel+1).ToString()`. Age-name key is `100_UI/Age/Name/{WorldLevel}` at IL_0011–0022.
- `Oc.Item.ItemData.get_DisplayWorkBenchLv`: formats `WorkBenchLv+1` alongside `100_UI/Age/Name/{WorkBenchLv}` localization.
- `Oc.Item.UI.OcItemUI_CraftMng.CanCraft(ItemData,int)`, IL_0018–002b: returns false if `ItemData.WorkBenchLv > OcWorldManager.WorldLevel` outside DebugCraft. `OcItemUI_AutoCraftMng.CanCraft` has the same comparison.

Consequences: 9 transition rows naturally accompany 10 age indices 0–9. Potion Brewing Stand [212] at raw workbenchLv/list index 1 is world level 1 / displayed age 2; its actual craft requires other recipe inputs and station access too. workbenchLv is an age gate, not an independent station-upgrade level. The capture has no age-name translations; exact English names remain unresolved despite numbering being resolved.

Collector provenance: `mod/DiagnosticCollector/ProgressionCapture.cs`, lines 57–61 copies `OcItemDataMng.ageEvoNewItemLists` in order; lines 67–84 copies `_ageEvoData`, `worldLevel`, `requireGold`, `isEnabled`, `requireItems`, and `featuredItems`. It does not shift the indices.

## Google revised report: tower claim-by-claim audit

The supplied Google attachment is a truncated JSON fragment ending inside the recipe registry. These findings cover every supplied tower and age entry, not nonexistent omitted text. For matching, obvious singular/plural labels (Old Rags/Old Rag, Wool/Sheep Wool, etc.) are treated as aliases; item IDs above are authoritative.

| Google tower | Stage 1 / raw 0 | Stage 2 / raw 1 | Stage 3 / raw 2 |
|---|---|---|---|
| Geezah | Wrong Stone 10→50 and Wood 10→Log 50; Sand 10 correct | Correct counts, aliases only | Correct counts, aliases only |
| Millewind | Correct | Correct | All four quantities 10→20 |
| Yarden Mountain | Silver Ore 20→50; Charcoal 10 correct | Each quantity 15→10 | Crystal 10→20; Sulfur 10→20; Elemental Dust 5→20 |
| Yarden Ruin | Wrong: actual Silver Ingot 30 + Rubber 10 | Wrong: actual Scrap Parts 10 + Glass 10 | Wrong: actual Black Gunpowder 20 + Electronic Circuit 20 |
| Owatatsu | Gold 15→20; Linen Wool 20→10 | Both herbs 15→5 | Both quantities 5→20 |
| Brigandine | Correct | Engine Parts 2→20; Fuel Tank 1→20 | Steel Pillar 1→20; Bioplastic 5→20 |
| Mount Muspelheim Volcano | No matching observed tower name; does not match Inferno costs | No matching observed costs | No matching observed costs |
| Sherbert | Correct | Correct | Correct |
| Unnefell | Adamantite 15→50; Glass Pane not required (Special Cement 30 instead) | Wrong: Light Weight Frame 20 + Light Weight Steel Frame 20 | Wrong: Felt Cloth 20 + Reinforced Plastic 20 |

Google says nine towers; ten are present in this single capture. It omits Miasma Wetlands, and its volcano label is not the localized observed Inferno Volcano Tower. The complete actual requirements for both appear above. The Google region-level bands and resistance-2 claims have no support in these tower definitions. Do not convert them to AP logic without area/access evidence. The absence of an environmental_gating entry is likewise not proof of unrestricted access.

## Google revised report: age assertions

Google's six supplied costs/material lists match the first six captured transition rows (world levels 0→1 through 5→6), with harmless English naming differences. Thus its displayed destination numbers 2–7 agree with assembly numbering, but the named ages cannot be authenticated from this capture. It omits three enabled transitions: world levels 6→7, 7→8 and 8→9, costing 1,000,000, 2,500,000 and 5,000,000 gold respectively.

Its unlock lists are not reliable complete lists. Compare each supplied target with the exact raw lists above:

- Destination displayed age 2: Copper tools, Blacksmith and Stone Furnace appear in index 1. Refinery [428] and Refining Scrap Bench [432] appear at index 2, not 1.
- Destination age 3: Iron tools and Conveyor Belt [644] are index 2. Excavator [476] and Breeding Facility [293] are index 3, not 2. Aging Facility [416] is index 2. Literal “Automatic Excavator” and “Aging Vat” are not the exact observed names.
- Destination age 4: Machine Factory [209] is index 3. Skilled Blacksmith [273] and Skilled Refinery [429] appear at index 5. The observed tool series at index 3 is Silver, not Steel.
- Destination age 5: Sorting Machine [650] is index 4. Platinum tools are index 5, not 4. “Factory” and “Automatic Smelter” must not be silently mapped to unknown IDs.
- Destination age 6: index 5 has Skilled Machine Factory [210], Platinum equipment and other entries above; Jetpack [799] is index 6 and Mithril tools index 7. “Advanced Factory”, “Solar Panel”, and “Power Transformer” need explicit item identity and availability validation, not acceptance as age-6 unlocks.
- Destination age 7: Adamantite tools and Chemistry Lab Table [215] are index 6. “Scientific Lab” is not the exact table name; “Ultimate Weapons” is unbounded and unverifiable as a specific unlock.

The list above evaluates exact captured unlock membership; absence alone cannot prove an item nonexistent or never craftable. A single item can be craftable with an age gate even if absent from this particular UI list. All 81 featured-item references (nine per transition) are resolved separately in the companion JSON to preserve this distinction.

## Muse revised report corrections

- “No separate displayed age numbering” is false: inspected UI adds one. Current world level index and displayed age are different numeric representations.
- Its off-by-one uncertainty for `age_new_items` is resolved by assembly, not guessed: index is WorkBenchLv; current transition row w exposes new items from index w+1.
- “worldLevel 5 requires Snow Mint ... before worldLevel 5” confuses a transition's source level with its destination. Snow Mint [3445] ×10 is consumed at level 5 to advance to 6. This alone proves neither spawn location nor a global access requirement; AP logic, trades, alternate acquisition, or data bugs can invalidate inferred geography.
- Requirement item IDs 6575–6618 have captured localized names; no need to leave them as unidentified tablets/fragments. All requirements resolve. Mushroom Wood occurs with two different IDs: [6597] Valley and [6612] Wetlands. Snow Mint requirement is [3445], not the status-99 Snowmint entry [6600]. Preserve IDs.
- WorkbenchLv 3–4 is compared to world age by the craft UI; interpreting it exclusively as station level is false. These gates correspond to displayed ages 4–5.
- Nine `ages` entries does not mean nine total reachable age states. They describe nine increments, while new-item lists cover ten states.

## Limits / next evidence

This is a static-data and code audit, not a claim that every observed late-game requirement is obtainable, or that every tower is reachable in every AP configuration. The three last age transitions use raw status-2 item records. Their presence and isEnabled=true are evidence of configured requirements, not spawn/access proof. No age names, region damage thresholds, enemy-level bands, or material spawn locations were inferred. Stable tower identity across worlds remains a separate verification problem. No game, save, session or implementation files were changed.
