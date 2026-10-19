> **Outcome update — October 19:** the owner completed the Millewind run and the server confirmed tower and dungeon victory. Connection/enrollment survived reload without enrolling again. The preparation notes below are historical. Current installation instructions are in [Installation](INSTALLATION.md); v1.15.0 map/pause acceptance remains pending.

# Millewind playtest prepared — October 17, 2026

Version 1.13.0 adds a cumulative Geezah/Millewind ending. Generated seed
`34287358488305010024` uses generator seed `2026101701` and slot `MillewindRun`.
It has 161 checks: 52 missions, 88 regional soul orbs, 11 category claims, six
tower stages, two age evolutions and two story dungeons. Victory requires
Millewind tower stage3 (3012) and Old Garden of Undead story clear (4101).

The completed workbook supplies the dungeon's regional association. Installed
localization and dungeon assets verify story source2; repeat source202 is
excluded. Native tower code confirms the completed Geezah tower can perform
the evolution needed for Millewind's first repair. See [identity evidence](research/millewind-20261017/README.md).

This run includes Basic Cooking101 and Mission Tutorial category rewards,
Equipment Care/Aging Facility access, audited repair materials, and the updated
notification cards. Existing Field Guide history and controls are retained.
The shorter Geezah profile now has119 checks. Old milestone catalogs remain
supported; no prior seed, character, ledger or game installation was changed.

69 world tests passed, including100 Geezah fills and100 Millewind fills with
orbs alternately enabled/disabled.107 focused bridge tests, all16 C# regression
programs, and10 milestone catalog tests passed. The Release mod builds with
zero warnings/errors. A separate code review found no confirmed
blocking defects. The seed archive was inspected directly for slot identity,
goals, regional bounds,88orb IDs,161locations and the correct catalog. Its
matching bridge package starts successfully. Package files have a SHA-256
manifest. This is prepared for installation, not a completed live validation.

Next live acceptance belongs in a fresh run: verify initial enrollment with
both selected towers, then play through crafting, new category claims, age
evolution, three Millewind repairs and the story dungeon. Resource availability,
combat sufficiency and individual orb routes remain experimental; optional
orbs cannot contain required progression. No arbitrary completion quota was
added to prolong the run.

The prepared package is in the workspace's `outputs/millewind-20261017` folder.
The finished Geezah session stays separate. Installation and fresh-character
enrollment are the next steps when the user is ready to start.
