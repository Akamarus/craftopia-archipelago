# Owatatsu and Brigandine materials audit — 2026-10-22

Research only. `extract.py` reproduces `evidence.json` from existing installed-build captures and the user's workbook; no production, workbook, save or game files are changed. It uses Python's standard library. The extraction passed: 33 tower-closure items, 12 age-material records, five gold-bearing drop definitions, three modern gold static components, and unchanged workbook SHA256. Assembly identity is the October16 audited `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`; this is reuse of that evidence, not a new installed-build hash check.

## Findings that affect implementation

Both towers' selected recursive crafting route requires **raw age4 / displayed Age5**. Nothing in either repair recipe closure requires raw age5. Linen Wool is craftable, and does not need a new unexplained gathering source. On October22 the user confirmed both herbs can be picked in Owatatsu and Brigandine, resolving direct regional supply. The precise gold rock/tool join remains a documented inference; combat sufficiency is independent of recipe availability.

| Tower / stage | Exact cost | Recipe dependency |
|---|---|---|
| 3810 / 1 | Gold Ingot714 ×20; Linen Wool3402 ×10 | Ingot: Gold Ore702 ×3, Furnace20018, raw4. Linen: Old Rag3400 ×5 + Sheep Wool758 ×5, Workbench20011, raw4. |
| 3810 / 2 | Bright Red Herb723 ×5; Ultramarine Herb735 ×5 | No active recipes. Their raw4 metadata is not a pickup or merchant age requirement. |
| 3810 / 3 | High Capacity Battery3463 ×20; Engine Parts3464 ×20 | Battery: Gold Ingot ×2 + Battery2100 ×5, Workbench, raw4. Engine: Iron Ingot712 ×5 + Battery2100 ×5 + Ceramic3470 ×5, Machine Factory20013, raw4. |
| 3811 / 1 | Bright Red Herb723 ×10; Ultramarine Herb735 ×10 | User-confirmed regional pickups; both towers together consume 15 of each. |
| 3811 / 2 | Engine Parts3464 ×20; Fuel Tank3465 ×20 | Tank: Iron Ingot ×5 + Rubber2455 ×5 + Cotton Canvas3401 ×5, Machine Factory, raw3. |
| 3811 / 3 | Steel Pillar5305 ×20; Bioplastic2459 ×20 | Pillar: Iron Ingot ×10 + Bolt3468 ×10 gives **10 pillars**, Furniture Workbench20020, raw3. Bioplastic: Bioethanol2452 ×2 + Grease2458 ×2, Aging Facility20206, raw4. |

The JSON contains every recursive dependency, plus stage-by-stage recipe-only leaf expansion. That expansion does not include station construction or age costs, does not substitute direct sand gathering, and should not be advertised as a minimal shopping list. It correctly respects Water and Sand outputs of10 and Steel Pillar output10. The selected tower chain reuses Smelting, Equipment Care, Farming and Cooking through existing modeled sources. Manual Machine Factory and Furniture Workbench recipes do not justify inventing a new production permission.

Two station capabilities must be added to the existing Yarden profile: Machine Factory item209 (Workbench, raw3; Iron Ingot10 + Stone10 + Log20) for family20013, and Furniture Workbench item206 (Workbench, raw1; Stone5 + Log5 + Copper Ingot1) for family20020. Their complete recursive material closure is included. Improved Workbench201 is not the recipe family20011 identifier: the captured basic and improved benches share that family. Do not replace the existing audited station semantics merely because raw4 recipes become available.

## Gold source and useful equipment

The workbook's Material sources row17 explicitly says **Gold Rocks / Owatatsu Archipelago**. This is user-provided regional evidence; its stale status column still says “Not answered,” so preserve the actual answer rather than treating that status as authoritative.

Modern source tables sharedassets13:200–202, SoEnemyData477–479, have rarity5 and guaranteed primary Gold Ore702 ×1. Captured level13 components21658,19306,21234 respectively reference those tables, have StaticObjType2 and respawn interval1800. These are templates in the earlier extraction, not proof of three placed Owatatsu rocks. Legacy table sharedassets0:80665 / ID73 has rarity6; do not mix its threshold with the modern records. Another gold table ID310 is an alternative enemy table, without a proved early combat route.

The audited harvesting comparison permits a matching tool when attack rank is at least source rarity minus1. Silver Pickaxe59 has rank4: it passes modern rarity5 gold. Recipe: Workbench20011, raw3, Silver Ingot713 ×1 + Log746 ×1. Thus a plausible supported gold route is Owatatsu + Silver Pickaxe, anchored by the workbook region and modern source definitions. That is a joined inference; an exact placed-rock join or player confirmation of Silver Pickaxe on those rocks would strengthen it. A Gold Pickaxe requirement would create an avoidable self-dependency. Bounce also reduces damage rather than proving no drops, so the threshold is a conservative efficiency requirement, not an absolute impossibility theorem.

