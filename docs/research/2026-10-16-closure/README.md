# Remaining-source runtime audit — 2026-10-16

This pass checks whether the previously found source tables can actually run in the installed normal game, and expands the asset search. It makes no production, game, save, workbook or AP tier changes.

## Findings

- **Three conditional weapon routes are blocked for newly generated normal ItemOffer quests.** Sting (1723), Mjolnir (1724) and Sharp Shooter (6162) occur in high reward tables, but those tables require an island-level value unavailable in normal current gameplay. Creative/debug overrides are separate exceptions. This corrects the earlier abstract table-level interpretation; it does not prove the items globally unobtainable. [Runtime evidence](itemoffer.md)
- **Ten higher-tier general-chest matches need the same correction.** Only Iron Gauntlet and Iron Glove have positive pool selection at the normal island-level value. All twelve reviewed gloves/gauntlets have live crafting-list entries, so their crafting evidence survives. [Chest and recipe comparison](chests.md)
- **The omitted static-placement group yielded no new target sources.** The census covers 10,891 bundles; all 1,557 placement payloads, 55,022 instances and 436 referenced static prefab trees were decoded. Render-only classes were inventoried with representative payloads, not exhaustively decoded. [Coverage and limits](bundles.md)
- **The 48 equipment IDs are classified as Legacy (presumed carryover), by user decision.** They are no longer an active research task and are excluded from required progression planning. Import-only provenance remains unproven; the underlying evidence is preserved. [Exact-ID classification and historical evidence](history.md)

Each report has a companion JSON file containing structured evidence. Earlier seven blueprint chains and fourteen medal-trader offers remain documented in the [preceding audit](../2026-10-16-deep/README.md).

## Still outstanding

1. Validate full station/material access for late crafting routes before assigning region logic or explicit equipment tiers.

The 48-item source hunt, including the proposed dynamic-child scan for those items, is parked. Reopen only if direct current-acquisition evidence appears or the user requests it.

The three named weapons are tracked separately from those 48 because their source tables exist, but the identified normal route is blocked. Do not turn the island-level table indices into world-age requirements.
