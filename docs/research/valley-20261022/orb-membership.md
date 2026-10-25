# Owatatsu and Brigandine native orb membership

The frozen subsets use `tools/export_yarden_orbs.py`'s native field resolver against the retained `2026-10-16/environment.json` audit. This resolves native zone cylinders in field-list order, with height limits, weight precedence and the native five-unit vertical offset; it does not use nearest-tower assignment.

- Owatatsu tower3810: GUID -27423557, position(920.6932,170.2,-1502.136), native Spring field41591, 51 orbs.
- Brigandine tower3811: GUID1283766509, position(1550.402,278.1348,-216.7521), native Autumn field41585, 51 orbs.
- Both fields have environmental damage type0.
- Every orb retained its original frozen GUID, AP ID and position. Subsets are disjoint. All eight perturbations of each position by +/-0.01 on each coordinate retain the same field assignment.

The tower source is retained F9 capture5ebf44fff52e4347844ea8c1c2226d33 from2026-10-22. Raw identity status remains candidate_not_verified_across_worlds. Native field membership is not a live traversal proof, and the exports explicitly retain traversal_verified=false.

`identity-evidence.json` independently records installed dungeon and entrance definitions, localization offsets and workbook answers Q028/Q029. Main source5 is Labyrinth of Torrent and main source6 is Old Garden of Massacre; repeat205/206 are excluded. The assembly hash matches the existing audit and decoding produced zero errors. Workbook SHA256 was checked unchanged after the read.

Reproduce all three production data files with bundled Python running `docs/research/valley-20261022/export-catalogs.py` from any working directory. This asserts preservation of all30 prior milestone rows, adds nine rows, excludes age4004, rechecks native membership and frozen orb identity, and writes only the three new versioned catalogs. `extract-identity.py` and `extract-orbs.py` run from the workspace root to regenerate retained source evidence.
