# APworld generation audit — 27 October 2026

This audit follows a failed sync attempt. The host's final YAML error was unavailable, so the audit does not claim to reproduce that specific message. It reproduces independent defects in the shipped package and multiworld guard.

## Corrections

- Read bundled catalogs through the Python module loader, so importing the actual ZIP works. Testing an extracted source folder had missed this failure.
- Remove the regional one-AP-player restriction. Keep the separate-slot requirement; native shared-world Craftopia multiplayer remains a separate validation task.
- Keep opening cooking and smelting local. Allow other progression to cross games. Local regional deadlines constrain the receiving player's items only when placed in that player's Craftopia world; they cannot impose a time deadline on another game.
- Use Archipelago's APworld container writer and version manifest: Craftopia 1.18.2, minimum AP 0.6.8.
- Report unsupported option combinations as `OptionError`, without hiding catalog corruption or other internal errors.
- Cache progression reachability by the complete relevant inventory-count snapshot, bounded to 128 snapshots per model. Copies of states and item removals cannot reuse stale unlock results. The previous evaluator repeatedly recomputed the entire graph for each location.
- Count surplus Skill Points consistently in generation and tracker reconstruction, including removal. Only four copies are marked progression for placement, but the remaining copies still grant real points. Universal Tracker 0.3.4 exposed a mismatch because it reconstructed those copies using the default item classification while server logic ignored them. The regression reproduced a server count of zero versus a tracker count of one before the fix.

## Final artifact

`artifacts/audit-delivery-1.18.2/craftopia.apworld`

SHA-256: `c75b20108f1b6d06b7dc646a242ec795ba43383f54d4f6ff658d75b880566c04`

`Akamarus.yaml` beside it uses `game: Craftopia`, the `Craftopia:` settings key, regional logic and a Geezah ending. Capitalization is intentional. The host must install the new APworld and restart an AP 0.6.8 generator before generating.

## Validation

- Actual final archive discovery and full generation, including fill, accessibility, balancing, spoiler and multidata: **72/72 passed**. Six endings × three starting-combat modes × solo, mixed APQuest, two Craftopia slots, and Craftopia in player slot 2. All 54 multiworld cases placed progression across players. Local opening placements and applicable item rules were checked after generation. Final matrix: `artifacts/audit-delivery-matrix/results.json`. The delivered Akamarus YAML was parsed and confirmed identical to the tested Geezah mixed-world configuration.
- Larger mixed multiworld on the final archive: **3/3 passed**, each with one Craftopia slot and 15 APQuest slots. This exercises player-count scaling, not compatibility with every possible other game. `artifacts/audit-delivery-large/results.json`.
- Starter bridge: **3/3 passed**. C# starter transaction and enrollment/reconnect/reload checks also passed. These are automated checks, not live gameplay acceptance.
- Source regression suite: **88/88 passed** in 389.587 seconds, including the multi-seed fill and progression suites. `artifacts/apworld-audit-tracker-fix-tests.log`. Independent review then caught the historical pilot profile's missing smelting-locality fallback. Its new two-player regression failed before the fix and passed afterward, alongside the existing regional-locality test (2/2 targeted tests). Before the GitHub merge, the complete final suite was rerun: **89/89 passed** in 186.137 seconds (`artifacts/premerge-apworld-tests.log`). The focused bridge suite passed 50/50, starter transaction/runtime tests passed using a writable TEMP/TMP directory, and the Release mod build completed with zero warnings/errors. The historical broad client runner stalled and was stopped; no all-client-suite pass is claimed.
- Ionium AP 0.6.8: **253 core tests and 360/360 fuzz cases passed**, with no failures across all 11 variants, in 6 minutes 59 seconds. This includes 100 Universal Tracker cases. Evidence: `artifacts/ionium-audit/passed-360.txt` and `passed-360.png`. That archive's SHA-256 is `ae6de0015de65698c99c66c00c3b31a889c4146fe05697535951ac553996074a`; it precedes only the pilot-locality fallback. The final delivery archive separately passed **253 core tests and 33/33 fuzz cases**, three per variant, in 1 minute 59 seconds. Evidence: `artifacts/ionium-audit/final-33.txt` and `final-33.png`. Both runs selected AP 0.6.8 only; the site's timeout calibration uses 0.6.7 regardless of the selected test version.

The earlier optimized candidate passed 500 standard regional fuzz generations and 49 additional no-restrictive-starts generations on the Ionium browser tester. That run was stopped intentionally after option-error handling was refined. Do not count its unrun variants as passes. The next 650-case run passed 649 and exposed one Universal Tracker mismatch on Brigandine, minimal accessibility, random starting combat and three skill points per check. Its visible reproduction is saved in `artifacts/ionium-audit/tracker-failure.txt` and `tracker-failure.png`. The final archive includes the resulting Skill Point fix.

Ionium runs use the supported regional profile overrides in `artifacts/ionium-audit/regional-meta.yaml`; ending region, starting combat, accessibility and other remaining options vary. These are not claims that every historical experimental configuration is playable.

The pre-audit package failed 498/500 standard fuzz attempts and 495/500 no-restrictive-starts attempts, largely because unsupported options were reported as crashes. Many other baseline variants were ignored rather than exercised. A valid later-region case also timed out. Baseline evidence is retained in `artifacts/ionium-audit/baseline.zip`.

## Reproduce

Use an AP 0.6.8 checkout and its Python environment, set `CRAFTOPIA_AP_SOURCE` to that checkout and `PYTHONUTF8=1`. On restricted machines, point TEMP/TMP at a writable directory.

```text
python tools/package_apworld.py artifacts/your-build/craftopia.apworld
python tools/test_packaged_world.py --apworld artifacts/your-build/craftopia.apworld --output artifacts/your-matrix --matrix --runs 3
python tools/test_packaged_world.py --apworld artifacts/your-build/craftopia.apworld --output artifacts/your-large-test --mixed --players 16 --runs 3
python -m unittest discover -s worlds/craftopia/test -p "test_*.py" -v
```

Use a new output directory per run. The archive harness installs the ZIP into an isolated AP user directory and asserts it was imported from `.apworld`, never an extracted source copy.

## Limits

Passing generation proves the modeled rules admit completion; it does not prove every modeled route works in the game. Native Craftopia co-op still requires host/guest validation. On October 28, the owner confirmed the Staff starter flow after the 1.18.2 Magic-key and session-display fixes; other starter classes retain automated coverage. The final host error cannot be identified without its traceback. This audit records the validated source update. Merging it into GitHub main does not itself publish a new downloadable release or update Steam Workshop.

## October 28 large-room follow-up

The same final APworld passed a full 50-player generation with 50 distinct games (Craftopia plus 49 others), including fill, balancing, accessibility and playthrough generation. The generated server data contained exactly the reviewed 119-item Geezah pool, with no missing or extra copies and all checks represented in reachable spheres. Opening cooking and smelting stayed local. Of the 119 incoming items, 117 were placed across 37 other games. This was a generation test, not a 50-person live session. Local evidence: `artifacts/fifty-distinct-20261028/results.json` and `craftopia-item-audit.json`.
