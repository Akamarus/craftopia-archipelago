# Remaining acquisition and access research — 2026-10-16

**Follow-up:** [Dynamic-source and blueprint research](../2026-10-16-deep/README.md) resolves all seven blueprint chest/entry chains and all fourteen medal-merchant instantiation routes. The findings below preserve this earlier audit's state; use the follow-up for current conclusions.

This continues the [previous audit](../2026-10-16-followup/README.md), targeting unresolved equipment, merchant placement, enemy renewal and utility production. It uses the same installed build and diagnostic capture. These are research findings, not applied AP progression changes or gameplay test results. The user's answered workbook is unchanged.

## What this pass resolved

| Area | New result | Detail |
|---|---|---|
| Remaining56 loot-only rows | One explicit quest reward; four temporary skill weapons identified;51 ordinary items still unresolved | [Equipment](equipment.md) |
| Seven unsupported accessories | Exact blueprint-to-recipe mappings and ingredients; obtaining the blueprints remains unresolved | [Equipment](equipment.md) |
| Existing67 shop-backed equipment items |53 now link to active world-scene merchant placements.14 belong to late LimitedDungeon shops with unresolved instantiation. | [Merchants](merchants.md) |
| Environmental potion herbs | Active traders sell Snow Mint/Hell Pepper and two special herbs for Tomatoes or Pumpkins, base10 each, with raw purchase-level0. Crop acquisition and travel remain dependencies. | [Merchants](merchants.md) |
| Early material enemies | Actual active placements, time conditions and repeat configurations identified for skeletons, animals, goblins and ordinary Wizards | [Spawns](spawns.md) |
| Small generator | Prefab2000 explicitly produces Battery2100; non-pet enemy and player mounting paths identified | [Utilities](utilities.md) |
| Water cooking | Inspected automated cooking path consumes filled buckets without creating empty ones; replenishing empty buckets is the supported conservative route | [Utilities](utilities.md) |

The merchant figures refine existing source evidence; they are not53 newly discovered equipment items. Likewise all four temporary swords are real items, but should not be treated as permanent loot progression.

## Important corrections for implementation

- **Demon Sword Sakura-fubuki1882:** the99-petal quest is not its full prerequisite. Its predecessor requires player level33 and999 each of Iron Ore, Sand and Ceramic. Do not encode a simple99-petal route.
- **Summoned swords3300–3303:** Unlimited Fake Blade creates their projectile/pickup route; special inventory handling clears them on unequip/drop. Their vanilla skill level is not an AP equipment tier.
- **Accessories5681–5687:** historical absence from the captured craft menu is explained by blueprint requirements. Recipe existence still does not prove that blueprints6801–6807 are obtainable.
- **Herb traders:** they use crop items, not gold. Buying outside cold/heat regions can be a viable route candidate, but only with the appropriate crops and a verified route to the trader.
- **Renewal:** modern skeletons use managed-reference time conditions; legacy night flags alone are misleading. A negative spawn interval alone is also not proof of one-time availability.
- **Small generator:** batteries come from operation, not a free hand-crafting recipe. Creature eligibility, mounting duration and production modifiers matter.

## Exact remaining scope

1. **51 ordinary equipment IDs** are listed in [equipment.md](equipment.md#still-unresolved-ordinary-loot-rows). No source was demonstrated in the inspected quests, special lotteries, fixed chests, mission rewards, alchemy, supply/boss rewards or material gacha, in addition to earlier shop/drop/general-chest searches. This does not prove global unobtainability; additional addressable/dynamic paths remain possible.
2. **Blueprints6801–6807:** no acquisition route in the audited channels. Keep their recipes conditional.
3. **14 LimitedDungeon shop items:** catalogs, level9 requirement, Red Boss Challenge Medal payment and randomized catalog selection are known; actual merchant instantiation/location remains unresolved.
4. **Regional access joins:** internal field IDs, active scene ancestry and coordinates are recorded. Localized region labels, safe traversal, runtime activation, currency production and AP requirements must be joined before treating these as reachable routes.
5. **Balancing:** explicit per-item AP tiers remain a design task; this audit deliberately does not assign tiers from rarity, recipe age or nominal item rank.

## Useful optional playtests

These can use the existing character and need not interrupt the current run:

- When reaching the Spring or fifth-biome farm trader, verify the crop payment and available herb stock in the UI.
- Observe a nearby night skeleton or animal respawn after killing it; serialized timers are game-update conditions, not guaranteed real-time deadlines.
- If any of the51 listed equipment items or seven blueprints is obtained naturally, capture its exact source and name. This is now more useful than testing generic drop families already resolved in data.

No additional user answer is required to retain or use these findings. Further implementation should consume confirmed routes and keep unresolved sources explicit.

## Evidence and verification

Each topic has a companion JSON with exact item/object IDs and source conditions. The equipment audit covers1,158 byte-checked selected objects and all63 target IDs. Merchant evidence compares68 catalog-route records with no differences and resolves21 active placements, excluding two test copies. Spawn evidence includes2,143 selected records, of which1,772 have active serialized ancestry;371 inactive rows are retained but excluded from ordinary-route conclusions. Utility extraction retains23 current-item/component records with zero decode errors, excluding old-item duplicates and unreliable synthetic headers.

Decode failures and bounded scan coverage are disclosed in the topic reports. No installed game files, saves, AP code, equipment tier assignments or workbook answers were changed.
