# Explicit loot equipment tiers

The user selected explicit per-item tiers on 2026-10-16. A weapon's star rank,
rarity or loot-only workbench level is not an unlock tier. Each item needs its
own acquisition evidence and balance decision; stronger variants are separate IDs.

## First mapping

New contract `equipment_loot_20261016_r1` (equipment protocol 2) adds only:

| Item | ID | AP requirement | Additional world-age gate |
| --- | --- | --- | --- |
| Goblin's Club | 6031 | Sword tier 2 | None (internal age minimum 0) |

This is an explicit experimental balance rule, not a native game requirement.
The tester obtained the club in the opening playthrough before reporting dungeon
completion. The screenshot showed its one-handed Sword category and 100 ATK / 50
MATK. The 2026-10-16 milestone capture confirms that name, ID and metadata.
Its physical attack lies between Copper Sword (50) and Iron Sword (150); this
comparison supports a provisional second Sword receipt reward without forcing
an unsupported age requirement. It does not prove equivalent combat strength.
Goblin's Club II-IV (6032-6034) remain unsupported pending acquisition review.

The runtime makes a separate copy of the frozen catalog and changes only 6031.
All live metadata checks, category ownership, loadout checks and enrollment
requirements still apply. Unknown items remain blocked. No drop is granted by
this rule, and the progression graph does not assume a random club drop.

## Compatibility

The original equipment catalog and its file are unchanged. Existing seeds use
the original rules, including the unsupported club. Newly generated regional
profile `regional_geezah_20261016_r2` selects the new contract. Other profiles
continue to select protocol 1. A session cannot switch catalog identities after
binding. Protocol 2 requires the updated bridge and mod v1.12.1; older mods hold
delivery rather than activate the new contract. The mod still advertises base
protocol 1 plus a maximum capability of 2 for old-bridge compatibility.

This update is prepared in source/build output, not installed into the live
game. Never edit a played seed's equipment identity to activate it.

## Remaining audit

`tools/audit_equipment_gaps.py` compares the frozen catalog with a matching-build
capture and produces `equipment-review-20261016.json`, an inactive review queue:

- 241 loot-only entries with matching eligible metadata (including 6031).
- 169 entries with unavailable metadata, which is not proof they are obtainable.
- 19 unsupported combat-support entries.

The queue intentionally retains null proposed tiers and ages. The implemented
6031 rule above is separate from this raw audit. Per-category craftable stats
are included for comparison, not automatic tier assignment. Input hashes are
recorded; world, account and character details are not exported.

Regressions cover locked/tier-1/tier-2 club use, changed metadata, untouched
variants and old catalog, session contract switching, old/new client capability,
malformed contracts, and audit privacy/identity checks. A live equip/unequip and
reload test remains necessary when the new regional session is installed.
