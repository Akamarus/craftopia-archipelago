# Regional runs: Geezah, Millewind and experimental Yarden

Yarden Mountain and Ruin endpoints are implemented for targeted testing. See
[Yarden runs](YARDEN_RUNS.md) for check boundaries, material assumptions and required
game settings. Existing Geezah/Millewind contracts described below remain unchanged.

Use `logic_mode: regional`, `ending_region: geezah` or `millewind`, and the instance mission catalog. These are experimental capability models for new seeds. The historical `geezah_pilot` remains separate with its original dungeon-only goal and original milestone catalog.

Current profiles are `regional_geezah_20261017_r6` and `regional_millewind_20261017_r1`, using `milestones_20261017_r2`. Geezah selects 52 missions and 16 milestones (68 checks), optionally adding its 51 native-field soul orbs. Millewind includes every Geezah mission/milestone plus tower stages3010–3012, age4001 and dungeon4101 (73 checks before optional orbs). Millewind optionally includes88 native-field orbs across both regions, for161 total checks. Categories28 (members529/530) and141 (512/513) are included in both. Category141 is excluded as a source for mission513, preventing its own reward tutorial from proving itself. No additional unverified missions are selected.

Geezah victory requires tower stage3 (3002) and Ruin of Beginning (4100). Millewind victory requires Millewind tower stage3 (3012) and Old Garden of Undead (4101, native dungeon source2). Geezah completion alone never grants Millewind victory. The bridge requires actual server-confirmed goal locations. Legacy objective/age/boss goal switches add no extra requirements in regional mode.

## Identity and region evidence

The installed native dungeon name term `800_DungeonName/2`, dungeon asset40808 (Story/Reaper20), establish its Story dungeon identity; the user-completed workbook sheet1 Q025 row31 identifies it Old Garden of Undead as Millewind's main dungeon (remembered region binding; native entrance coordinates were not established). See [Millewind evidence](research/millewind-20261017/README.md). The earlier guessed dungeon source1 is not reused for new sessions; historical catalog mappings remain frozen.

Millewind membership uses native field `GrassField01`, not distance to a tower. Its environmental type does not require heat protection. Region membership limits selected checks, not physical travel. Orb membership does not establish traversal difficulty, so optional orb checks can only hold non-progression rewards; advancement permissions cannot be hidden on them.

## Opening progression

Smelting and Cooking are randomized into 18 selected opening missions accessible without AP permissions. Exclusions must leave enough distinct eligible destinations. They are not starting grants. Four Skill Points are progression: three support the optional Geezah Tamer route and one supports healing magic. Surplus copies are filler.

Tower3806 uses local Stone698, Sand752, Log746, Old Rag3400 from Straw2454, Sheep Wool758, Leather683, Copper Ore699 and Small Placeable Torch5123. Its recipes and source-age0 evolution costs come from `progression_audit_20261016_r2`. Stone/log gathering, wildlife, local sand and local age-material availability remain explicit repeatable-resource assumptions.

Furnace construction requires raw age1, a workbench and materials; operating it requires Smelting. Copper ingots support the blacksmith and cooking pot. Farm use requires Farming, the wheat field and crafted buckets filled at water. Buckets are replenishable from stone/log recipes; cooking water consumes filled bucket839 and yields Water811, with no assumed guaranteed bucket return.

Live observations corrected two earlier rules: mission67's campfire updraft does not require Cooking, and the Geezah dungeon Bow route can succeed at raw age0. Metal gear routes retain their actual station/age requirements. The model offers Sword, Bow and Tamer alternatives for Geezah. These combat estimates remain experimental.

## Millewind materials and age

The native tower UI enables age evolution on a completed world heritage tower without requiring a particular tower ID. A completed Geezah tower therefore supports raw age1→2; requiring completed Millewind tower3807 would incorrectly create a cycle with its raw-age2 recipes. This transition costs500 gold plus the audited local materials6575–6578. The model explicitly assumes renewable local-material sales through the constructed market can supply500 gold; this remains an experimental acquisition assumption, not a starting currency grant.

| Capability | Requirements |
| --- | --- |
| Iron712 | Furnace, raw age2, Smelting, Iron Ore700; local ore harvesting remains an assumption. |
| Aging Facility416 | Workbench, raw age2, iron, stone and Bone695; night skeleton combat uses the opening Sword route. |
| Grease2458 | Aging facility, raw age2, Equipment Care, Raw Flesh2050 from wildlife. |
| Bioethanol2452 | Aging facility, raw age2, Equipment Care, Wheat2055 from Farming. |
| Paper2457 | Workbench, raw age2, straw and Cooking-produced Water811. |
| Small Generator2000 | Workbench, raw age2, iron, Cogwheel2451 and logs. Player operation produces Battery2100; no Tamer or perpetual captured-enemy assumption. |
| Cogwheel2451, Bolt3468, Spring3469 | Workbench, raw age2, audited iron/copper recipes. |
| Ceramic3470 | Furnace, raw age2, Smelting, Clay3450 (sand/water) and Charcoal749 (logs). |

Tower3807 stage1 costs Paper2457×20 and Grease2458×20; stage2 costs Battery2100×20 and Bioethanol2452×20; stage3 costs Cogwheel2451×20, Bolt3468×20, Spring3469×20 and Ceramic3470×20. Each stage requires its predecessor. The graph models renewable capabilities, while these quantities remain charged by vanilla gameplay.

The generator output is tied to the inspected item2000 prefab, not Battery's inactive recipe metadata. See [utilities evidence](research/2026-10-16-remaining/utilities.md). Equipment Care is added to the Millewind permission pool. The modeled dungeon route uses Iron One-Handed Sword46: raw age2, blacksmith family20017, Iron712×3, Log746×2, Leather683×1 and three progressive Sword copies. The exact recipe is recorded in installed capture `craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json`; combat sufficiency remains an explicit conservative playtest assumption. Mandatory armor is not invented.

## Compatibility and validation

New contracts require newly generated seeds and fresh-character/world enrollment. Existing played slots, ledgers, frozen IDs and pilot sessions must not be rewritten. Protocol2 equipment behavior remains required. The new milestone catalog preserves historical catalog validation for old sessions while correcting only new-session dungeon identity.

Tests cover exact cumulative selection, category-cycle prevention, all graph nodes under full permissions, production gates, both goal conditions and100 actual AP0.6.8 fills with full reachability spheres. Model reachability is not proof of physical-game combat or harvesting sufficiency. Geezah and Millewind have reached victory in owner playtests; future expansion uses [targeted live validation](TARGETED_REGIONAL_VALIDATION.md) for new assumptions, with occasional representative full runs rather than a mandatory playthrough per region.
