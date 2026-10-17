# Craftopia progression research audit — October 16, 2026

## Result

The supplied Google and Muse reports are useful leads but are not safe to import as progression logic. This audit replaces their tower and age tables with installed-build evidence, checks every supplied processed recipe, investigates material-source alternatives, and extracts environmental effects from the game assets. No mod, active seed, save, or game installation was changed.

The scope is everything supplied in the two Google attachments, the inline environmental registry, and the two Muse investigations. Both Google attachments end inside their recipe registries; missing text was not reconstructed. This is a substantial audit of those claims, not a declaration that every location, drop table, dungeon or late-game route in Craftopia is verified.

## Detailed evidence

| Audit | Coverage |
|---|---|
| [Towers and ages](towers-ages.md) | All 10 observed towers and 30 repair stages; all 9 captured age transitions; all 10 new-item lists; Google/Muse comparisons and assembly evidence |
| [Recipes and AP production gates](recipes.md) | All 34 complete supplied processed-recipe rows across both reports; 58 claims/candidates and 68 recursive item records; recipe batches, stations, ages and AP enforcement |
| [Environmental resistance](environment.md) | Actual resistance item effects, durations, environmental damage logic and extracted field/area definitions |
| [Material acquisition](material-sources.md) | Raw-source claim review; 33 relevant shop entries; 857 geographic leads from the map author's dataset; explicit missing access evidence |

Each audit has an adjacent JSON evidence file. Exact item IDs are retained because duplicate names exist. The detailed tables are the corrected reference; the points below explain their consequences.

## Corrections that materially change progression

1. **There are 10 towers in this capture, not nine.** Google omitted Miasma Wetlands and its volcano description does not match the observed Inferno Volcano Tower. Numerous stages have wrong ingredients or quantities. Millewind's third repair needs 20 each of Cogwheel, Bolt, Spring and Ceramic, not ten. The full tables include every observed stage, including the very different desert and volcano costs.
2. **`workbenchLv` is a world-age requirement.** The game directly compares it with `WorldLevel`; it is not an independent station upgrade level. The UI displays that index plus one. This resolves Muse's uncertainty through code rather than inference.
3. **Nine transition rows describe ten age states.** Google's six listed transition cost sets match the corresponding captured rows, but its unlock examples are unreliable and it omits three later transitions. Their configured presence does not prove all late-game resources are obtainable in normal released play.
4. **Paper, Grease and Bioethanol were mischaracterized.** Paper uses Straw×2 + Water×1 at displayed Age 3. Grease uses Animal Flesh×2 and Bioethanol Wheat×2 at the Aging Facility, also Age 3. This supports the actual Millewind blockers found during play.
5. **Batch size matters.** One filled Bucket makes ten Water. A Steel Pillar batch makes ten pillars. Treating these as one-output recipes greatly exaggerates requirements.
6. **The potion recipes in the supplied environmental JSON are wrong.** Active Heat Resistance Potion uses Yellow Herb + Snow Mint + Water; Freeze Resistance Potion uses Yellow Herb + Hell Pepper + Water. Both are displayed Age 5 recipes in family 20012. Their extracted effects last 600 seconds. The asset audit supplies the numeric resistance values and environment comparisons.
7. **Hell protection is a separate mechanic.** HellFire is not an environmental-damage enum member. A stored Hell Resistance Potion exists but is absent from the normal captured recipe map; that does not prove every alternative route to hell protection is impossible. Do not merge fire damage, environmental heat and status freezing into one requirement.
8. **Sources can bypass manufacturing gates.** Loaded trade definitions include Snow Mint, Hell Pepper, special herbs, manufactured components and other materials. These are concrete alternative-route leads, but merchant access/currency still need verification. Raw gathering ages and mandatory pickaxe claims in Google remain unproven.
9. **Several AP station families need review.** The generic-bench resolver lacks Potion Brewing Stand family 20012, Machine Factory 20013 and Furniture Workbench 20020. If their runtime objects use that generic class, they fail closed before consuming materials even with all production unlocks. This is a code-grounded conditional, not a claimed live reproduction.
10. **The installed assets are available.** The diagnostic omitted effect fields; that did not mean the game's data could not be inspected. Direct asset extraction resolved resistance values that Muse left unknown. Capture omissions, incomplete extraction and absent game content are different findings.

