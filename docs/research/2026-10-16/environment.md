# Environmental evidence audit — 2026-10-16

## Result

Actual installed Unity assets were readable. Potion values, several equipment values, and field thresholds are now verified asset evidence, not missing data. This changes both Muse reports materially. No game process, save, mod source, APWorld logic, or installed game file was modified.

Companion `environment.json` contains 77 full resistance-bearing item records (including disabled/development entries deliberately retained with status), their captured runtime records, and 40 map/list/area assets. Numeric asset evidence is distinct from proof of obtainability and from a practical entry route.

## Sources and verification

- Capture: workspace `evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json`, keys `items`, `skills`, `craft_bench_map`, `game_assembly_sha256`.
- Game logic: `mod/refs/AD__Overcraft.dll`, decompiled specific types using existing ILSpy 9.1; method names below identify the evidence independently of generated line numbers.
- Installed assets: `G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data/sharedassets0.assets`, `sharedassets6.assets`, `sharedassets7.assets`; Unity version 2022.3.62f3. Sharedassets0 object 91163 (`So--ItemDataList`) references 7,053 item assets.
- Installed `Managed/AD__Overcraft.dll` SHA-256 is `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`, exactly matching the capture. Item asset file hash is preserved in JSON. All 2,387 common scalar fields in the 77 selected records matched the capture, with zero mismatches. Full item/field/area reads used byte-consumption checks.
- Decoding used existing UnityPy 1.25.3 and an isolated temporary installation of TypeTreeGeneratorAPI 0.0.10. Assets initially failed because built-in MonoBehaviour type trees were stripped/incomplete, not because files were inaccessible. Generating trees from installed managed assemblies resolved item decoding. The managed-reference registry required resolving reference types through the same generator and omitting its synthetic four-field MonoBehaviour header for non-MonoBehaviour embedded classes. No asset writes were performed. Synthetic generated MonoBehaviour header pointer values are excluded from the JSON; these are not relied upon as evidence.

## Environmental damage methods

`Oc.OcOWEnvDmgController.OcEnvDmgType`: None=0, Cold=1, Fire=2, Poison=3. Absence of Hell/Freeze enum members is not evidence of absent HellFire mechanics or a nonfunctional potion named Freeze.

`OcOWEnvDmgController.move` uses `Interval=3f`. `applyEnvDmg(type, needResistLevel)` checks final player resistance **strictly less than** the required level; equality protects. On a failing check, it queues:

| Type | Each check |
|---|---|
| Cold | -2% max HP and -2% max MP |
| Fire | -3% max HP and -1% max satiety |
| Poison | -5% max HP |

These are values passed to restoration events, not a claim that every possible downstream health modifier has been excluded. The option `IgnoreEnvDmg` bypasses this system. This method does not apply Chill/Frozen conditions.

`Oc.Maps.OcOpenWorldAreaManager.GetEnvironmentDamage` resolves dungeon SO first if in a dungeon, otherwise current area/zone if present, otherwise field data; subworld state bypasses the ordinary field branch. Therefore field thresholds alone are not universal per-location or per-tower requirements.

## Exact field/area asset rules

`environment.json.world_assets` identifies each by `_asset_file`, `_asset_path_id`, `m_Name`, `envDmgType`, `needResistLevel.rid`, and the matching `references.RefIds[].data`.

| Asset | Object | Rule |
|---|---:|---|
| sharedassets6 / Desert | 41586 | Fire, constant 1 |
| sharedassets6 / Poison | 41589 | Poison, constant 2 |
| sharedassets6 / SnowMountain | 41590 | Cold; levels [1,2], height boundary [550] |
| sharedassets6 / Volcano | 41593 | Fire; levels [3], height boundary [600] |
| sharedassets7 / cave_026 | 472 | Fire, constant 999 |
| Other 27 extracted area assets | 445–471 | None, constant 0 |
| Default, empty island, Autumn, GrassField01/02, Spring, Tutorial fields | 41582/41583/41585/41587/41588/41591/41592 | None, constant 0 |

`EnvDmgLevelByHeight.Get` loops over levels, returning level[i] if i exceeds available height bounds or player y < height[i]; if no condition matches, it returns 0. Thus SnowMountain is Cold1 for y<550, Cold2 for y>=550. Volcano is Fire3 for y<600 and Fire0 for y>=600. The surprising latter result is the literal array plus method behavior, not a guessed tier. It remains subject to area/dungeon precedence and confirmation of the selected current field. Heights are world-space player y.

