# Dynamic sources and dungeon blueprints — 2026-10-16

**Runtime correction:** the [subsequent audit](../2026-10-16-closure/itemoffer.md) establishes that fresh normal ItemOffer quests cannot select the three weapons' high reward tables. Island-level table indices below are not reachable world-age stages in normal current gameplay.

Continuation of the [remaining-source audit](../2026-10-16-remaining/README.md). This pass traced additional addressable assets, dynamic NPC creation and subworld entry bindings. It resolves important sources that searches of ordinary world spawners and general reward tables missed. No gameplay, save, workbook, mod, APWorld or equipment-tier changes were made.

## Resolved acquisition chains

### All seven accessory blueprints

Blueprints6801–6807 have exact fixed-chest rewards in seven cave prefabs. Each chest is active, forced to spawn and registered in its subworld's gimmick list. Each cave also has an active elevator entry in the installed world scene, linked through the subworld manager/list and addressable catalog to the same prefab. This is stronger than merely finding an unused reward definition.

| Accessory | Blueprint | Cave ID |
|---|---:|---:|
| Expansion Magazine II5681 |6801|25|
| Quickloader II5682 |6802|21|
| Custom Grip II5683 |6803|19|
| Bipod II5684 |6804|24|
| Laser Sight II5685 |6805|22|
| Sling Belt II5686 |6806|23|
| Resin Stock II5687 |6807|17|

The [blueprint report](blueprints.md) records entry object IDs, saved restart coordinates, exact reward objects, recipe constraints and load methods. Cave traversal, localized names, environmental requirements along the route and earliest access are still separate questions. The earlier statement that these blueprints lacked an identified source is superseded.

### All fourteen late medal-shop items

The trader is summoned by the craftable **Medal Trader's Table3522**, not an ordinary fixed dungeon NPC. The exact item prefab reaches the summon component, which applies a seeded random selection among ten medal catalogs. With this finding, all67 previously shop-backed equipment entries have a demonstrated world-placement or instantiation chain.

The table requires displayed recipe Age10, Log x5, Orichalcum Ingot x1 and Iron Ingot x1. Its offers require raw world level9 and cost base50 Red Boss Challenge Medals each. The trader cycles on day-end events; one summon does not guarantee every catalog.

The challenge gate also has a live Age10 recipe, requiring Goddess's Rune x1, Stone x100 and Orichalcum Ingot x10. Exact challenge records establish red-medal awards: Dragon level80 gives3, level100 gives5, level120 gives10, with additional hazards on higher challenges. Ingredient acquisition and combat viability still matter.

See [dynamic merchants](merchants.md) for all14 IDs, the complete prefab chain, recipes, summon timing and challenge conditions. This supersedes the earlier uncertainty about the merchant's instantiation.

## Conditional findings, not yet reachable sources

**Sting1723, Mjolnir1724 and Sharp Shooter6162** have verified NPC-prefab reward chains. Required-item tables link to weighted reward tables; the ItemOffer NPC code selects and awards the item. However, the relevant NPC enums were not found in the audited5,628 loose spawners or181 bundle spawners. The ordinary NPC-summoning-table dictionary also does not list these enums. Their encounter route remains unresolved.

The [equipment report](equipment.md) retains these three conditional sources separately from the **48 ordinary equipment items still lacking a demonstrated source**. This split does not mean the three are now safe to use in progression logic. The original51 are now48 without a source plus3 with conditional chains and missing encounters.

## What implementation can use next

- Replace the seven unknown-blueprint-source records with exact cave-entry/chest bindings, retaining unverified traversal constraints.
- Replace the fourteen unknown-merchant-instance records with the table/currency/challenge dependency chain.
- Keep the three ItemOffer rewards conditional and the48 unsupported acquisition routes out of required progression until actual access is established.
- Assign explicit AP tiers only after acquisition and regional access review; recipe age, item rank and shop existence do not automatically define those tiers.

## Verification and limitations

Companion JSONs preserve exact IDs and evidence. The expanded equipment scan decoded6,882 selected objects, including reused prior sources, with full byte-consumption checks. Decode/identification failures and unscanned bundle categories are disclosed in the reports. The blueprint investigator independently verified all seven chest hierarchies, subworld registration and catalog/entrance joins. Merchant evidence traces both the item/equipment prefab and runtime overwrite calls.

No live chest collection, NPC summon, trade or boss victory was performed. External historical item guidance is not substituted for installed-build evidence; the blueprint report explicitly separates the publisher's legacy-era boss-drop description from the current fixed-chest route.
