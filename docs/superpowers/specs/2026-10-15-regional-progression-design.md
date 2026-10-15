# Regional progression and early Smelting protection

Status: proposed design, awaiting review. Research completed October 15, 2026.

## Intended result

Keep the large mission/Soul Orb pool, but stop treating geographic distance as free access. Early production must not depend on reaching a distant region that needs that production. Preserve the current character, world, seed, frozen location IDs, and receipt history. Tamer logic uses vanilla capabilities; respawning chests remain excluded.

## Evidence collected

The public [Kregap map](https://kregap.github.io/craftopia-map/) loads its marker definitions and coordinates from this [spreadsheet](https://docs.google.com/spreadsheets/d/1m_MW0M2kCiKJtVJ6anJQOqbHOILGh10Q6TtNCQK_KCw). Downloaded public tables and reproducible comparison are in workspace `map-research-20261015/`.

- Runtime catalog: 439 Soul Orbs. Map: 439 Soul Orb rows.
- 434 have unique matches within 0.1 world units in 3D, with no reused map row.
- AP 2208 and 2235 each encounter duplicate map rows at identical coordinates. Do not pick one arbitrarily.
- AP 2036, 2078 and 2231 have no map row within that tolerance. Nearest distances are approximately 4.90, 74.42 and 51.66 units respectively. Keep runtime coordinates authoritative.
- The original Smelting placement, AP 2420 / GUID 1902381509, matches map row 3458 exactly. Its nearest listed tower is Sherbert Iceberg, but nearest tower is not proof of region membership.
- The table has no region boundary polygons or access prerequisites. Two Unnefell tower rows share a name and marker identifier. Their distinct spreadsheet row IDs must remain separate until resolved.

The existing `Expedition.py` assigns default mission access to `field`; Soul Orbs are added directly to Overworld without access rules. The existing model correctly describes some AP permission dependencies, but does not establish physical access to all those checks.

### Geographic triage, not verified region membership

All 439 runtime entries have been grouped by nearest tower using horizontal distance solely to organize review. These counts must never be imported as access rules:

| Tower anchor | Map row | Orb candidates |
|---|---:|---:|
| Geezah Plateau | 2 | 55 |
| Millewind Hill | 3 | 41 |
| Yarden mountains | 4 | 23 |
| Yarden ruins | 5 | 20 |
| Owatatsu Archipelago | 6 | 44 |
| Brigandine Valley | 7 | 43 |
| Sherbert Iceberg | 8 | 52 |
| Unnefell, first anchor | 9 | 49 |
| Unnefell, second anchor | 10 | 62 |
| Inferno Volcano | 11 | 50 |

### Early dependency findings

Runtime recipe evidence is `batch-20261009/progression-capture-v130.json`, tied to the installed assembly in the existing audit.

| Capability/item | Captured requirements | Implication |
|---|---|---|
| Small Placeable Torch, 5123 | Log + Old Rag, bench level 0 | The first tower's listed torch cost does not itself require smelting. |
| Stone Furnace, 356 | 25 Stone + 10 Log, bench level 1 | Furnace construction and permission to operate it are separate. The user's Agricultural Age test verified operation after unlocking. |
| Copper Ingot, 711 | 3 Copper Ore, furnace bench 20018, level 1 | Copper crafting route needs Smelting plus native age/station/material access. |
| Copper Sword, 45 | 3 Copper Ingot + 2 Log + 1 Leather, level 1 | Also needs Sword permission; ownership alone does not supply the ingredients. |
| Cogwheel, 2451 | 2 Iron Ingot, level 2 | A downstream crafting route depends on Smelting. |
| Bolt / Spring, 3468 / 3469 | Iron Ingot + Copper Ingot, level 2 | Both depend on the metal-processing route. |
| Ceramic, 3470 | Clay + Charcoal, furnace bench 20018, level 2 | Furnace operation is another dependency. |

The map lists cogwheels, bolts, springs and ceramic among Millewind repair costs. This supports protecting Smelting early, but is not proof that tower repair gates travel, nor that crafting is the sole possible material source. Battery has no ingredient fields in this capture; that does not make it freely available. Map repair notes also contain English/Japanese discrepancies and need runtime validation.

## Chosen approach

Use two stages. First introduce a narrowly supported early-placement safeguard. Then build region access rules from evidence, keeping unresolved travel and resource paths visible. Do not infer region borders from nearest tower or require every tower in spreadsheet order.

Alternatives considered: granting Smelting at start is simple but removes its randomized opening reward; immediately gating all distant orbs behind guessed ages would retain the pool but manufacture progression constraints. The opening-placement safeguard preserves an early discovery without either assumption.

## Stage 1: early-placement safeguard and research metadata

For newly generated expedition seeds, constrain that player's `Production: smelting` item to the same player's ten tested opening missions: IDs 30, 50, 51, 52, 53, 54, 55, 56, 57, 58. Mission 60 is excluded because it has a Sword permission dependency. Resolve these through frozen mission IDs; never use title matching.

Apply the restriction to every potential destination, including remote worlds in multiworld, using AP placement constraints that compose with existing item rules. Do not just prefer early placement. Respect exclusions, plando and priorities; report a clear generation error if settings leave no valid opening destination. Do not silently remove items, force a starting grant, weaken the rule, or substitute an orb. Confirm the implementation approach against AP 0.6.8 before coding.

Keep pool counts and existing checks unchanged. Mark the generation profile/version distinctly so a spoiler identifies this safeguard. Historical slot data and current sessions keep their original contracts. This is an opening safety improvement, not a declaration that expedition logic is beatable.

Keep map matches as separate research metadata containing runtime GUID/AP ID, external row ID, coordinate difference, source hash, match confidence, provisional anchor and access status. Do not ship copied map notes or the external table as authoritative game data. Frozen AP IDs and names remain unchanged.

## Stage 2: actual regional logic

Each reviewed region needs explicit borders or manually reviewed membership, real entrance alternatives, native age/tower conditions where demonstrated, and a source for required materials. Each orb needs any additional traversal, cage, puzzle or encounter requirement. A nearby tower is only a geographic reference.

Represent capabilities as conjunctions and alternatives using the existing least-fixed-point progression model. Extend coverage to orb identities explicitly; no implicit `field` fallback for unknown entries in a reviewed profile. Preserve alternate vanilla combat/Tamer routes when evidence supports them.

Use logical capability nodes for repeatable access to resources, not current inventory counts. Actual observed age/tower events need dedicated identifiers and rules before integration; do not equate a raw age array index with a reached age. A resource cycle must never prove itself reachable.

A reviewed regional profile must refuse generation with unmapped enabled checks and name the missing evidence. The existing broad experimental profile remains available and clearly labeled. No claim of complete regional logic until every enabled check and the goal have evidence-backed coverage.

## Validation for Stage 1

1. Generate and fill real AP 0.6.8 expedition seeds with and without Soul Orbs across fixed test seeds. Check item/location counts, reproducibility, and exact Smelting destination membership.
2. Verify Smelting is reachable with no AP items in the modeled opening state. The existing opening playtests provide the physical-access evidence; passing fill alone is insufficient.
3. Cover two-player placement and invalid exclusion/plando combinations. Smelting must not escape into another player's distant location.
4. Retest legacy/opening generation and slot-data routing. Current generated seeds and bridge receipts must not change.
5. Validate metadata uniqueness and all 439 runtime rows. Preserve the five disputed matches as unresolved; no fabricated IDs.

## What is deliberately not changed now

No live seed regeneration, replacement DLL, forced age progression, save edits, new character, new chest checks, or retrospective reward relocation. The already authorized Smelting test grant remains a documented workaround in the active playthrough.

## Review decision

Approve Stage 1 implementation with the ten-mission Smelting constraint and separate research metadata. Stage 2 needs verified region memberships and travel/resource evidence before its access rules can be implemented responsibly.
