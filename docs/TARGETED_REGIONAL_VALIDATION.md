# Regional expansion without repeated full playthroughs

Agreed direction, 2026-10-21: use automated logic validation and targeted live checks with an existing advanced test character. A full fresh-character playthrough is not required for each ending region. Geezah and Millewind owner playtests provide the current end-to-end baseline.

## What each type of evidence establishes

| Evidence | Use | Does not establish |
| --- | --- | --- |
| Installed recipes, tower and age definitions | Exact IDs, quantities, station and age constraints | Material access or combat sufficiency |
| AP generation and reachability tests | No modeled dependency cycles; selected-region bounds; permissions and goal rules | That the model matches physical gameplay |
| Targeted in-game observation | A new route, station, resource/tool threshold or completion hook works | Overall pacing or fresh-run balance |
| Occasional representative full run | Pacing and interaction of many systems | Every optional route or region |

Reuse verified evidence and detection hooks. Re-test them when their code or relevant game data changes. Do not ask players to repeat early missions merely because a seed has finished.

## Automated coverage before enabling an ending

- Exact cumulative check selection; no later-region checks or nonexistent locations.
- Tower-stage ordering and age dependencies, including recipe ingredients and station access.
- Tower plus mapped main-dungeon victory, requiring server-confirmed completion of both.
- Full modeled reachability across a seed batch, plus missing-permission and exclusion cases.
- Essential unlocks cannot be placed behind their own requirements.
- Unverified orb traversal cannot provide required progression rewards.
- Existing seed contracts and single-player behavior remain compatible.

The current regional tests already exercise many of these conditions for Geezah and Millewind. Later regions are intentionally not selectable yet. Running current seed tests is not evidence that later-region logic exists.

## Targeted evidence queue

| Priority | Target | Evidence already available | Specific remaining work |
| --- | --- | --- | --- |
| 1 | Yarden Mountain tower3808 and Ruin tower3809 | All six repair cost lists captured | Join ingredients to obtainable routes and age/station gates; resolve main-dungeon identity and which tower completion defines a Yarden ending before implementing it |
| 1 | Regional age materials | Exact transition costs; user confirmed two pickups, one mined material and one chopped material per regional set | Test minimum viable pickaxe/axe on the mined/chopped sources, starting with the next required set; do not infer tiers from rarity or age |
| 2 | Owatatsu3810 and Brigandine3811 | Repair costs and shared later age-transition ingredients captured | Establish whether the intended endpoint requires cross-region materials; join herbs, engine parts and manufacturing access |
| 3 | Sherbert3812 | Tower costs; cold effects and height-dependent field rules; resistance recipes/items | Confirm named-region/route binding and practical protection route at the relevant age; test only then |
| 3 | Unnefell3813, Miasma3814, Inferno3815 | Tower costs; fire/poison field rules and resistance evidence | Join actual travel and sustainable material/protection routes; Inferno also needs a verified Captured Anubis route |
| Separate UI task | Orb markers and Field Guide | Existing implementations and tests | One focused check of immediate unexplored-region markers, collected-grey state, pan/zoom and pause/camera behavior; no seed completion needed |

Sources: [tower/age audit](research/2026-10-16/towers-ages.md), [environment audit](research/2026-10-16/environment.md), [current regional model](REGIONAL_RUNS.md), and the user's completed workbook. Consult that workbook before asking for region/dungeon names again. Capture internal IDs to confirm name mappings rather than guessing from display names.

## Small live test packets

Prepare one packet only after exhausting existing captures, workbook answers and installed-data evidence. Each packet names the exact object/action, expected observation and evidence to collect. Use the same advanced test character where possible; an existing completed AP seed is not a new test session.

For the next material packet: identify the relevant ore/tree, try the lowest candidate tool, record whether it yields the target material, then try the next tier only if needed. A failure to damage or harvest and the equipped tool must be distinguishable in the capture. Collect screenshots/F9 evidence where the collector records the necessary state. A menu recipe alone does not prove harvestability.

For a new tower/dungeon hook: observe the relevant state before and after one repair/clear, check exactly one intended check, then reload and verify it does not duplicate. Do this in a deliberately prepared diagnostic/test session rather than silently repurposing a completed slot or changing personal save enrollment.

Presumed legacy equipment remains excluded. Multiplayer testing is a separate queue and does not block this work. No new seed or installation is required merely to prepare the evidence matrix.

## Baseline verification — 2026-10-21

Against the local AP0.6.8 source, `unittest discover -s worlds/craftopia/test -p 'test_regional*.py' -v` passed all17 tests, and `-p 'test_millewind.py' -v` passed all5 tests. These include100 full-reachability fills each for current Geezah and Millewind profiles, plus pilot/exclusion cases. AP used its pure-Python fallback because the optional C++ speedup was unavailable; neither suite failed. No live game action, seed replacement, save edit or later-region enablement was performed.
