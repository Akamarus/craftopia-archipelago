# Opening profile: opening_20261014_r1

This is an opt-in **11-mission pilot**, not a full-game beatability claim. Use a new seed. Existing candidate seeds and the ongoing AP192 test keep their current locations and items.

## Evidence and scope

Numeric IDs and frozen AP IDs come from `mission_catalog_20261007_r1.json`. That capture proves the entries exist, not their reachability. The additional basis is the October live opening-route tests documented in STATUS.md and EQUIPMENT_HOOKS_v192.md: punching trees, gathering logs/stone, crafting/equipping stone tools, using the ordinary workbench, and crafting/equipping the stone sword after permission. Stone pickaxe and ordinary crafting controls passed. Resource access is modeled as repeatable gathering, not inventory quantities. Native crafting still charges materials.

| Missions | Modeled requirement |
| --- | --- |
| 50, 53, 54, 57 | Opening tree/rock gathering, no AP permission |
| 51, 52, 55, 56 | Gather resources; stone tools remain vanilla |
| 58, 30 | Gathering and stone tools provide workbench access |
| 60 | Workbench access and one Progressive Equipment: Sword |

The profile's `reviewed` marker means this bounded dependency specification was reviewed; it is not proof that every mission notification was observed on every world. A full new-seed run remains pending. World/layout variation, native mission ordering and achievement timing may reveal additional requirements. Excluded: glider/leather chains, age advancement, ingots, bosses, all late missions and other check categories. The 125-entry snapshot is not treated as universally achievable.

## Pool and goals

One each: Progressive Equipment: Sword, Production: smelting, Production: cooking, Skill Tree: Magic, Skill Tree: Tamer; six Skill Points. Smelting and Cooking are opening rewards for continued play, not requirements of the opening victory. No placeholders or undeliverable bundles are added.

Victory is the configured count (1–11) of distinct enabled mission checks. All eleven is the sample goal. Boss/age goals remain rejected. Separate slots only: the existing equipment protocol rejects shared-slot equipment. Receiving an unlock still requires normal materials and native age/skill prerequisites. Equipment uses catalog equipment_20261010_r1 and protocol 1; existing explicit equipment-test admission and enrollment safeguards still apply. This is a controlled test profile, not a turnkey release.

The broad `verified` mode remains unavailable. `candidate` remains unchanged. `opening` exports only its actual eleven locations in routing and objective maps. No automatic migration or reseeding.

## Validation

Production generator tests exercise actual AP 0.6.8 fill for 20 seeds, check beatability under this model, prohibit Sword behind mission 60, check exact routing/pool counts, and reject shared mode and oversized goals. Synthetic cycle tests remain separate from game evidence. In-game full profile completion is pending.