An especially useful finding is the torch route: One-Handed Torch 788 provides Cold2 and its captured recipe is Straw×1 + Log×1, displayed Age1; Two-Handed Torch 787 provides Cold4 with the same ingredients at displayed Age2. Both use player crafting family20002 and have status1. This can break the assumed cold-ingredient/heat-ingredient dependency, subject to the seed's equipment permission and actual equipped behavior. It should be evaluated before making late-age potions the only cold-protection route.

The extracted field rules are Desert Fire1, SnowMountain Cold1 below world height550 and Cold2 above, and Volcano Fire3 below height600 with a zero fallback above. These are field-asset rules, not yet a verified named-region route map; local area and dungeon definitions take precedence. One extracted cave override requires Fire999. The environmental report preserves the exact rules and their limitations.

## How this should shape AP logic

Use a route-based dependency model: an item may be crafted, harvested, purchased, dropped, rewarded or generated. For a crafting route, include recipe age, batch yield, station construction, station operation permission, required blueprint and recursively obtainable ingredients. Do not require smelting merely because an ingot *can* be smelted when an accessible alternative provides it.

For tower checks, preserve exact tower identity and ordered stages. A stage needs its materials and access; the next stage also needs the prior stage. Age transitions have separate costs and should use raw indices internally with correct displayed labels. Do not use item rank or a community enemy-level band as an automatic world-age gate.

For resistance, combine the actual environmental type and level at the destination with a sustainable protection route. Temporary potion duration must be considered; a recipe alone does not prove enough protection for a journey. Field definitions and local area overrides matter, so a whole named region should not receive a guessed single threshold.

For the agreed regional runs, keep checks within the chosen endpoint and require that region's tower completion plus main dungeon clear for victory. The audited dependencies should constrain placement so an unlock cannot be hidden behind its own required crafting, protection or tower path. The present report does not claim existing seeds have already been repaired or validated against this expanded model.

## Remaining work, in priority order

1. Resolve actual runtime classes for the three missing production families and the generator; deliberately assign AP permissions and verify both denied and allowed operations. This is the highest-value implementation follow-up.
2. Tie field/area damage assets to named regions, tower locations and traversable routes. Distinguish hazardous subareas from safe approaches.
3. Resolve merchant locations, barter currencies and stock rules; generator output/cycle requirements; exact harvest and enemy drop tables. Prioritize Battery, Elemental Dust, resistance ingredients and later age materials.
4. Build region-scoped reachability using alternative routes, then test generated seeds for self-locks and dependency cycles. Keep provisional late-game content out of required progression until acquisition is established.
5. Perform focused gameplay confirmation on the existing character: station behavior, potion application/expiry and boundary transitions. A new character or repeated long playthrough is not needed merely to read static definitions.

## Provenance and confidence

Primary capture: `evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json`, schema 5, collector 1.5.2, timestamp 2026-10-16T18:18:40Z. It contains 7,053 items, nine age rows, 21 bench maps and 87 loaded stations. Loaded-only world data is not a complete census of every possible scene.

Game assembly SHA-256: `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`. Directly inspected methods and extracted asset references are documented in the component reports. Code behavior, serialized definitions and observed gameplay are kept distinct.

The [interactive map](https://kregap.github.io/craftopia-map/) is used as its author's geographic dataset, not as proof of recipe costs, current availability or access gates. Historical pre-Seamless guides were not allowed to override the installed build. The Google and Muse documents are the claims under review, not independent supporting sources.
