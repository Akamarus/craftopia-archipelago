# Regional progression and early Smelting protection

Status: region approach approved in conversation; revised scope includes tower repair stages and age evolution. Exact environmental requirements and runtime completion boundaries remain experimental. Research completed October 15, 2026.

## Intended result

Keep the large mission/Soul Orb pool and organize it by region. Most of the world is physically open: distance and higher-level enemies alone do not establish a mandatory tower, age or weapon gate. Model actual environmental and individual check requirements. Preserve the current character, world, seed, frozen location IDs, and receipt history. Tamer logic uses vanilla capabilities; respawning chests remain excluded. Add a separate AP check for each successful tower repair stage and each actual age advancement in future seeds.

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

For newly generated expedition seeds, constrain that player's `Production: smelting` item to the same player's verified Geezah Soul Orbs OR ten tested opening missions: IDs 30, 50, 51, 52, 53, 54, 55, 56, 57, 58. The user explicitly accepts Geezah orbs as opening placements. Resolve these through frozen IDs; never use title matching. Mission 60 remains outside the initial guaranteed-opening set because it has a Sword permission dependency.

Review Geezah membership before adding orb IDs to this placement set; the 55 nearest-tower candidates are not automatically Geezah. Any individual orb prerequisite must still be modeled, and Smelting must not lock itself behind that prerequisite. When orb checks are disabled, use the opening missions. If the verified eligible union is empty, reject generation with an explanation.

Apply the restriction to every potential destination, including remote worlds in multiworld, using AP placement constraints that compose with existing item rules. Do not just prefer early placement. Respect exclusions, plando and priorities; report a clear generation error if settings leave no valid opening destination. Do not silently remove items, force a starting grant, or widen the eligible union. Confirm the implementation approach against AP 0.6.8 before coding.

Keep pool counts and existing checks unchanged. Mark the generation profile/version distinctly so a spoiler identifies this safeguard. Historical slot data and current sessions keep their original contracts. This is an opening safety improvement, not a declaration that expedition logic is beatable.

Keep map matches as separate research metadata containing runtime GUID/AP ID, external row ID, coordinate difference, source hash, match confidence, provisional anchor and access status. Do not ship copied map notes or the external table as authoritative game data. Frozen AP IDs and names remain unchanged.

## Stage 2: actual regional logic

Each reviewed region needs explicit borders or manually reviewed membership, real entrance alternatives, native age/tower conditions where demonstrated, and a source for required materials. Each orb needs any additional traversal, cage, puzzle or encounter requirement. A nearby tower is only a geographic reference.

The user reports cold and heat survival requirements in the iceberg and volcano areas. Represent these as distinct survival capabilities with alternative proven routes, rather than inventing a mandatory armor tier. Trace each route's recipes, ingredient sources, station permissions, native age, equipment permissions if applicable, strength and duration. Verify whether protection is needed throughout a region or only in specific hazard zones. Do not assume a brief unprotected run, healing spam, death recovery or a modded Tamer ability is a normal logic route. Exact thresholds and practical traversal remain playtest questions. Keep enemy difficulty as advisory information unless an actual required encounter has supported capability requirements.

Represent capabilities as conjunctions and alternatives using the existing least-fixed-point progression model. Extend coverage to orb identities explicitly; no implicit `field` fallback for unknown entries in a reviewed profile. Preserve alternate vanilla combat/Tamer routes when evidence supports them.

Use logical capability nodes for repeatable access to resources, not current inventory counts. Actual observed age/tower events need dedicated identifiers and rules before integration; do not equate a raw age array index with a reached age. A resource cycle must never prove itself reachable.

A reviewed regional profile must refuse generation with unmapped enabled checks and name the missing evidence. The existing broad experimental profile remains available and clearly labeled. No claim of complete regional logic until every enabled check and the goal have evidence-backed coverage.

## Tower repair stages and age evolution checks

### Location identity and scope

Create one network location per authored tower and completed repair stage, using a stable game tower identifier plus the resulting stage. Use readable labels such as `Geezah Plateau: Tower Repair Stage 1`. Enumerate the actual number of stages per tower; do not assume every tower has three stages or turn the map's ten rows into ten verified tower identities. Give final repair its own stage location, not a duplicate generic completion check.

