# Game-wide check expansion audit — October 15, 2026

## Scope and result

This audit inventories the installed game's systems and the available runtime captures, including difficult, late-game and unimplemented checks. It does not restrict the backlog to the eleven opening missions. It is not an exhaustive enumeration of every asset, world placement or executable path: unopened areas and unloaded assets remain uncatalogued.

Primary sources: installed `AD__Overcraft.dll` SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`; collector 1.3.0 capture at `2026-10-10T07:04:42.3558176Z`; frozen mission catalog `20261007_r1`; existing local playtest records. Read-only inspection scanned 4,739 top-level assembly types, selected 437 system-related types, extracted 492 enemy-enum constants, and examined calls in six reward/purchase methods. Enum entries include NPCs, variants and possibly unavailable content; they are not 492 playable enemies.

The mission catalog contains **125 distinct mission locations, 11 opening-tested and 114 outside that opening profile**. Those other 114 are catalogued candidates, not all blocked and not all proven playable. The newer capture exposes raw `worldLevel` 0 for 92 missions and 1 for 33. Zero does not mean free access: a level-35 enemy mission can still have raw worldLevel 0. Subclass requirements, quest order, enemy locations and resource chains matter.

The progression capture has 7,053 item rows (7,052 unique positive IDs), 2,985 rows whose first material ID is positive, 21 recipe-bench groups and nine age records at world levels 0–8. These are raw data counts, not supported item/recipe/location counts. Age names, transition indexing and all costs must be tied to actual advancement behavior before assigning goals.

## Check families beyond the captured missions

Evidence labels: **Static** = installed implementation or data structure found; **Catalog** = runtime row observed; **Proposal** = a designed milestone requiring a new observer. None of these labels alone means runtime completion or reward replacement has passed.

| # | Candidate check family | Location granularity | Evidence and remaining work |
|---|---|---|---|
| 1 | Every captured mission | One numeric mission ID | Catalog: all 125 listed below; extend prerequisite coverage rather than flattening them into free checks. |
| 2 | Additional missions outside the snapshot | One newly observed mission ID | More character/world/age captures; append frozen AP IDs without renumbering. |
| 3 | Native achievements | One achievement ID | Static achievement manager plus build/craft/item/kill/dungeon/tower subclasses; enumerate actual data and avoid duplicating mission wrappers unintentionally. |
| 4 | NPC quest completion | Quest group + quest ID | Static `SoQuestData`, `OcQuestMng.QuestClear`, reward methods; capture quest chains and native conditions. |
| 5 | Quest-chain finale | One terminal quest ID | Proposal derived from quest links; distinguish optional side chains and mandatory story progression. |
| 6 | Tower restoration | One authored tower identity | Static tower getters, network messages and restoration achievement; capture tower IDs, material costs and completion boundaries. |
| 7 | Age advancement | One reached world level | Static `SetWorldLevel`/`OnWorldLevelChange`, nine catalog rows; distinguish load/set from a new advancement and verify tower dependencies. |
| 8 | Open-world soul orbs | One authored orb placement | Static spawner `_guid`, `SetTaken`, manager save array and `TryGetReward`; verify IDs across reloads and world layouts. |
| 9 | Gated/caged soul orbs | One orb placement, with puzzle requirements | Static cage/fire classes; same orb identity must not award twice through cage and pickup observers. |
| 10 | Fixed overworld chests | One persistent treasure identity | Static `OnOpenTreasure(int,long)` and `Open_FromRpc`; establish argument semantics, world binding and first-open evidence. |
| 11 | Respawning overworld treasure | One bounded location per authored site | Static treasure handler; choose first opening per seed, never unlimited checks on daily respawn. Procedural identity needs separate investigation. |
| 12 | Dungeon treasure | Dungeon + authored chest identity | Static treasure-box completion and dungeon systems; distinguish generated placements from persistent ones. |
| 13 | Supply-corps rewards/chests | One finite reward stage | Static supply-corps chest/save types; do not treat every player storage chest as a location. |
| 14 | Challenge completion rewards | Challenge definition + placement | Static `SoChallengeData.id/GetReward` and challenge starter; definition ID alone may be shared by several placements. |
| 15 | Token races | One race/placement, first success | Static token-race and partial-token variants; capture winning conditions and movement requirements. |
| 16 | Extermination challenges | One encounter challenge | Static extermination data; include combat capability and enemy region, not just a reward hook. |
| 17 | Bedrock/grave challenges | One challenge placement | Static break-bedrock/break-grave data/events; capture tool/combat/time conditions. |
| 18 | Snowball challenges | One challenge placement | Static snowball data; availability and actual reward path need capture. |
| 19 | First dungeon discovery | One dungeon identity | Static discovery mission and manager source IDs; catalogue entrances and access requirements. |
| 20 | First dungeon clear | One dungeon ID | Static clear-event and save interfaces; reject load restoration and repeated clears as new checks. |
| 21 | Boss first defeat | Boss identity + intended encounter/region | Static enemy data and existing generic dungeon-boss mission; enumerate real encounter assets and verified defeat ownership. |
| 22 | Limited boss dungeon | One configured dungeon encounter | Static limited-boss classes/lists; capture entry requirements, keys and repeat behavior. |
| 23 | Boss-rush milestones | Selected finite floors or first clear | Static boss-rush best-floor and progression methods; actual floor table/rewards and DLC/availability remain unknown. |
| 24 | Anubis meeting | One authored NPC/meeting milestone | Catalog mission 100; optional generalization needs stable NPC identity. |
| 25 | Slate-of-Growth stat awards | Stat track + finite purchase number | Catalog mission 101; inspect the actual slate exchange independently of soul-orb exchanges. |
| 26 | Soul-orb exchange awards | Reward track + purchase number | Static exchange-count array, reward table and confirmation path; finite cap, currency preservation and reward replacement required. |
| 27 | Shop stock purchases | Shop identity + stock entry + finite ordinal | Static `SoShopList` and `TryPurchase`; do not key only by item ID or count every repeat purchase. |
| 28 | Rotating/random merchant stock | Explicit bounded AP purchase slots | Static random sale-info types; stock churn cannot create new locations. Decide catalog versus AP-specific finite stock. |
| 29 | First craft of selected equipment | Exact item recipe ID | Catalog recipes/equipment; native ages, station, materials and progressive category tier all apply. |
| 30 | First craft of selected production stations | Exact station recipe ID | Catalog recipes; crafting a station and operating it can have different permissions. |
| 31 | First successful production batch | Station group + selected output | Proposal using production systems; observe successful output rather than start-button presses. Keep automation from repeating checks. |
| 32 | First harvest of each selected crop | Crop item/species | Wheat mission exists; other crop milestones need actual output/source data and farm permission. |
| 33 | First catch of selected fish | Fish species ID | Static fishing states/lottery; capture fish tables, bait/regions and successful inventory addition. |
| 34 | Cooking/alchemy recipe milestones | Selected finished recipe ID | Catalog recipes and existing flour/bread/potion missions; permission and renewable inputs needed. |
| 35 | First capture of selected creatures | Species + first capture | Mono/cow missions catalogued; capture support and weakened-target requirements must come from actual game data. |
| 36 | First registered pet of selected species | Species + pet registration | Static `OcPetMng.NewPet`, pet save model; capture is not automatically taming. |
| 37 | First pet deployment | Species or one introductory milestone | Static `DisplayPet`; avoid rewards from restoring already-deployed pets. |
| 38 | Pet level milestones | Species or team-best level at selected thresholds | Static level controller; choose finite thresholds and prevent pet replacement from farming checks. |
| 39 | Pet skill acquisition/upgrade milestones | Selected pet skill + threshold | Static pet skill UI/strengthening systems; capture actual skill IDs and success boundary. |
| 40 | Pet riding/flying licenses | One license type | Static license predicates; actual unlock transition and prerequisites still needed. |
| 41 | Breeding/creature-combining milestones | Selected successful result | Static breeding/BreedPlant systems; inspect which is animal breeding versus crop breeding and enforce appropriate production unlock. |
| 42 | Enemy hunting milestones | Species + selected level/count threshold | Numerous catalog missions already provide this; additional thresholds are proposals, not discovered locations. |
| 43 | Picture-book collection milestones | Entry or category threshold | Static picture-book/statistical UI; capture registration data, category counts and reachable entries. |
| 44 | Refinement milestones | Selected operation/threshold | Catalog missions and Refinery/Refining Scrap Bench; refinement is not smelting. |
| 45 | Enchantment milestones | Selected operation/recipe | Catalog missions 571–575; enchant source access and item prerequisites required. |
| 46 | Combat/skill techniques | Explicit technique event | Existing jump-slash, ignite, headshot, mana-use and Heal missions; new techniques need exact success semantics. |

These are 46 families, not 46 additional locations. Per-placement systems could contribute many locations, but this audit does not invent orb/chest/shop totals without their runtime catalogs.

## Important implementation findings

1. **Quests and missions are separate.** `SoQuestData` exposes `groupID`, `questID`, predecessor and player-level conditions, requirement lists and `GetReward`; the quest manager exposes completion events and `QuestClear(int)`. A mission-only capture misses this content.
2. **Orb identity has a concrete starting point.** `OcSoulOrb_Spawner._guid` and the manager's taken-orb save data are better evidence than a position-only heuristic. Stability and world scope are not yet proven.
3. **Treasure has a host/RPC path.** `OnOpenTreasure(int,long)` and `Open_FromRpc(int,long,int)` exist. The types of arguments do not prove which is a permanent location ID; inspect bodies and compare live save records before choosing keys.
4. **Challenges have definition IDs, but instance identity still matters.** Reusing a challenge definition at multiple map placements must not collapse all instances, as the old mission-class approach did.
5. **Reward replacement needs actual transaction boundaries.** IL call inspection shows challenge and quest reward methods grant money/experience. Soul-orb stat confirmation calls `AddSkillPoint` and writes exchange counts. Shop purchase code can decrease an item stack and change currency. A postfix that reports a check after vanilla rewards is additive, not replacement. Preserve costs and progression flags while replacing only the selected reward.
6. **Progression dependencies are broader than AP ownership.** Raw mission worldLevel does not capture all recipe age, station, enemy-region, traversal, resource and quest-chain requirements. Never place Smelting behind a check that requires smelted equipment without a genuinely available alternative.
7. **Tamer should be a full route.** Separate capturing, registration, deployment, command skills, pet levels and advanced training. Do not impose a Tamer-tree requirement on creature capture unless actual game behavior requires it. Optional TamerCombat must not become mandatory for base-world logic.
8. **Repeated content needs finite identities.** Respawns, repeat dungeon clears, purchases, reconnects, new pets and shared-world network echoes must not create unlimited AP rewards.
9. **Native achievements may overlap existing missions.** Make deliberate choices about counting both. Different observation callbacks for the same reward are not automatically two locations.
10. **The eleven-check seed is complete and immutable.** It cannot gain new randomized locations in place. Keep the current character/world for observation and higher-tier tests. A larger generated seed needs an explicit continuity/enrollment plan; never relabel old grant indices or treat a progressed character as fresh.

## Boss candidates requiring encounter cataloguing

The installed enum/data classes include Dragon, Griffon, DragonBone, Kraken, MonoKing, SnakeGiant, TigerIce, GriffonAutumnal, NPC_Boss_AnubisDark, Hydra, DragonDemons, NPC_Paladin, Satyros, Gigantes variants, NobleFrog, NPC_Boss_AnubisDark_True, WhaleDesart, SandDragon, Goddess variants and DragonCyber. These names are code identifiers, not a verified boss-selection menu. NPC-prefixed types, variants and legacy content need encounter/availability classification. The configurable boss goal should expose only verified encounters, while this wider list remains in the investigation backlog.

## Proposed substantial development batches

**Batch A — full mission catalog expansion:** classify all 125 entries, retain frozen IDs, and implement explicit requirements for traversal, building, farming, commerce, metalworking, equipment, alchemy, refinement, enchantment, taming and hunting. Build a broad experimental profile plus a tested progression profile; label the difference plainly. Include raw age and subclass constraints in the collector. Do not restrict this effort to easy first-age checks.

**Batch B — exploration and rewards:** implement per-instance soul orbs, fixed overworld treasure, challenge rewards and dungeon clears using a shared durable observation/replacement contract. Capture actual locations, test one specimen per system, then enumerate the full maps. Include Anubis exchange rows and NPC quests in the capture immediately, even if their adapters follow.

**Batch C — Tamer and production:** implement capture/tame/deploy milestones, then finite pet-level/skill/license milestones, fishing, farming and selected production outputs. Keep optional companion-mod compatibility visible in the test matrix.

**Batch D — world progression and endgame:** tower/age checks, quest chains, boss encounters, boss-rush milestones, advanced enemy hunts, refinement and enchanting. Use their actual chains to support boss/age/objective goal combinations.

**Batch E — shop and replacement completion:** finite shop/Anubis reward catalogs, useful materials/money delivery, capacity/overflow handling, successful-payment semantics, restart recovery and shared-world authority. These systems should be investigated alongside earlier batches; ordering reflects integration risk, not exclusion from scope.

Category toggles remain part of the design. Supported configurations require enough locations for progression items and a reachable goal; unsupported combinations must explain the missing evidence rather than silently weakening logic. All branches retain normal resource crafting and age requirements, separate progressive equipment categories, shared-credit design intent and explicit multiplayer support limits.

## Runtime evidence still needed across the game

Capture authoritative orb/chest/challenge placements and identities, quest graphs/rewards, tower and age requirements, dungeon/encounter tables, shop stock/cost identities, Anubis reward tracks, capture/tame species and pet-skill/license tables, fish/crop sources, recipe unlock conditions, and achievement/picture-book catalogs. Capture definition data independently of loaded instances where the game API permits it; otherwise identify which regions/ages must be loaded. For each adapter, test success, cancellation/failure, repeated action, save/reload, offline replay and host/client ownership. Read-only capture comes before reward suppression in an established save.

## Complete captured mission inventory

The table below is generated directly from the frozen catalog and matched to the newer raw mission fields. Grouping and AP prerequisite notes are audit recommendations, not claims that access rules have already been implemented. Opening-tested refers specifically to the eleven-check opening run; other entries may have individual live observations described in earlier test documents.

| Group | Captured checks |
|---|---:|
| Opening tested | 11 |
| Traversal | 4 |
| Foraging and food gathering | 4 |
| Building and storage | 7 |
| Commerce | 3 |
| Farming and cooking | 7 |
| Equipment and metalworking | 9 |
| Alchemy | 4 |
| Refinement | 5 |
| Enchanting | 5 |
| Clothing | 3 |
| Automation | 4 |
| Tamer | 5 |
| Skills and combat techniques | 6 |
| Exploration, dungeon and Anubis | 6 |
| Mission meta | 2 |
| Enemy hunting | 40 |

### All 125 mission instances

| Game ID | AP ID | Group | Exact catalog title | Raw worldLevel | Evidence |
|---:|---:|---|---|---:|---|
| 30 | 1005 | Opening tested | Equip a stone workbench and place it | 1 | opening-tested |
| 50 | 1012 | Opening tested | Punch trees 5 times | 1 | opening-tested |
| 51 | 1013 | Opening tested | Make a Stone Axe | 1 | opening-tested |
| 52 | 1014 | Opening tested | Equip a Stone Axe | 1 | opening-tested |
| 53 | 1015 | Opening tested | Obtain 30 numbers of Log | 1 | opening-tested |
| 54 | 1016 | Opening tested | Punch rocks 5 times | 1 | opening-tested |
| 55 | 1017 | Opening tested | Make a stone pickaxe | 1 | opening-tested |
| 56 | 1018 | Opening tested | Equip a stone pickaxe | 1 | opening-tested |
| 57 | 1019 | Opening tested | Collect 30 stones | 1 | opening-tested |
| 58 | 1020 | Opening tested | Make a stone workbench | 1 | opening-tested |
| 60 | 1022 | Opening tested | Craft Stone One-Handed Sword | 0 | opening-tested |
| 10 | 1001 | Traversal | Make a glider on the stone workbench | 1 | catalogued; rule/runtime validation needed |
| 59 | 1021 | Traversal | Fly through the sky with a glider | 1 | catalogued; rule/runtime validation needed |
| 67 | 1025 | Traversal | Ride the updraft from a campfire to fly high into the sky | 1 | catalogued; rule/runtime validation needed |
| 79 | 1030 | Traversal | Equip a Glider | 1 | catalogued; rule/runtime validation needed |
| 61 | 1023 | Foraging and food gathering | Knock out an animal and get 3 pieces of raw meat | 1 | catalogued; rule/runtime validation needed |
| 514 | 1051 | Foraging and food gathering | Obtain 20 Straw by destoying bushes in the field | 0 | catalogued; rule/runtime validation needed |
| 521 | 1054 | Foraging and food gathering | Harvest 5 Wild Berry | 0 | catalogued; rule/runtime validation needed |
| 523 | 1056 | Foraging and food gathering | Gather 3 Red Herb | 0 | catalogued; rule/runtime validation needed |
| 63 | 1024 | Building and storage | Craft a campfire | 1 | catalogued; rule/runtime validation needed |
| 95 | 1039 | Building and storage | Craft Wooden Slope | 0 | catalogued; rule/runtime validation needed |
| 96 | 1040 | Building and storage | Deploy Wooden Slope | 0 | catalogued; rule/runtime validation needed |
| 97 | 1041 | Building and storage | Rotate Wooden Slope | 0 | catalogued; rule/runtime validation needed |
| 98 | 1042 | Building and storage | Collect the buildings you've set up | 0 | catalogued; rule/runtime validation needed |
| 563 | 1075 | Building and storage | Equip and Deploy Bed | 0 | catalogued; rule/runtime validation needed |
| 564 | 1076 | Building and storage | Equip and Deploy Chest | 0 | catalogued; rule/runtime validation needed |
| 16 | 1003 | Commerce | Make 100G from selling on the market | 1 | catalogued; rule/runtime validation needed |
| 36 | 1007 | Commerce | Set up a market | 1 | catalogued; rule/runtime validation needed |
| 516 | 1052 | Commerce | Craft Market on Workbench | 0 | catalogued; rule/runtime validation needed |
| 15 | 1002 | Farming and cooking | Let's make 5 wheat fields | 1 | catalogued; rule/runtime validation needed |
| 70 | 1026 | Farming and cooking | Craft Cooking Pot | 1 | catalogued; rule/runtime validation needed |
| 105 | 1046 | Farming and cooking | Obtain a bucket of water from the well | 0 | catalogued; rule/runtime validation needed |
| 106 | 1047 | Farming and cooking | Water the wheat field | 0 | catalogued; rule/runtime validation needed |
| 107 | 1048 | Farming and cooking | Harvest 1 counts of wheat from the grown wheat field | 0 | catalogued; rule/runtime validation needed |
| 529 | 1059 | Farming and cooking | Craft Wheat Flour on Cooking Pot | 0 | catalogued; rule/runtime validation needed |
| 530 | 1060 | Farming and cooking | Craft Bread on Cooking Pot | 0 | catalogued; rule/runtime validation needed |
| 72 | 1027 | Equipment and metalworking | Craft a Stone Furnace | 1 | catalogued; rule/runtime validation needed |
| 73 | 1028 | Equipment and metalworking | Smelt ores with a furnace to Craft 20 ingots | 1 | catalogued; rule/runtime validation needed |
| 78 | 1029 | Equipment and metalworking | Craft Blacksmith | 1 | catalogued; rule/runtime validation needed |
| 80 | 1031 | Equipment and metalworking | Craft Branch Bow on Workbench | 1 | catalogued; rule/runtime validation needed |
| 81 | 1032 | Equipment and metalworking | Craft Branch Arrow on Workbench | 1 | catalogued; rule/runtime validation needed |
| 82 | 1033 | Equipment and metalworking | Use Blacksmith to craft a copper shield | 1 | catalogued; rule/runtime validation needed |
| 85 | 1034 | Equipment and metalworking | Craft a detector | 0 | catalogued; rule/runtime validation needed |
| 86 | 1035 | Equipment and metalworking | Equip a detector | 0 | catalogued; rule/runtime validation needed |
| 565 | 1077 | Equipment and metalworking | Equip Stone One-Handed Sword | 0 | catalogued; rule/runtime validation needed |
| 46 | 1010 | Alchemy | Obtain 3 Mana potion | 1 | catalogued; rule/runtime validation needed |
| 522 | 1055 | Alchemy | Craft Medicinal Herb | 0 | catalogued; rule/runtime validation needed |
| 524 | 1057 | Alchemy | Craft Potion Brewing Stand on Workbench | 0 | catalogued; rule/runtime validation needed |
| 525 | 1058 | Alchemy | Craft Small Life Potion on Potion Brewing Stand | 0 | catalogued; rule/runtime validation needed |
| 539 | 1064 | Refinement | Craft Refinery on Workbench | 0 | catalogued; rule/runtime validation needed |
| 540 | 1065 | Refinement | Craft Refining Scrap Bench on Workbench | 0 | catalogued; rule/runtime validation needed |
| 541 | 1066 | Refinement | Generate Refining Stone III on Refining Scrap Bench | 0 | catalogued; rule/runtime validation needed |
| 542 | 1067 | Refinement | Put Iron One-Handed Sword into Refinery's Chest for Refining Material Weapon | 0 | catalogued; rule/runtime validation needed |
| 543 | 1068 | Refinement | Put Refining Stone III into Refinery's Chest for Refining Stone | 0 | catalogued; rule/runtime validation needed |
| 571 | 1082 | Enchanting | Craft Enchanting Table | 0 | catalogued; rule/runtime validation needed |
| 572 | 1083 | Enchanting | Obtain Enchanting Fragment | 0 | catalogued; rule/runtime validation needed |
| 573 | 1084 | Enchanting | Apply Enchantment to Iron One-Handed Sword | 0 | catalogued; rule/runtime validation needed |
| 574 | 1085 | Enchanting | Apply "Good Cook's" Enchantment to Cooking Pot | 0 | catalogued; rule/runtime validation needed |
| 575 | 1086 | Enchanting | Craft Bread with Enchantments | 0 | catalogued; rule/runtime validation needed |
| 519 | 1053 | Clothing | Craft Old Rag on Workbench | 0 | catalogued; rule/runtime validation needed |
| 556 | 1070 | Clothing | Use Hatter to make capes | 1 | catalogued; rule/runtime validation needed |
| 557 | 1071 | Clothing | Use Hatter to craft Red Beret | 1 | catalogued; rule/runtime validation needed |
| 37 | 1008 | Automation | Place 5 Rotating Saws | 0 | catalogued; rule/runtime validation needed |
| 38 | 1009 | Automation | Place 5 Excavator | 0 | catalogued; rule/runtime validation needed |
| 531 | 1061 | Automation | Craft Rotating Saws on Workbench | 0 | catalogued; rule/runtime validation needed |
| 532 | 1062 | Automation | Craft Excavator on Workbench | 0 | catalogued; rule/runtime validation needed |
| 533 | 1063 | Tamer | Craft Monster Prism on Workbench | 0 | catalogued; rule/runtime validation needed |
| 560 | 1072 | Tamer | Craft Pet Management Machine on a Workbench | 0 | catalogued; rule/runtime validation needed |
| 561 | 1073 | Tamer | Capture Mono | 0 | catalogued; rule/runtime validation needed |
| 562 | 1074 | Tamer | Capture Cow | 0 | catalogued; rule/runtime validation needed |
| 570 | 1081 | Tamer | Tame Mono as a pet | 0 | catalogued; rule/runtime validation needed |
| 7 | 1000 | Skills and combat techniques | Do a jump-slash | 1 | catalogued; rule/runtime validation needed |
| 28 | 1004 | Skills and combat techniques | Set an enemy on fire | 1 | catalogued; rule/runtime validation needed |
| 34 | 1006 | Skills and combat techniques | Defeat a cow with a headshot | 1 | catalogued; rule/runtime validation needed |
| 47 | 1011 | Skills and combat techniques | Use 110 Mana within 10 seconds | 1 | catalogued; rule/runtime validation needed |
| 99 | 1043 | Skills and combat techniques | Use the skill points to learn a skill | 0 | catalogued; rule/runtime validation needed |
| 544 | 1069 | Skills and combat techniques | Learn Heal and use it | 0 | catalogued; rule/runtime validation needed |
| 87 | 1036 | Exploration, dungeon and Anubis | Explore the island and find a dungeon | 0 | catalogued; rule/runtime validation needed |
| 88 | 1037 | Exploration, dungeon and Anubis | Defeat the boss inside the dungeon | 0 | catalogued; rule/runtime validation needed |
| 93 | 1038 | Exploration, dungeon and Anubis | Open the map | 0 | catalogued; rule/runtime validation needed |
| 100 | 1044 | Exploration, dungeon and Anubis | Meet the God of Sky Island (Anubis) | 0 | catalogued; rule/runtime validation needed |
| 101 | 1045 | Exploration, dungeon and Anubis | Give Slate of Growth to Anubis to increase your stats | 0 | catalogued; rule/runtime validation needed |
| 567 | 1079 | Exploration, dungeon and Anubis | Give Soul Orb to Anubis to exchange them with rewards | 0 | catalogued; rule/runtime validation needed |
| 512 | 1049 | Mission meta | Check the mission screen | 0 | catalogued; rule/runtime validation needed |
| 513 | 1050 | Mission meta | Receive a reward by completing all missions of a mission category | 0 | catalogued; rule/runtime validation needed |
| 566 | 1078 | Enemy hunting | Defeat 1 Goblin | 0 | catalogued; rule/runtime validation needed |
| 568 | 1080 | Enemy hunting | Defeat 1 Deer | 0 | catalogued; rule/runtime validation needed |
| 577 | 1087 | Enemy hunting | Defeat 5 Cow of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 578 | 1088 | Enemy hunting | Defeat 5 Sheep of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 579 | 1089 | Enemy hunting | Defeat 5 Deer of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 580 | 1090 | Enemy hunting | Defeat 5 Chicken of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 581 | 1091 | Enemy hunting | Defeat 3 Bear of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 583 | 1092 | Enemy hunting | Defeat 5 Goblin of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 584 | 1093 | Enemy hunting | Defeat 5 Goblin Sword of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 585 | 1094 | Enemy hunting | Defeat 5 Goblin Archer of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 586 | 1095 | Enemy hunting | Defeat 3 Greataxe Gremlin of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 587 | 1096 | Enemy hunting | Defeat 3 Greatshield Gremlin of Lv 1 or higher | 0 | catalogued; rule/runtime validation needed |
| 589 | 1097 | Enemy hunting | Defeat 5 Goblin of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 590 | 1098 | Enemy hunting | Defeat 5 Goblin Sword of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 591 | 1099 | Enemy hunting | Defeat 5 Goblin Archer of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 592 | 1100 | Enemy hunting | Defeat 3 Greataxe Gremlin of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 593 | 1101 | Enemy hunting | Defeat 3 Greatshield Gremlin of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 595 | 1102 | Enemy hunting | Defeat 5 Killer Bee of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 596 | 1103 | Enemy hunting | Defeat 5 Phoechicks of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 597 | 1104 | Enemy hunting | Defeat 5 Wizard of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 598 | 1105 | Enemy hunting | Defeat 5 Lizard Axe of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 599 | 1106 | Enemy hunting | Defeat 5 Lizard Sword of Lv 10 or higher | 0 | catalogued; rule/runtime validation needed |
| 601 | 1107 | Enemy hunting | Defeat 10 Goblin of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 602 | 1108 | Enemy hunting | Defeat 10 Goblin Sword of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 603 | 1109 | Enemy hunting | Defeat 10 Goblin Archer of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 604 | 1110 | Enemy hunting | Defeat 5 Greataxe Gremlin of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 605 | 1111 | Enemy hunting | Defeat 5 Greatshield Gremlin of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 607 | 1112 | Enemy hunting | Defeat 10 Skeleton Soldier of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 608 | 1113 | Enemy hunting | Defeat 10 Skeleton Archer of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 609 | 1114 | Enemy hunting | Defeat 10 Werewolf of the Crescent Moon of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 610 | 1115 | Enemy hunting | Defeat 3 Skeleton Pirate of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 611 | 1116 | Enemy hunting | Defeat 5 Skeleton Sailor of Lv 20 or higher | 0 | catalogued; rule/runtime validation needed |
| 613 | 1117 | Enemy hunting | Defeat 10 Goblin of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 614 | 1118 | Enemy hunting | Defeat 10 Goblin Sword of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 615 | 1119 | Enemy hunting | Defeat 10 Goblin Archer of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 616 | 1120 | Enemy hunting | Defeat 5 Greataxe Gremlin of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 617 | 1121 | Enemy hunting | Defeat 5 Greatshield Gremlin of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 619 | 1122 | Enemy hunting | Defeat 3 Gigantes Ballista of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 620 | 1123 | Enemy hunting | Defeat 3 Gigantes Hoplon of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
| 621 | 1124 | Enemy hunting | Defeat 3 Kraken of Lv 35 or higher | 0 | catalogued; rule/runtime validation needed |
