# Verified progression — v1.12.3

Regional revision 4 uses the audited recipes for the Workbench, Stone Furnace, Blacksmith, Cooking Pot, Wheat Field, Handmade Glider, Market and Campfire. Their ingredient and operating-permission dependencies now participate in the same least-fixed-point graph as tower repairs. Construction does not grant an operating permission.

The 52-check Geezah selection and tower-stage-3 AND Ruin of Beginning goal remain unchanged. The pool gains a second Bow progression item in place of one filler point. Equipment contract `equipment_loot_20261016_r2` preserves the reviewed Goblin's Club and adds Goblin's Bow6187 at Bow tier2, with no additional AP age gate. This is an explicit balance decision, supported by an active Tutorial-region goblin archer source; it is not an automatic conversion from item rank. Older equipment contracts retain their behavior. See [equipment rationale](LOOT_EQUIPMENT_REVIEW_r2.md).

## Water and farming correction

Well332 needs a blueprint and five iron ingots; it is not a free early structure. Mission105's title mentions a well, but its installed target is item839 only: sharedassets6 Mission41092 points to sharedassets0 ItemData82505. The collection handler checks ID and possessions, not station provenance. Mission106 targets Wheat Field296 (sharedassets6 Mission41093 → sharedassets0 ItemData81962).

The new route crafts empty buckets1919 from stone/logs and fills them at a water surface. Farming requires Wheat Field construction, filled-bucket access and Production: farming. Local accessible water is an explicit Geezah route assumption for live verification. No Well unlock, iron or blueprint is fabricated.

## Reusable later progression support

Audited catalog revision2 contains259 item definitions. The original revision1 JSON is preserved. All ten towers can compile exact sequential repair dependencies through `repair_nodes`; paid age transitions require an explicit currency capability through `evolution_node`. Quantities remain documented costs rather than simulated inventory balances.

`protection_nodes` compiles separate fire/cold/poison thresholds from individually reviewed torches, amulets and potion recipes. It requires ingredient, station, age and operating-permission routes supplied by the profile. It accepts one sufficient item per alternative: it does not assume accessory stacking, sum potion buffs, equate Hellfire with heat, or use inactive Hell Resistance Potion5547. Potion routes represent repeatable supply, not permanent protection or proof of a safe travel duration.

This is infrastructure for later-region logic, not authorization to select unverified endings. Geezah has no environmental requirement. Later endings remain rejected until their dungeon/check identities and complete regional access routes are established. The 48 presumed legacy IDs remain parked; additional loot tiers without individual review remain unsupported.

## Focused playtest

Use the new isolated regional seed with the matching v1.12.3 mod/bridge after installation. Do not substitute it into a previously played slot. No game, save, running server or installed plugin was changed by preparing it.

1. Craft empty buckets, fill one at accessible water and confirm mission105 sends its check without building a Well.
2. With Farming received, water/harvest a Wheat Field and confirm the checks.
3. Confirm Blacksmith/Cooking Pot remain dependent on actually obtaining copper ingots; Smelting and Cooking should be found among the opening missions.
4. If Goblin's Bow drops, verify denied below Bow tier2 and allowed at tier2. It is not required to drop for victory.
5. Complete all three Geezah tower repairs and Ruin of Beginning. Neither goal alone should finish the run; both should. Verify completion survives reload.