Gold One-Handed Sword48 has raw4 Blacksmith20017 recipe: Gold Ingot714 ×3 + Log746 ×2 + Leather683 ×1; captured attack350. Silver Sword47 is raw3; Platinum Sword49 is raw5, consumes Platinum Ingot715 ×3 and the same log/leather, attack400. These equipment capture rows are preserved verbatim because the progression material catalog omits them. Gold Sword is a craftable candidate within the repair age ceiling once gold is accessible; **recipe and attack value do not prove sufficient combat for either dungeon**. Requiring Platinum unnecessarily adds raw5, Platinum Ore and additional source questions. User workbook Q028/Q029 reports no special entry items/quests, with combat gear the practical limit, but gives no proven minimum weapon.

## Herbs: direct route confirmed; barter alternative incomplete

Workbook rows23/25 were blank, superseded by the user's October22 confirmation: both herbs are pickable in Owatatsu and Brigandine. Use the explicit regional field capability; item metadata does not force raw4 acquisition. A supplemental read-only native join in `herbs.py` independently decoded item723 as sharedassets0:82389 and735 as82401. The existing full scene spawner export contains20 red/15 blue spawners under4th_biome and16 red/17 blue under5th_biome; these68 rows are retained in `herbs.json`. Examples level13:22391 (red) and22410 (blue) have repeat interval60, InitSpawnOnly0 and empty spawn conditions. Counts are serialized placements, not live stock guarantees. The user confirmation supplies the regional access conclusion; hierarchy names alone would not. `herbs.py` uses the bundled Python3.12 runtime and archived UnityPy dependency path; AP Python3.13 cannot load those native extensions. The previous [merchant audit](../2026-10-16-remaining/merchants.md) resolves a concrete alternative:

* Spring farming merchant, level10 spawner55145, approximately (912.739,71.818,-1342.322), catalog farming05, charges Tomato1204.
* Fifth-biome farming merchant, spawner55173, approximately (1544.413,257.453,27.442), catalog farming06, charges Pumpkin1234.
* Both offer each herb at raw0, base10 crop items per herb, renew stock at day end, have active serialized ancestry and recurring NPC configuration. All30 herbs for both towers cost300 crops at base price (100 for Owatatsu, 200 for Brigandine).

This is not a ten-gold purchase. Neither existing `farm_use` (Wheat) nor Suspicious Pumpkin6595 proves Tomato1204 or Pumpkin1234 acquisition. **6595 and1234 are distinct IDs.** Before enabling either trade route, establish renewable crop acquisition and traversal to the merchant. Merchant death is a random eligible-stock drop, not guaranteed desired-herb access. External map herb markers in the older material audit are investigation leads only; they do not prove live renewable pickup conditions. No new herb drop-table matches were found in the three reused harvesting asset exports; that bounded scan does not prove other sources absent.

## Raw age4 and raw age5

To reach raw4/displayed Age5, pay raw3 transition: 20,000 gold; Carnivorous Plant6585 ×5, Alexandrite of Meadow6586 ×20, Petrified Wood6587 ×20, Meadow Ancient Doll Fragment6588 ×5. A completed earlier tower can support evolution. Requiring completed3810 first makes a false cycle because3810 itself needs raw4 crafting.

Raw4→5 costs50,000 gold and **both** regional sets: Thunder Flower6590 ×5, Morganite6591 ×20, Bamboo6592 ×20, Archipelago Doll6593 ×5; Suspicious Pumpkin6595 ×5, Topaz6596 ×20, Mushroom Wood6597 ×20, Valley Doll6598 ×5. This transition is optional for the two repair closures; include only if separately selected as a check or required by a justified combat route.

The earlier [harvesting audit](../2026-10-16-followup/harvesting.md) already resolves all12 source configurations, copied into this JSON. Meadow, Archipelago and Valley ore/wood rarity1 accept Stone tools. Plants/dolls have pickup spawners, except Suspicious Pumpkin6595 has axe-target static source97, rarity1, primary drop5, which Stone Axe handles. There is no need for higher age-material tool tiers. Geographic access remains separate from template source configuration. Currency can reuse the earlier explicit market economy assumption rather than silently granting balances.

## Release decision

The recipes and age/station closure are ready to model without invention. Gold has user regional evidence and a non-circular source/tool candidate. The user-confirmed direct regional herb route now closes that gap; the exact Tomato/Pumpkin barter chain remains optional and unmodeled. Combat requires an explicit reviewed sufficiency assumption or targeted evidence. Keep unsupported routes unsatisfied; do not turn a material catalog row or external marker into free access. These findings do not themselves authorize changing the existing experimental designation or advertising a fully verified public release.
