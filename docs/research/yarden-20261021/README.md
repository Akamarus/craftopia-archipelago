# Yarden targeted progression audit — 2026-10-21

Read-only evidence audit. No mod, seed, save, workbook or installed files changed. `evidence.json` contains workbook answers, exact six-stage costs, a 31-item recursive recipe closure, age-transition costs and native dungeon definitions/localization. `extract.py` reproduces the extraction from the workspace using the existing research decoder. Current assembly SHA256 matches the October16 audit: `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.

## Dungeon mapping resolved

| Endpoint | User workbook evidence | Native Story source | Asset | Boss metadata |
| --- | --- | --- | --- | --- |
| Yarden Meadow's Mountain Tower3808 | Logic questions Q026, row32: Ruin of Hunters | 3 | sharedassets6:40809 | type101, level35 |
| Yarden Meadow's Ruin Tower3809 | Logic questions Q027, row33: Old Garden of Storms | 4 | sharedassets6:40810 | type44, level35 |

Installed localization terms `800_DungeonName/3` and `/4` match these exact English names. Story definitions have `battleDungeonType=0`. Sources203/204 have the same names but `battleDungeonType=1`; never substitute the repeat sources for the Story checks. Entrances40797/40798 default to40809/40810 respectively and both have `_underDevelop=0`. Their `subDungeonList` age restrictions describe repeat variants, not proof of a Story entry gate.

The workbook explicitly reports no special entry item/quest requirement and combat gear as the practical limitation. That is remembered player evidence. Native entrance coordinates, exact field membership and a combat-sufficient AP equipment route are not established by this extraction. Do not derive an AP weapon tier from boss level35.

The two tower/dungeon pairs support distinct proposed endpoints; this is not justification to collapse both into one unexplained Yarden goal. Selection and cumulative orb/mission boundaries must still be defined before generation support is added.

## Mountain tower3808

| Stage | Exact costs | Dependency findings |
| --- | --- | --- |
| 1 | Silver Ore701 ×50; Charcoal749 ×10 | Silver harvesting/source access; Charcoal is2 Log per unit in Furnace20018, raw age1/displayed Age2, Smelting |
| 2 | Fang680 ×10; Bird Feather692 ×10; Bone695 ×10 | Existing specific goblin, chicken/phoechick and skeleton drop evidence; no invented crafting-age lock on loot |
| 3 | Crystal2453 ×20; Sulfur763 ×20; Elemental Dust2460 ×20 | Existing wizard/skeleton drop routes for Crystal/Dust; sulfur gathering/trade access still needs a regional route |

The only active crafted ingredient in this tower's immediate list is Charcoal. Silver Ore's metadata age3 is **not** a recipe/acquisition gate: it is no-craft family20001. Do not force Age4 for this tower solely because its ore has that metadata. Combat, effective harvesting and travel may impose practical requirements independently.

Specific drop evidence is retained in [the drop audit](../2026-10-16-followup/drops.md): ordinary Wizard554–556 has Crystal at base1.0; Wizard557 at0.2; weapon-bearing Skeleton558–573 supplies Bone/Dust at base1.0; Goblin534–545 supplies Fang at1.0; Chicken20 supplies Feather×3 at1.0. Eligibility, spawn access and renewal remain separate from table membership.

## Ruin tower3809

| Stage | Exact costs | Immediate recipes |
| --- | --- | --- |
| 1 | Silver Ingot713 ×30; Rubber2455 ×10 | Ingot:3 Silver Ore, Furnace, raw age3. Rubber:2 Bioethanol, Aging Facility, raw age3 |
| 2 | Scrap Parts3373 ×10; Glass769 ×10 | Scrap:5 Iron Ingot +5 Bolt +5 Spring, Workbench, raw age3. Glass:5 Sand, Furnace, raw age1 |
| 3 | Black Gunpowder764 ×20; Electronic Circuit3466 ×20 | Gunpowder:1 Sulfur +1 Saltpeter +1 Charcoal, Workbench, raw age2. Circuit:1 Crystal +1 Copper Ingot +1 Ceramic +1 Rubber, Workbench, raw age3 |

All listed recipes output one per batch. The selected full crafting chain requires displayed **Age4 (raw3)**. It reuses **Smelting, Equipment Care, Farming and Cooking**: Rubber→Bioethanol→Wheat; Circuit→Ceramic→Clay→Water. Water produces10 per filled bucket; Sand produces10 per Stone. Do not multiply their ingredient counts as if output were one. Workbench manual crafting is not an additional invented production permission.

Age3→4 (raw2→3) costs2500 gold, Mandragora6580×5, Emerald of Hills6581×20, Ancient Wood6582×20, Hills Ancient Doll Fragment6583×5. These are the Hills set, not Meadow's next-transition set. A completed earlier tower can support evolution; requiring Yarden Ruin completion first would create a cycle. Renewable gold access needs an explicit economic assumption/route, as in the earlier profiles.

Saltpeter2046 is an Aging Facility recipe at raw2, consuming Feces2048×1. The workbook's Material sources row34 reports natural player production **if enabled**. Existing decompiled `moveStoolTimer` checks `DisableStool`, while the player toilet path also checks `Option_NotStool` (which is populated from settings.NotStool). Do not assert unconditional supply or silently change user settings. A supported source/settings policy or verified alternative is required before this route is treated as guaranteed. This audit has not exhausted alternative shops/loot or proved that every source is disabled by that setting.

## Harvesting correction: age materials are already resolved in data

The [prior harvesting audit](../2026-10-16-followup/harvesting.md) established source rarity0/1 for all nine age-material ore/wood sets. Stone Pickaxe56 and Stone Axe68 have attack rank0 and pass the actual tool comparison. Hills6581/6582 and Meadow6586/6587 therefore do not need invented higher tool tiers. A live trial would validate this operationally, but it is not an unanswered minimum-tier question.

Ordinary Silver Ore701 is different from those age materials. Existing decoded drop definitions include silver-bearing static data71 (sharedassets0:80664) and473–475 (sharedassets13:197–199), rarity4. Under the audited damage comparison, rank3 Iron Pickaxe is sufficient to avoid bounce **if these are the targeted live resource definitions**; rank0 Stone and rank1 Copper are below that no-bounce threshold. This is not an absolute impossibility gate: the damage code reduces damage on bounce rather than proving zero drops. Exact live resource linkage and practical harvesting time remain useful targeted checks. Sulfur also occurs in multiple positive drop tables; the workbook's “Rocks / Everywhere” is too broad to encode literally.

## Short remaining gameplay packet

No fresh character or full regional playthrough is needed. Prepare a separate diagnostic session before asking the player to do these; do not repurpose a finished AP slot silently.

1. **Silver/sulfur source:** at one accessible Yarden silver node, record position/source, equipped tool and actual drops. Compare a lower tool only if practical efficiency matters to the chosen logic. Identify an accessible sulfur source in the same outing. No need to repeat age-material tiers across nine regions.
2. **Dungeon evidence:** record each Yarden Story entrance and existing completion state with the advanced character. If a completion event has not been captured, clear that dungeon once and capture the forcefield-completion event; do not redo earlier regions. Use the event's source3/4 and reload persistence, not a nearby chest, to validate credit.
3. **Saltpeter supply:** after inspecting alternative sources/settings code, confirm the selected enabled route yields Feces→Saltpeter at the Aging Facility. Record relevant settings; avoid an indefinite wait presented as a guaranteed resource source.

Tower cost definitions and generic repair hooks need no six-stage replay just to re-prove already captured costs. Read existing stage state first; request a stage transition only if its identity or hook behavior remains uncertain.

## Verification and remaining implementation work

Extraction completed with zero dungeon decode errors, four matching localization records, two Story and two repeat definitions, two default entrance joins, and31 recipe-closure items. Workbook SHA256 was identical before/after: `611c5e3997042e3523e6942c3a0cccc009e26450e04c5df1ef8b4b7a27d0bc76`.

Still needed before enabling an ending: cumulative region/check membership, conservative combat/harvesting routes, sustainable saltpeter supply policy, and explicit profile/goal tests. Existing Geezah/Millewind profiles stay unchanged. This report resolves evidence gaps; it does not advertise Yarden as a playable generated endpoint.
