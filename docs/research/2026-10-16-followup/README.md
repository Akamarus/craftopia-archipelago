# Follow-up progression research — 2026-10-16

**Runtime correction:** the [subsequent chest audit](../2026-10-16-closure/chests.md) qualifies the twelve general-chest pool matches: only the two iron items have positive selection at normal IslandLevel zero. All twelve retain live crafting-list evidence.

This follows the user's completed logic workbook and clarification about four age materials per region. It resolves source behavior from the installed build; it does not change AP logic, assign equipment tiers, install a mod, or alter a save. The original answered workbook is unchanged.

All investigations use assembly SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`, matching the installed game and diagnostic capture. Companion reports retain exact IDs, source conditions, hashes and limitations.

## Findings ready for progression design

| Question | Result | Evidence |
|---|---|---|
| Do regional age materials require upgraded axes/pickaxes? | Stone tools pass the rank comparison for all identified age-resource definitions. Do not add higher tool gates from item rank or regional order. | [Harvesting](harvesting.md), `harvesting.json`: 36 materials and 20 current tools |
| Are there always two pickups, one mined material and one chopped material? | Almost: 17 ordinary pickup sources, 9 mining sources and 10 axe-target sources. Suspicious Pumpkin6595 is also an axe target. This does not exclude alternative sources. | [Harvesting](harvesting.md) |
| Are the remembered enemy material sources real? | The specified skeleton, mage, goblin, sheep, chicken, phoechick and Reaper tables support them. Variants matter: leather and crystal rates are not uniform across levels/families. | [Drops](drops.md), `drops.json`: 851 enemy tables |
| Can loot equipment sources be filled without manually testing every item? | 185/241 loot-only rows have eligible shop/enemy-table evidence; 56 still lack sources in these channels. Twelve additional AssistEquip rows have chest-pool evidence. | [Equipment](equipment.md), `equipment.json`: all 429 review rows |
| Should normally excluded equipment be added because its ID appears in a drop table? | No. Wirbelwind I–IV have positive raw slots but fail the normal appearance filter. Preserve raw evidence without treating these as reachable items. | [Equipment](equipment.md), [Drops](drops.md) |
| Must every age be advanced at a different tower? | The tower menu enables evolution when the current tower is no longer under construction; evolution then checks the current age row, money and materials. No regional tower identity gate appears in these methods. | [Utility mechanics](utility-mechanics.md) |
| Is a used water bucket always returned? | The pouring action grants an empty bucket on a 30% random branch. Filling is supported directly; cooking consumption must be kept separate from pouring. | [Utility mechanics](utility-mechanics.md) |

## What is still needed

1. **Location and access joins:** map enemy variants, merchant catalogs and resource templates to actual placed instances and regions. A valid table is not proof that its producer is reachable in a short regional run.
2. **Renewal rules:** verify relevant spawner reset/respawn conditions before encoding an unlimited supply. The user's nighttime and regional observations remain useful reported evidence, separately from table definitions.
3. **Equipment choices:** assign explicit per-item AP tiers to the now-source-backed equipment, retaining exact IDs and distinguishing duplicate names. Resolve the remaining 56 loot-only sources and decide support for AssistEquip.
4. **A small operational test:** when naturally visiting a later region, try its age ore and wood with Stone tools. This validates the extracted behavior in play; a new character is unnecessary.
5. **Remaining utility bindings:** confirm the generator prefab's output and mounting eligibility, and whether converting filled buckets into recipe Water returns anything. Generic generator timing and pouring behavior alone do not settle these.

The next implementation can use the confirmed harvesting and source definitions while retaining explicit unresolved access rules. It should not turn a shop listing or enemy drop slot into unconditional regional reachability.

## Verification performed

- Selected harvesting source records and level13 component reads used byte-consumption validation; unrelated candidate decode errors are disclosed in the harvesting report.
- All 851 enemy-data reads consumed expected bytes with zero decode errors. Family-prefix reads are explicitly partial.
- Equipment asset/capture comparison: 13,299 matching scalar fields, zero mismatches. Chest arrays were joined by actual item identity.
- Cross-report review caught the normal-appearance filter on the four Wirbelwind IDs; those slots are now marked ineligible under normal settings.
- No runtime gameplay test or new software test result is claimed by this research batch.
