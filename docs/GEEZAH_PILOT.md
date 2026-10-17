# Geezah milestone pilot — v1.11.0

The isolated `geezah_pilot` profile contains 50 checks: 36 early missions,
nine mission-category reward claims, three Geezah tower repair stages,
Agricultural Age, and Ruin of Beginning. Victory requires AP confirmation
of the dungeon's first clear (location 4100). Later regional tower-and-dungeon
AND goals are supported by the wire contract but not yet selectable presets.

Categories: Gather Trees, Gather Rocks, Glider, Combat, Market, Building,
Rookie Farmer, Basic Item Crafting, and Workshop. Claim the native category
reward after finishing its member missions. Native rewards remain intact.

Smelting can only be placed on the same player's 17 selected opening missions.
Generation rejects an empty eligible set. The pool supplies Smelting, Cooking,
Farming, two Sword progressions, Bow, Body armor, vanilla Tamer, and Skill Points.
Soul Orb checks are omitted until individual Geezah positions have a reviewed
region assignment; nearest-tower candidates alone are insufficient. Respawning
chests, Anubis purchases, and late-region checks are excluded.

## Evidence and limitations

This is an **experimental playtest**, not a proof of physical beatability.
Category membership comes from the complete 125-active-mission capture on
2026-10-16, with 165 loaded category definitions. Unused categories with no active
membership remain research only. The frozen catalog records the assembly and
capture hashes. Tower costs and source identities come from captured definitions;
Millewind stage 1 and Frontier advancement were observed live. Geezah stages,
Agricultural advancement, and category claim observers still need live acceptance.
Ruin of Beginning was identified by the user and its clear list changed [] to [1].

Local sand, wool/leather, copper ore, and placeable torch availability are
playtest assumptions. The age and dungeon routes are conservative capability
estimates; combat difficulty and vanilla resource routes remain to be tested.
No cold/heat-region access is assumed. Placeable torches are separate from
equippable weapon permissions.

Use a **fresh character and fresh world** for this pilot. First binding rejects
already-completed selected milestones, unreadable state, or an unloaded first
tower. Existing matching bindings allow normal reloads. The world fingerprint
compares creation time and world seed; byte-identical copied worlds cannot be
distinguished and are unsupported. Do not copy saves between pilot sessions.

Only successful native transitions are observed. Loading, repeated dungeon
clears, failed category claims, and sampled stage changes do not grant checks.
The bridge persists observations before acknowledgment and computes victory
from selected AP-confirmed locations. A socket reconnect does not repeat its
victory announcement; a full bridge restart may resend AP's idempotent goal status.

If storage fails after native completion, a bounded in-memory retry is retained
and further processing holds. A possibly partial outbox write is not overwritten.
Keep the game/session and evidence for recovery: a crash before durable recording
can lose the observation. There is no arbitrary retro-award of save history.

## Validation

- 147 bridge unittest cases and both legacy asynchronous client scripts pass.
- 40 world tests passed before two additional regional restriction tests;
  all six regional tests pass, including ten actual AP 0.6.8 fills.
- Six milestone catalog/export tests pass.
- Milestone transition/baseline tests: 56 assertions pass.
- Observation persistence, enrollment regression, and collector suites pass.
- AP mod Release build: zero warnings/errors.
- Fresh review caught the pre-completed-world admission problem and reconnect
  announcement duplication; both now have guards and regression coverage.

Tests do not substitute for live hook validation. Runtime binding and hook adapters
still require the controlled pilot below. Later-region source rows in the catalog
are not selected in the Geezah seed.

## Live sequence

1. Establish and enroll the new pilot character/world; collect nothing beforehand.
2. Complete an opening mission category and claim its native reward. Expect its
   mission checks plus exactly one category check.
3. Reload/reconnect and verify counts do not increase from repeated claims.
4. Repair the Geezah tower and advance to Agricultural Age as play progresses.
5. Clear Ruin of Beginning: one dungeon check and victory after AP confirmation.
6. Repeat/reload the dungeon and confirm no additional check or duplicate reward.

Keep the older CraftopiaOrbs session and its saves intact. The new seed is separate.
Anubis diagnostics now capture the active exchange UI correctly (observed exchange
counter 11, next reward 12); AP Anubis checks remain disabled pending a successful
exchange and reload verification. No orb farming is required for this pilot.

Installed locally with the game closed on October 16. DLL SHA-256:
`7E87F8B931714A20C0D6E0B70FFD0DA3E819C2A99B8621E8C9417D168BA4BB6C`.
The separate seed is generated; baseline enrollment and live acceptance are pending.