Create one network location per actually reached age beyond the starting age, using the verified destination world-level value and display name. Do not award a starting-age check. Nine raw age-data rows do not prove nine advancements. Freeze new AP IDs after catalog validation in a non-overlapping range, preserving all mission and orb IDs.

Existing mission checks may also complete during the same action. Retain those distinct locations deliberately; never accidentally submit a tower stage twice through both a repair callback and an achievement callback. Decide pool composition for the added locations in the implementation plan, preserving item/location balance.

### Observation and delivery

Inspect the installed implementation before choosing hooks. `OcAchievement_RestoreTower.OnAchieve` is currently only a candidate for full restoration; it is not evidence of individual stages. The collector already observes `Oc.Maps.OcWorldManager.SetWorldLevel(int)`, but that method can be called while loading or synchronizing state. A setter callback alone is not an age-advancement event.

Capture stable tower identity, world identity, prior/resulting stage, paid costs and successful completion boundary. For age evolution capture prior/resulting world level, the authoritative advancement path, required costs, and whether the call came from load/synchronization. Only successful new transitions in the bound session create observations. Cancellation, failed payment, repeated callbacks, entering a region, reconnect, and restoring a save do not create new checks.

Persist observations through the existing outbox before sending. Bridge routing must use explicit slot-data allowlists for these families. Deduplicate by seed/slot and stable location identity, and only display completed status after confirmation. Preserve world/character enrollment rules and reject observations from a different world. In shared-world play, validate authoritative changes and distribute credit using the existing enrolled-player policy; do not assume every network callback is a locally paid repair.

Keep native repairs, material spending, age unlocking and world changes intact. AP checks are attached to successful progression. Do not suppress native advancement or its enabling effects. Any separate native item/currency reward requires inspection before deciding replacement behavior.

### Dependencies and existing saves

Tower stage N requires the preceding stage plus valid access to its material sources and operating permissions. Age advancement requires its real vanilla conditions and material capabilities; it is not automatically locked behind every geographically earlier tower. Never place the only required production/resistance unlock at the stage or age that needs it.

New tower/age checks require a newly generated seed. Do not append locations to the running 564-location seed. Preserve the current character for diagnostics and further playtests. Before a future seed uses a progressed character, explicitly define enrollment/baseline treatment for already-completed stages and ages; do not silently retro-award them or reset progress. Runtime probes should use upcoming uncompleted stages where possible.

### Verification

Test intermediate and final stage success, unsuccessful payment, cancellation, duplicate callbacks, save/reload, disconnect/replay, mismatched identity and allowlist rejection. For ages also test same-level assignment, lower-level/load assignment and legitimate advancement; unexplained multi-level jumps must be held rather than inventing intermediate completions. Confirm stages and actual age names with runtime evidence before marking the catalog ready. Generation tests must include repair/resource cycles, environmental self-locks, pool balance, and routing compatibility with old seeds.

## Validation for Stage 1

1. Generate and fill real AP 0.6.8 expedition seeds with and without Soul Orbs across fixed test seeds. Check item/location counts, reproducibility, and exact Smelting destination membership.
2. Verify Smelting is reachable with no AP items in the modeled opening state. The existing opening playtests provide the physical-access evidence; passing fill alone is insufficient.
3. Cover two-player placement and invalid exclusion/plando combinations. Smelting must not escape into another player's distant location.
4. Retest legacy/opening generation and slot-data routing. Current generated seeds and bridge receipts must not change.
5. Validate metadata uniqueness and all 439 runtime rows. Preserve the five disputed matches as unresolved; no fabricated IDs.

## What is deliberately not changed now

No live seed regeneration, replacement DLL, forced age progression, save edits, new character, new chest checks, or retrospective reward relocation. The already authorized Smelting test grant remains a documented workaround in the active playthrough.

## Review decision

The user approved regional grouping, early missions OR verified Geezah orbs for Smelting, open travel without invented enemy-level gates, and experimental refinement of environmental survival rules. They additionally requested checks for every tower repair stage and age evolution. Next prepare an implementation plan covering evidence collection first, catalog/observer integration second, and generation safeguards with regression tests. No claim of complete regional access or working tower-stage hooks until those paths are verified.
