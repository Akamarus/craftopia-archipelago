# Owatatsu and Brigandine release candidate

One cumulative run can now end in Brigandine Valley. It includes Geezah,
Millewind, both Yarden towers, Owatatsu and Brigandine. This is the target scope
for the first public release, pending targeted gameplay validation.

| Ending | Checks without / with orbs | Victory |
| --- | --- | --- |
| Owatatsu | 87 / 275 | Owatatsu Tower stage3 AND Labyrinth of Torrent |
| Brigandine | 91 / 330 | Brigandine Tower stage3 AND Old Garden of Massacre |

The combined pool contains239 frozen soul orbs, including51 per new region.
All selected map markers appear immediately. Only non-progression rewards may
be placed at orbs because native region membership does not establish every
individual traversal requirement. No Sherbert or later-region checks are included.
Earlier tower/dungeon checks are included, but are not extra victory conditions.
Both final checks must be confirmed by AP. Repeat dungeon variants205/206 do not count.

## Progression

Both towers' repair chains require displayed Age5 (raw4). Age5 advancement uses
Yarden materials and20,000gold through the existing market route; a completed
earlier tower supports evolution. It does not require the later tower's own repair.
Age6 is not required or selected as a check in this profile.

Gold Ore is modeled in Owatatsu with a craftable Silver Pickaxe. This joins the
user's regional source answer with modern gold-rock/tool data; the exact placed
rock/tool interaction remains a targeted live check. Bright Red and Ultramarine
Herbs use direct regional gathering confirmed by the user on2026-10-22 and backed
by native recurring pickup data. No crop barter route is assumed.

Linen Wool is craftable. Machine Factory and Furniture Workbench construction
and their recipes are included. Existing Smelting, Cooking, Farming and Equipment
Care permissions cover the recursive chains; no invented manufacturing permission
is added. Human feces production must remain enabled for the existing Saltpeter route.

Both new dungeons use a craftable Gold Sword and five progressive Sword receipts
as an explicit combat sufficiency assumption. This is not a proven minimum, and
alternative combat builds have not yet been modeled. Their native entry definitions
show no environmental damage requirement. Regions are not artificially tower-gated.

## Compatibility and release validation

The additive milestones_20261022_r4 catalog preserves every older row and contract.
Existing slots retain their original selected checks. Use the matching new mod and
APWorld for new endings. A generated seed cannot be expanded in place: the combined
seed is a replacement prepared before the new run starts, without editing saves.

Before calling this the public release, verify new tower/dungeon transitions and
reconnection behavior, Gold mining, manufacturing recipes, and practical dungeon
combat during the combined run. Multiplayer host/guest validation remains separate
and unverified; do not advertise tested multiplayer support.

Sources: [identity and orb audit](research/valley-20261022/orb-membership.md),
[material audit](research/valley-materials-20261022/README.md), and the user's herb
confirmation. No installation, server start, GitHub publication or Workshop upload
is implied by preparing this candidate.

## Automated verification — 2026-10-22

The Brigandine suite passed100 actual AP0.6.8 fills with full reachability spheres,
alternating orbs on/off, plus boundary, dependency and final-goal tests. Existing
regional/pilot tests passed17 tests, including100 Geezah fills. Yarden catalog/slot
boundaries passed. Milestone tools passed12 tests; packaging passed3 tests. Native
catalog and map tests passed, including239 selected markers and older137/88-marker
sessions. The Release DLL built with zero warnings/errors.

The isolated example generated330 checks and passed beatability with goal3052+4105.
These automated checks do not replace the live validations listed above.
