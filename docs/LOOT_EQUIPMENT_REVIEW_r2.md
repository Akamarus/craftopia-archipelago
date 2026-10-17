# Reviewed loot equipment contract r2

`equipment_loot_20261016_r2` uses equipment protocol 2 and adds exactly one
decision to the historical r1 overlay:

| Exact item | ID | Category | AP tier | Additional minimum age |
| --- | --- | --- | --- | --- |
| Goblin's Club (retained) | 6031 | Sword | 2 | 0 |
| Goblin's Bow | 6187 | Bow | 2 | 0 |

The bow's captured 35 ATK / 17 MATK is close to Copper Bow 93 (30 / 15,
Bow tier 2), and below Iron Bow 94 (100 / 50, tier 3). This is an explicit
experimental balance choice, not a rank, rarity, enemy-level or island-level
conversion. Minimum age 0 deliberately imposes no additional AP age gate;
it is not a claim that earliest acquisition was proven at world age 0.

`research/2026-10-16-followup/equipment.json` records exact item 6187, status 1,
current data, usable prefab 4, inventory tab 1, weapon category 6, slot 2,
noncraftable workbench family 20001 and workbench level 0. Enemy data 538 and
765 have quantity 1 in slot 2 at approximately 0.1 base rate, passing the
normal item filter. `research/2026-10-16-remaining/spawns.json` records enabled,
active Tutorial Goblin_Archer placements 51291 and 51345 at fixed level 10,
120-second repeat intervals and no time/manual conditions. Level selects the
base enemy table; it does not supply an equipment age gate. These are configured
source routes, not a promise of a drop or a completed gameplay accessibility test.

The original catalog and r1 parser remain independent. Only regional profile
`regional_geezah_20261016_r4` selects r2; prior r2/r3 regional profiles retain
the original reviewed contract. Session identity cannot switch between them.
Bridge forwarding and mod binding recognize both reviewed identities at protocol
2. A prior mod that does not recognize r2 will reject that session contract.

Goblin's Bow II-IV, Wizard's Staff, unsupported AssistEquip, and all 48 exact
IDs in `research/2026-10-16-closure/history.json` remain unsupported. Active
Silver Bow 95 remains progressive; same-name 6161 remains unsupported. No
name-based matching or automatic mapping of a whole family is used. Random
drops are not added as mandatory logic requirements.

Regression coverage checks category ownership, tier boundaries, age-zero use,
live metadata mismatch, frozen base/r1 behavior, all 48 historical exclusions,
contract forwarding and profile selection. A live pickup/equip/reload test is
still needed when the user runs a newly generated r4 session. This change does
not install a mod or alter a save.
