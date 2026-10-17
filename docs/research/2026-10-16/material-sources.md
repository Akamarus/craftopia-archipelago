# Material acquisition audit

This complements the recipe audit: a recipe age is not a universal acquisition age. Purchasing, harvesting, combat drops and generator output must be modeled separately. Evidence is the October 16 schema-5 capture and the October 15 snapshot of the [map author's dataset](https://kregap.github.io/craftopia-map/). `material-sources.json` preserves 33 relevant loaded shop entries and 857 external map leads; `extract-material-sources.py` reproduces this extraction offline.

## Claims that cannot become progression rules yet

| Supplied claims | Audit result |
|---|---|
| Stone/wood/sand are hand-harvested at Age 1 | Plausible routes, but the supplied tool/age assertions are not established by item recipe fields. Exact captured wood name is Log. Shops also list Log and Stone at raw required world level 0. |
| Copper, Silver, Gold, Platinum and later ores require particular pickaxes and ages | No harvest-tool thresholds were captured. Map ore/bedrock labels establish candidate locations, not minimum tools. Ingot crafting ages do not prove ore acquisition ages. |
| Titanium, Steel, Mythril and Orichalcum progression | Exact item IDs are required. The active observed series includes Silver and Mithril; do not silently replace them with different named materials. Refer to the recipe and age tables. |
| Straw and Clay are hand-foraged at Age 1 | Not proven as exclusive or sufficient routes. Straw is also present in a loaded shop definition at raw level 1. |
| Scrap Parts come from mobs/ruins | Drop/salvage route unverified. Two loaded shop definitions include Scrap Parts or related manufactured materials; their access still needs mapping. |
| Wool, Leather, Fangs, Feathers and Bones have specific enemy drops | Animal markers exist, but enemy presence does not prove a drop table, quantity or renewal rule. Preserve the user's successful gameplay observations separately from a complete drop audit. |
| Sulfur and Crystal require Iron Pickaxe/Age 3 | Both appear in shop definitions at raw level 0, including farming trade lists. This makes the claimed universal mining gate unsafe; the shop location and trade currency still need verification. |
| Fire Organ, Obsidian and Monolithic Shard have asserted sources | No verified item/source pairing established here. They are not substitutes for the actual tower requirements. |
| Bright Red/Ultramarine Herbs require Owatatsu/Age 4 | External map has 22 and 30 marker rows respectively. Farming shop definitions list both at raw level 0. These are alternatives to investigate, not proof of opening access. |
| Holy Water is only purchased from Anubis or obtained in chests | Contradicted as an exclusive route: active Holy Water 5565 has a recipe of Water×5, displayed Age 6. A limited-dungeon shop definition also lists it at raw level 9. The capture does not bind that shop to Anubis. |
| Rare Leather/Rare Feather/Fluffy Wool require specific high-level species | Exact drop-table evidence still needed; names and tower costs alone cannot identify the species. |
| Desolation Sand is a desert requirement | Not present in the captured Unnefell tower costs. Do not invent a tower gate around it. |

## Resistance ingredients: alternatives matter

The map dataset has 51 Snow Mint rows, 9 Hell Pepper, 26 Yellow Herb and 1 Poisonous Mushroom. Counts are dataset rows, not guaranteed unique harvest nodes or an exhaustive census. Ore rows can represent an area rather than an individual deposit. Coordinates are preserved without assigning them to the nearest tower.

Loaded shop definitions `SoShopList_OW_Overwrite_farming_05` and `_06` both list Snow Mint 3445, Hell Pepper 3360, Bright Red Herb 723 and Ultramarine Herb 735 with `_requirWorldLv=0`. This is concrete evidence of configured trade alternatives. It **does not establish** where the merchants are, whether stock is available, what the barter costs are, or whether their locations require protection. In particular, the `_overwritePrice=10` field must not be described as ten gold without resolving the cashier.

Other useful configured alternatives include Bioethanol in farming_04, Sheep Wool/Bird Feather in farming_03, and Engine Parts/Fuel Tank in ordinary and farming lists. These can change accessibility substantially once merchant access and currency sources are established.

## What the source evidence does and does not prove

- Shop rows come from `world_evidence.shop_catalog.rows[].fields._saleInfos.items[].fields`; raw required levels are preserved, not relabeled as displayed ages without checking the shop's own code path.
- These are loaded assets, not a complete world shop census. Merchant positions, stock renewal and cashier costs are unresolved.
- Map markers are the author's geographic leads, not game item IDs, biome boundaries, authoritative drop tables or AP access rules.
- Neither an absent recipe nor an absent marker establishes impossibility. Likewise, an item or enabled transition present in data does not establish obtainable released content.
- Battery generation and Elemental Dust acquisition remain important unresolved dependencies. A Monster Prism is not established as a consumed Battery recipe ingredient.

The next source work should target merchant placement/currency, generator prefab output, harvest/drop tables and biome boundaries. Those are the missing links between verified recipe definitions and a defensible reachability model.
