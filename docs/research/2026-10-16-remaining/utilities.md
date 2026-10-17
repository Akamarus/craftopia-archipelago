# Generator and bucket follow-up

This resolves the remaining prefab-output question from the preceding utility review. `utilities.json` retains byte-checked records and file hashes from the installed build; research scripts are workspace `artifacts/remaining-utilities.py` and `package-remaining-utilities.py`. No game changes or live tests were performed.

## Small generator produces batteries

The actual `resources.assets` component514032, class `OcBldg_ItemGenerator`, has `generateItemID=2100` and `isAlwaysProcessing=false`. Its parsed GameObject/Transform ancestry ends at `oc_pf_ItemPrefab_02000`, via `EquipPrefab--Generator_2000`. Current item2000 is Generator (Small), status1. Item2100 is Battery, status1, craft-time value20. Thus the output is established from the prefab, not inferred from the class name. Sharedassets0 component112670 provides the corresponding equipped-part definition. Large-generator prefab2002 also points to2100, but current item2002 is status0 and must not be treated as a normal alternative.

The small generator's captured recipe uses bench20011 at raw age2 (displayed Age3): Iron Ingot712 x1, Cogwheel2451 x1, Log746 x5. Its current asset `extraValue_A=1` participates in speed calculation. Do not interpret Battery's no-ingredient metadata as a hand-crafting recipe.

`OcZone_BldgMountBase.OnTriggerStay` accepts a non-pet enemy with health and an expired mounting cooldown, or a sprinting player facing within45 degrees of the zone direction. `OcCondition.setAf_BldgMount` additionally requires `!IgnoreGenerator`, not NoHit, not HA and not UA. Captured non-pet balls entering an available zone spawn at the mounting position; pet balls use ordinary spawn behavior. These are eligibility conditions, not proof that every enemy species can operate it.

`OcZone_GeneratorRunning.StateLoop` starts production after the starting animation and stops/disposes when mounting ends. The request is ten queued units, not ten batteries per completed unit. The base crafting speed uses the building's ExtraValue_A and enchantment speed contribution (including the implementation's additive clamp); statue bonuses and craft modifiers also participate. A simple claim of one battery every20 wall-clock seconds is therefore not justified by the item's20 craft-time field.

Enemy mounting behavior periodically applies damage based on that enemy's `GeneratorDmgRate` when slip damage is enabled and its chunk is not frozen. This prevents assuming that one captured creature supplies an unlimited uninterrupted stream. Player operation is a separate supported path in the mounting code.

## Cooking water does not establish bucket reuse

The previous audit verified the filling action and a30% empty-bucket branch in the separate pouring action. The automated Cooking Pot path is now traced through `OcBldg_ItemGenerator_CookingPot.moveCoe` -> `OcChestComponent.searchCookingItem` -> `consumeAutoCraft` -> stack `TryCostItem`/`Decrease` -> `OcItemDataMng.CraftItem`.

The normal consumption path subtracts recipe ingredient counts; `CraftItem` produces the requested output and transfers eligible enchantments. No empty-bucket creation appears in these inspected methods. Water811's recipe consumes filled bucket839 x1 and produces10 Water. Treat the filled bucket as consumed for this route; a guaranteed return is not supported. Build-mode, infinite-cooking and no-consumption enchantment branches are exceptions to consumption, not empty-bucket rewards. This conclusion describes the inspected vanilla automated route, not every external mod or every possible event subscriber.

The empty-bucket recipe remains early and inexpensive: current item1919, raw age0, ten buckets from one Log746 and one Stone698. This provides a replenishment route even without guaranteed bucket recycling.
