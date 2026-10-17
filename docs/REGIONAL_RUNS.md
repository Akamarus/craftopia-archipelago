# Regional runs — Geezah first

Use `logic_mode: regional`, `ending_region: geezah`, and the instance mission catalog. The complete example is `worlds/craftopia/Regional.yaml`.

Three Skill Point copies are progression for the modeled Tamer route. Surplus copies still award skill points but are classified as filler, allowing excluded locations to receive them. This classification change applies only to regional mode.

Victory requires AP confirmation of **both** Geezah Tower Repair Stage 3 (3002) and Ruin of Beginning (4100). Earlier tower stages alone do not satisfy tower completion. The generator models both requirements; the existing bridge checks the actual confirmed location IDs. Legacy objective/age/boss goal switches do not add extra conditions in this mode.

The current profile is `regional_geezah_20261016_r4`. See [v1.12.3 changes](VERIFIED_PROGRESSION_v1123.md) for exact construction dependencies, the bucket route and Bow tier2. It has 52 checks: 38 missions, nine category reward claims, three tower stages, Agricultural Age, and Ruin of Beginning. This includes the previously omitted mission-screen (512) and category-reward (513) tutorials. The latter requires a completable native category, regardless of where its AP reward is placed. No completed-category reward is granted automatically. Revision 2 adds the [explicit Goblin's Club rule](LOOT_EQUIPMENT_REVIEW.md) and requires the updated bridge and mod v1.12.1.

All selected milestones belong to Geezah, and mission routing contains only explicitly selected missions. Categories must have every member selected. The selection boundary limits randomized checks; it does not prevent physical travel outside Geezah. Soul orbs, respawning chests, Anubis and later-area checks are not included in this first regional profile.

## Progression rules

Rules are explicit for every selected mission and milestone; there is no catch-all unknown-mission access rule. Smelting and Cooking are restricted to 18 selected opening missions that require no AP permissions. Both remain randomized, not starting grants. Exclusions must leave enough distinct eligible destinations for both items. Placement constraints compose with AP location rules; impossible settings fail generation rather than widening the placement set.

| Capability | Modeled requirements and evidence |
| --- | --- |
| Stone tools / workbench | Local stone/log gathering; opening playtest passed. |
| Tower stage 1 | Stone 698, Sand 752, Log 746; local sand gathering. The smelting recipe for sand is not the only source. |
| Tower stage 2 | Stage 1, Old Rag 3400 (Straw 2454 at the workbench), Sheep Wool 758, Leather 683. No invented Body armor permission requirement. |
| Tower stage 3 | Stage 2, Copper Ore 699, Small Placeable Torch 5123 (log + old rag). Ore is not an ingot; the placeable torch is not a gated weapon. |
| Agricultural Age | Completed Geezah tower plus local age-material gathering. Captured age items include Clover, Plateau Moonstone, Ironwood and Plateau Ancient Doll Fragment. Currency quantities remain vanilla. |
| Stone Furnace | Agricultural Age, stone/logs and workbench; construction does not require Smelting. |
| Copper ingots | Furnace + ore + Smelting. |
| Blacksmith / Cooking Pot | Agricultural Age and copper ingots; both recipes require 10 ingots in the capture. These checks cannot hide the only Smelting unlock. |
| Glider/updraft | Glider uses local wildlife materials; using the campfire updraft requires Cooking. |
| Farming | Wheat Field construction at Agricultural Age plus crafted buckets filled at water; watering and harvesting require Farming. Well construction is not assumed. |
| Dungeon | Agricultural Age and a Sword/Smelting, Bow, or vanilla Tamer route. These combat capability estimates remain experimental. |

Material names/recipes were read from the collector capture `craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json`; frozen tower and mission identities remain in their existing catalogs. The live pilot confirmed all three Geezah repair transitions, Agricultural Age, category claims, dungeon victory and reload deduplication. This does not prove every alternative resource route or combat threshold. Local wildlife, sand and age-material access are documented playtest assumptions, not claims inferred solely from crafting recipes. The graph models repeatable capabilities, not already-owned inventory quantities.

## Run-length readiness

Geezah is currently the only selectable ending. Another region needs an explicit check selection, its actual final tower stage, a captured main-dungeon identity, and reviewed recipe/resource/access rules before being added. Millewind's tower is captured, but its dungeon/end-to-end resource logic is not ready. Heat/cold item values and recipe capabilities are audited, but complete named-region routes remain unverified; no late-region access is invented. Nearest-tower orb proximity is not accepted as region membership.

## Compatibility and testing

The `geezah_pilot` profile stays at 50 checks with its original dungeon-only goal. Existing IDs, ledgers, running sessions and saves are unchanged. The new regional mode requires a newly generated seed and the established fresh-character/world enrollment process; do not switch an existing played slot to this contract. Revision 2 requires equipment protocol 2; revision 1 retains the original equipment contract.

Automated tests cover exact selection and routing, both goal conditions, recipe dependencies, zero-permission opening placements, exclusions including insufficient distinct slots, unsupported regions/options, full reachability spheres and 100 actual AP 0.6.8 fills. Passing these establishes reachability under the documented model, not a physical-game proof. A new regional playthrough remains the live acceptance step.

Revision 3 uses the [audited progression catalog](AUDIT_INTEGRATION_20261016.md) for tower materials and age costs. Mod v1.12.2 also adds the audited station-family mappings. Existing live seeds are not rewritten.
