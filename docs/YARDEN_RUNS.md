# Experimental Yarden endpoints

Use regional logic with `ending_region: yarden_mountain` or `yarden_ruin`.
These are experimental profiles pending targeted live validation, not evidence of
a completed Yarden playthrough. No existing slot is migrated.

| Ending | Cumulative checks | Victory | Count without / with orbs |
| --- | --- | --- | --- |
| yarden_mountain | Existing Geezah/Millewind checks, Mountain stages3020–3022, Age4 check4002, Story dungeon4102 | Mountain Tower stage3 AND Ruin of Hunters (Story3) | 78 / 215 |
| yarden_ruin | Mountain selection plus Ruin stages3030–3032 and Story dungeon4103 | Ruin Tower stage3 AND Old Garden of Storms (Story4) | 82 / 219 |

The r2 profiles include137 orbs:51 Geezah,37 Millewind and49 Yarden. Both Yarden
towers share one native field, so both endings include all49 Yarden orbs. See
[membership evidence](research/yarden-20261021/orb-membership.md). New regional missions
are not yet selected. Orb checks remain non-progression.
Repeat dungeons203/204 cannot satisfy the Story checks. Both goals must be confirmed
by the AP server before runtime victory. Earlier-region checks are included, but
their completion is not an additional victory condition.

## Material and combat model

- Silver: Yarden access and craftable Iron Pickaxe58. Captured recipe:
  Workbench/raw age2, Iron Ingot1 and Log1; the tool remains vanilla equipment.
- Sulfur: player-confirmed Geezah caves/normal mining, using the existing ordinary
  mining capability. No invented later-region requirement.
- Saltpeter: Feces2048 through Aging Facility/raw age2, Equipment Care. This model
  uses the confirmed renewable human-feces route with stool production enabled.
  Require `DisableStool=false` and `settings.NotStool=false` (runtime
  `Option_NotStool=false`); both natural production and toilet use check these.
  Do not disable stool production for these experimental runs. The player also
  confirmed animal sources; their exact prerequisite chain is not yet modeled as
  an alternative. The mod does not change game settings. AP Farming is not required
  for feces itself; it is required for the wheat/bioethanol/rubber chain.
- Age4: audited Hills materials and2500gold, via the completed opening tower.
  Age3 is a prerequisite; no Yarden completion/age circular dependency.
- Mountain repairs: exact audited costs. Silver Ore is not forced to Age4 merely
  because of its metadata. Crystal uses an experimental Iron Sword wizard combat
  route; Bone/Dust use the existing skeleton route.
- Ruin repairs: audited recursive recipes, including Smelting, Equipment Care,
  Farming and Cooking. Age4 is required by Silver Ingot, Rubber and manufactured parts.
- Both dungeons use craftable Silver Sword47 (four progressive Sword receipts) as
  a conservative combat assumption, not a measured minimum or physical entry
  restriction. Recipe: Blacksmith/raw age3, Silver3/Log2/Leather1. No new armor
  requirement is imposed. Alternative combat builds are not yet represented.

Source evidence: [Yarden audit](research/yarden-20261021/README.md), player source
confirmations dated2026-10-21, and diagnostic capture
`0312114d9ed4418f839ef6e7d2b126d9` from2026-10-16. That capture supplies exact
tool/sword recipes and tower GUID/definition/position. Its SHA256 is in the new
catalog provenance. Runtime tower matching uses definition plus position, with
the captured GUID serving as catalog key, following the existing tower policy.

## Compatibility and next live packet

The additive `milestones_20261022_r3` catalog retains all previous rows unchanged.
Old profiles keep their old milestone and equipment contracts. The updated bridge
and mod must be used for Yarden sessions; older builds cannot interpret this catalog.
This source update does not install, publish, enroll a character or replace a seed.

Use an explicitly prepared diagnostic session with the advanced character. Capture
both Story entrances and saved completion state. Clear only a dungeon whose
completion event is still missing; verify source3/4, one credit and persistence on
reload. Read tower stage state first and request a repair only if the identity/hook
remains uncertain. No full fresh-character regional replay is required.

## Initial r1 verification — 2026-10-22 (before Yarden orbs)

- Yarden: four tests passed, including100 real AP0.6.8 fills per ending with full
  reachability spheres, alternating optional orbs on/off.
- Existing regional/pilot tests:17 passed; Millewind:5 passed, including its100 fills.
- Milestone catalog/export/version tests:11 passed. Native catalog harness passed
  legacy/current/Yarden loading and bind dispatch, including negative Mountain source
  and rejection of repeat dungeon identities.
- Mod Release build succeeded with zero warnings/errors.
- Isolated example generation produced166-check Mountain and170-check Ruin sessions,
  with the expected goal pairs and successful beatability assertions. Examples are
  retained locally under `artifacts/yarden-validation-20261022/`; no server was started.

AP used its optional-speedup pure-Python fallback. Live dungeon and tower validation
is still pending; these results validate the implementation and modeled progression.

## Advanced-world saved-state check — 2026-10-22

Capture `5ebf44fff52e4347844ea8c1c2226d33` from the loaded advanced world confirms
Story dungeon3 (Ruin of Hunters) and4 (Old Garden of Storms) already cleared.
Repeat203/204 are separate entries. Both Yarden towers are already at stage3.
Their definition IDs and positions match the catalog; their world-specific GUIDs
differ from the earlier capture, consistent with using definition/position matching.
See [extracted evidence](research/yarden-20261021/live-state-20261022.json).

Do not request a repeat clear or repair just to confirm these saved states. This
validates loaded completion data, not a fresh first-clear event or AP delivery in
an enrolled Yarden slot. Those transition checks remain pending until an appropriate
uncleared test world is available; this character need not be reset.

## Expanded r2 verification — 2026-10-22

The expanded pool passed 100 real AP fills per Yarden ending (200 total), with full
reachability spheres and optional orbs alternated on/off. Existing regional tests
passed (17 tests), as did the focused Millewind cumulative check. Export and package
tests passed. Native marker tests verified all 137 selected markers and compatibility
with older 88-orb sessions. The Release mod built with zero warnings/errors.

A 219-check Ruin seed generated with the expected tower/dungeon goal pair and passed
beatability and archive checks. Fresh in-game transitions remain untested.

