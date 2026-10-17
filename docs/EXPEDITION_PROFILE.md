# Expedition playtest — 20261015_r1

This is the first implementation batch from the game-wide audit: all 125 captured
mission instances, full catalog equipment progression, eight skill unlocks, and
ten production permissions. It is an **experimental playtest**, not verified
game-wide logic. Use a new seed and character. Existing opening and legacy seeds
retain their rules and delivery behavior.

Current pool: **88 equipment unlocks + 8 skill unlocks + 10 production unlocks
+ 19 Skill Points = 125 items**.

## Implemented contract

- Select `logic_mode: expedition`, `mission_catalog_version: 20261007_r1`, and
  separate slots. Numeric mission IDs and their frozen AP locations are unchanged.
- Every mission is distinct, including missions sharing a class. The pool and
  routing allowlist contain exactly 125 locations.
- Equipment receipts advance through each category's distinct captured recipe
  tiers. Empty tiers are skipped, but actual age, resource, recipe, and skill
  requirements remain enforced by the game. All eleven equipment categories are
  represented. The bridge sends the existing absolute-tier permission contract.
- Tier schedules travel in slot data. Old seeds without schedules still advance
  one tier per receipt. Invalid schedules withhold new equipment delivery.
  Durable receipt history continues to determine replay behavior.
- All eight skill-tree items and ten production items are included. Remaining
  slots are Skill Points. Money/material bundles remain excluded until their
  transactional delivery path is ready.
- Tamer uses only captured vanilla missions: prism, pet-management machine,
  Mono/Cow capture, and Mono taming. The optional Tamer mod is not required and
  contributes no AP checks or additional progression assumptions.
- Objective-count victory uses actual confirmed mission checks. The suggested
  playtest target is 60 of 125; boss and age goals remain unavailable.

## Logic and its limits

`Expedition.py` explicitly opts into an experimental prerequisite model. It cannot
be loaded through the reviewed-profile path. Rules cover known permission
boundaries such as smelting, copper shields, farming/cooking, Heal, and refinement.
Some extra gates are conservative assumptions (including Tamer access for pet
management, clothing tiers, and automation construction); they are not claims
about exact vanilla prerequisites.

Hunts at levels 1/10/20/35 use alternative combat routes at catalog tiers 1/2/3/4,
or vanilla Tamer permission with 1/2/3/4 AP Skill Points. These are **test pacing
assumptions**, not measured combat requirements. Dungeon combat uses tier 3.
Physical access, enemy spawns, pet availability, native skill point sources,
mission prerequisites, and age-resource chains are not fully modeled.
Missions assigned to `field` have no modeled AP permission requirement; that
does not mean they are all immediately achievable in the game.

The equipment catalog includes unsupported loot-only gear. Uncaptured station
families still fail closed; later stations may need runtime identification before
their missions work. Passing AP fill proves consistency of these rules, not
Craftopia beatability. Keep an obstacle log during the playtest and collect
evidence before changing blocked station mappings or dependencies.

## Remaining implementation batches

1. Expand runtime captures for mission targets/prerequisite links and unresolved
   station families; refine this profile from actual play.
2. Add stable instance catalogs and adapters for soul orbs, overworld chests,
   challenges, and quests, with replacement-reward transactions.
3. Add tower/age/dungeon/boss evidence and configurable goals.
4. Add Anubis and finite shop checks plus useful transactional material/money
   delivery. Test multiplayer separately after the single-player path.

No invented IDs for these future categories are added to the seed. The broad
audit remains the implementation inventory; this batch does not complete all
46 proposed check families.

## Verification

Real AP 0.6.8 tests exercise 20 deterministic fills, full-pool reachability under
the modeled rules, objective completion, equipment counts, permission gates,
alternative hunt routes, slot-data routing, and rejection of shared mode.
Bridge tests cover sparse tiers, replay, and malformed schedules alongside the
existing progressive-equipment regression tests. Live validation is pending.

Validation on 2026-10-15: 31 world tests, 28 equipment bridge tests, and 51
delivery/goal/activity/integration regressions passed. A separately generated
seed was decoded and exercised through the real bridge: all 125 numeric routes,
the 60-objective threshold, all equipment schedules through their maximum tier,
receipt replay, and restored durable history passed. Independent source review
found no actionable regressions; it did not independently rerun the suites.