Google's blanket Volcano Heat2 and Desert Heat2 are contradicted by these actual field values. Iceberg Cold2 is only conditionally consistent with SnowMountain's upper height band; exact named-region/tower association and route geometry are not proved here. Do not turn these field data into a universal hard AP gate without location/route evidence. A resistance requirement in a damage check is also not proof that traversal is impossible without resistance.

## Potion effects and duration

| ID / captured name | Asset path ID | Base effects | Base effectiveTime | Captured status |
|---|---:|---|---:|---:|
| 5546 Heat Resistance Potion | 87212 | Fire +999 | 600 seconds | 1 |
| 5547 Hell Resistance Potion | 87213 | Fire +1000, HellFire +100 | 600 seconds | 3 |
| 5548 Freeze Resistance Potion | 87214 | Cold +999 | 600 seconds | 1 |
| 5550 Poison Resistance Potion | 87216 | Poison +999 | 600 seconds | 1 |

All are in sharedassets0. These are base data values. `Oc.Item.OcItemHandler_Potion.Affect` multiplies each BuffResist field by the item all-effects multiplier (base 1 plus enchant AllEffectRate, clamped 0..1.1; cooked-food NutrientAbsorption has a separate branch), then calls the corresponding TemporaryExtend resistance updater with `item.EffectiveTime`. `Oc.Item.OcItem.EffectiveTime` directly returns `data.EffectiveTime`. Timer advancement also multiplies deltaTime by the FoodBuff_ReduceSpeed option. Thus 600 seconds describes normal-speed base duration, not an unconditional wall-clock promise under debug options.

5548 is conclusively a Cold-resistance route, despite its localized name Freeze. Muse's initial argument that there is no corresponding system is false. The later statement that effects/duration can only be resolved with consumption tests is superseded by the verified assets plus handler logic. Runtime consumption remains useful validation, not the sole available evidence.

5547 has real effect fields, but status3 plus absence from the capture's crafting map is reason not to rely on its ordinary crafting route. Do not conflate a blocked recipe with an absent mechanic, or infer that no other consumable/loot route can ever exist just from a recipe omission. See companion recipe audit for status and crafting filter semantics.

## Stacking and equipment

`OcPlStatusCtrl.TemporaryExtend.UpdateExtend` keeps the greater positive value (or more negative negative value). Equal/stronger incoming effects reset duration to the incoming duration; weaker incoming effects neither replace nor refresh the stronger slot. `TemporaryExtendSet` maintains independent positive and negative slots and sums them. Resistance potions do not add their levels together. `move` clears a non-additive slot on expiry. Different resistance types have separate slots.

`OcPlStatusCtrl.CalcPresetParameter` adds equipped `ItemData.ResistFireLv`, `ResistColdLv`, `ResistPoisonLv`, and `ResistHellFireLv`; `move` combines item-buff and equipment contributions into final stats. Thus equipment-plus-potion can combine, while two same-type potion strengths do not sum. Elemental damage resist rates (`resistElementFireRate` etc.) are separate fields and must not be substituted for environmental level resistance.

Selected captured-status1/2 equipment with verified base stats (obtainability still needs recipe/drop/age analysis):

| IDs / names | Base resistance |
|---|---|
| 787 Two-Handed Torch | Cold4 |
| 788 One-Handed Torch | Cold2 |
| 2307 / 2338 Amulet of Passion Lv1/Lv2 | Fire1 / Fire2 |
| 2308 / 2339 Amulet of Immunity Lv1/Lv2 | Poison1 / Poison2 |
| 2309 / 2340 Amulet of Cold-blood Lv1/Lv2 | Cold1 / Cold2 |
| 2376 Amulet of Versatility Lv1, 2385 Mono Charm | Fire1 + Cold1 + Poison1 |
| 2363 Master Craftsman's Whetstone, 2364 Cleansing Talisman | Poison2 |
| 2367 Earring of Ice Crystal | Cold1 |
| 2368 Earring of Contamination | Poison3 |
| 2511/2512 Red; 2515/2516 Black; 2517/2518 Blue Dragon Mail | HellFire50 |
| 2519/2520 High; 2521/2522 Assault; 2523/2524 Magic Dragon Mail | Fire999 + HellFire50 |

The code additionally adds Fire1000 for equipped IDs2511–2512 and2515–2524 when `OcWorldManager.isHell()` is true. This is a conditional code effect, not an always-present asset stat. Generic claims about cloth/leather/iron sets providing heat/cold tiers should be replaced with exact item IDs and stats. No evidence here supports arbitrary generic set rules.

Not all nonzero fields imply useful equipment: items5501–5512 have resistHellFireLv30 in data but require their item category and equipment eligibility to be checked; food-like status4 records also exist. JSON retains all 77 rather than silently treating them as viable protection. Higher amulet levels with status3/99/0 similarly must not be assumed obtainable.

## HellFire, safe zones, skills

HellFire is implemented independently of OcEnvDmgType. `OcHealth.setResist_HellFireLv` stores the value. During HellFire attribute processing, accumulation adds incoming BadCond_HellFireVal minus resistance and starts HellFire at accumulated100. A separate damage branch subtracts resistance*0.15 from HellFire condition damage with a floor of1. Thus HellFire50 is not equivalent to an environmental threshold of50 or automatic immunity. `OcTrap_HellLava.Start` normally sets 5% max-player-HP base attack damage, adds BadCond_HellFire, and sets BadCond_HellFireVal1000 (SandDragon has different handling).

`OcZone_ResistCold.OnPlStay` calls Cold TemporaryExtend.UpdateExtend(1,0.03). This can protect against a level1 requirement while refreshed, subject to stronger-slot behavior. It does not prove where such zones are placed. `OcZone_HellSafeZone.OnPlStay` calls `setAf_TrapHellLava_IgnoreUpdate`, suppressing trap updates; it is not a blanket grant of environmental fire resistance.

Capture `skills[id=1083]` is Hell Resistance, category/tier None, `is_implemented=false`, max_level6. The only skill name containing 'resist' in the 360 captured skills is this entry. Do not model it as a normally learnable route. This does not prove all skills have no indirect benefit (healing, food effects, movement, etc.), and it says nothing about the verified active resistance potion routes.

## Remaining limits and actionable evidence needs

- The extracted named asset family is not yet a fully localized named-region-to-tower/route mapping. Dungeon/environment overrides and actual area selection must be evaluated for exact checkpoints.
- Ingredient spawn locations, available gathering routes and circular access dependencies are not established by the item resistance fields. Potion recipes being surprising is not evidence they are fake.
- Equipment values are now known; crafting map membership, material supply, age unlocks, and alternate shop/drop sources determine viability. Torches are particularly useful candidates to investigate because their actual Cold2/4 stats are proven.
- Status flags and absent crafting entries do not alone prove an item is unobtainable from every channel. Do not turn Muse's broad absence claims into progression axioms.
- No invasive runtime or consumption experiment was performed. Asset-level numerical rules are strong evidence, with normal gameplay confirmation still useful for final AP logic.

## Reproduction recipe

Existing ILSpy executable: `C:/Users/Jack/.codex/.chatgpt-projects/g-p-6a888cf2cfb48191bcae6c0956701629/.local-tools/ilspycmd/ilspycmd.exe`. Use `-t <fully qualified type> mod/refs/AD__Overcraft.dll` for the methods above.

Python3.12: bundled `C:/Users/Jack/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. Add existing `C:/Users/Jack/.codex/.chatgpt-projects/g-p-6a888cf2cfb48191bcae6c0956701629/.local-tools/python` and isolated `C:/Users/Jack/AppData/Local/Temp/craftopia-research-typetree` to sys.path. Instantiate `UnityPy.helpers.TypeTreeGenerator.TypeTreeGenerator('2022.3.62f3')`; call `load_local_dll_folder(<game>/Craftopia_Data/Managed)`; set loaded environment's `typetree_generator`. Read sharedassets0 object91163; follow its `all` references for item records. For map reference registry fields, disable `TypeTreeHelper.read_typetree_boost`, replace `get_ref_type_node` in memory with a resolver calling `generator.get_nodes_up(type['asm'], namespace + class)`, strip the first four synthetic header children once per generated embedded class, and keep `check_read=True`. Read map and area path IDs listed above. This changes only the analysis process's decoding functions, not any source library or game file.
