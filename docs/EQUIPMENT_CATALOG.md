> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Equipment Catalog — Static Metadata Extraction

**Source:** `AD__Overcraft.dll` (SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`),
inspected with Python `dnfile` (metadata tables only — TypeDef, Field, Constant; assembly never executed).
**Date:** 2026-10-06

All type/field/enum names below are verified verbatim from metadata. Enum integer values were
resolved from the `Constant` table via `HasConstant` parent row matching and cross-checked by two
independent methods (row-identity and 1-based row-number); both agree.

---

## 1. Equipment slots — `Oc.OcEquipSlot` (enum, sealed)

| Member | Value | Notes |
|---|---|---|
| `WpMain` | 0 | Main-hand weapon |
| `WpSub` | 1 | Sub/off-hand weapon |
| `WpTwoHand` | 2 | Two-handed weapon slot |
| `WpDual` | 3 | Dual-wield slot |
| `EqHead` | 4 | Head armor |
| `EqBody` | 5 | Body armor |
| `FlightUnit` | 6 | Glider / jetpack / wing |
| `Ammo` | 7 | Ammunition |
| `Accessory` | 8 | Accessory |
| `Accessory_Dummy01` | 9 | Second accessory slot (name suggests placeholder) |
| `Max` | 10 | Sentinel (count) |
| `None` | 11 | Sentinel (empty) |

**Determinable:** 10 usable slot values (0–9); weapon slots are `WpMain/WpSub/WpTwoHand/WpDual`;
armor slots are only `EqHead`/`EqBody`.
**NOT determinable:** whether `Accessory_Dummy01` is actually usable in-game vs. reserved;
UI presentation/grouping of slots; per-slot equip rules (e.g., can a 1H sword go in `WpSub`?).

---

## 2. Weapon categories — `Oc.OcWpCategory` (enum, sealed)

This is the per-item weapon/tool category, stored on `Oc.Item.ItemData.wpCategory`
(verified field type: `valuetype Oc.OcWpCategory`).

| Member | Value | Member | Value |
|---|---|---|---|
| `None` | 0 | `Ball` | 19 |
| `Tool_Axe` | 1 | `Bell` | 20 |
| `Tool_Pickle` | 2 | `BucketWater` | 21 |
| `Tool_Torch` | 3 | `_Dummy3` | 22 |
| `Tool_Hammer` | 4 | `Seed` | 23 |
| `Sword` | 5 | `FishingRod` | 24 |
| `Bow` | 6 | `BucketEmpty` | 25 |
| `Shield` | 7 | `Detector` | 26 |
| `Head` | 8 | `AccessoryNormal` | 27 |
| `Ammo` | 9 | `SwordTwoHand` | 28 |
| `Lance` | 10 | `RecallRod` | 29 |
| `_Dummy1` | 11 | `FlightUnit_Wing` | 30 |
| `_Dummy2` | 12 | `GravityRod` | 31 |
| `Bldg_Put` | 13 | `PhotoDrone` | 32 |
| `Bldg_Summon` | 14 | `Binoculars` | 33 |
| `FlightUnit_Glider` | 15 | `GravityGlove` | 34 |
| `FlightUnit_JetPack` | 16 | `Gun_TH` | 35 |
| `Lantern` | 17 | `Katana` | 36 |
| `TorchTwoHand` | 18 | `MagicStaff` | 37 |

| Member | Value |
|---|---|
| `AssistEquip` | 38 |
| `ColorSpray` | 39 |
| `Rail_Spline` | 40 |
| `Bldg_Group` | 41 |
| `WireActionTool` | 42 |
| `VacuumGun` | 43 |
| `Tool_Hoe` | 44 |
| `Max` | 45 |

**Combat weapon categories** (cross-referenced with `Oc.OcAttackWeaponAttr` bitflags and the
per-weapon damage-buff fields on `ItemData`): `Sword` (5), `Bow` (6), `Shield` (7),
`Lance` (10), `SwordTwoHand` (28), `Gun_TH` (35), `Katana` (36), `MagicStaff` (37),
`VacuumGun` (43). Everything else is tools, buildings, flight units, gadgets, or dummies.

**Determinable:** the full category list and values.
**NOT determinable:** which `ItemData` rows (item IDs) use each category — item data lives in
`ScriptableObject` assets / `Oc.SoItemDataList`, not in this assembly's metadata.

---

## 3. Weapon attack attributes — `Oc.OcAttackWeaponAttr` (enum, sealed, bitflags)

| Member | Value |
|---|---|
| `None` | 0 |
| `Unarmed` | 1 |
| `Sword` | 2 |
| `TwoHandSword` | 4 |
| `Dual` | 8 |
| `Katana` | 16 |
| `Bow` | 32 |
| `Lance` | 64 |
| `MagicStaff` | 128 |
| `Gun` | 256 |
| `Magic` | 512 |

These flags correspond 1:1 with the per-weapon damage-buff fields on `Oc.Item.ItemData`:
`buffFinalDamageRate_Bow`, `_Sword`, `_Dual`, `_Lance`, `_TwoHandSword`, `_NoWeapon`
(= Unarmed), `_Katana`, `_Gun`, `_MagicStaff`. (`Magic` has no buff field.)

**Determinable:** 9 weapon combat styles + Unarmed + Magic.
**NOT determinable:** which style a given item ID uses at runtime (comes from item data / animation state).

---

## 4. Item type & category — `Oc.Item.ItemType` / `Oc.Item.ItemCategory`

### `Oc.Item.ItemType` (enum, sealed)

| Member | Value |
|---|---|
| `N_A` | 0 |
| `Equipment` | 1 |
| `Building` | 2 |
| `Material` | 3 |
| `Relic` | 4 |
| `Consumption` | 5 |
| `Skill` | 6 |
| `Pet` | 7 |
| `Important` | 8 |
| `CustomVehicle` | 9 |
| `Other` | -1 |

All equippable gear is `ItemType.Equipment` (1).

### `Oc.Item.ItemCategory` (enum, sealed) — equipment-relevant members

| Member | Value |
|---|---|
| `_01_Weapon` | 1 |
| `_02_Tool` | 2 |
| `_04_Shield` | 4 |
| `_09_Head` | 9 |
| `_10_Glider` | 10 |
| `_11_Accessory` | 11 |
| `_12_Ammo` | 12 |
| `_55_BodyEquipment` | 55 |
| `_84_Weapon_CantDeliver` | 84 |
| `Empty` | 0 |

(Full enum has 52 named categories + `Empty` = 53 named members; the rest are food/building/fish/etc. — omitted here
as non-equipment. Complete list verified in metadata.)

**Anomalies verified (not extraction errors):**
- `_42_Ornament = 41` — the numeric name prefix does NOT match the value (41, not 42).
- `_78_MarineProducts = 80` **and** `_80_SUHI = 80` — duplicate value 80 exists in the
  shipped enum. Confirmed by two independent constant-resolution methods.

**NOT determinable:** the item-ID → category mapping (asset data, not metadata).

---

## 5. Armor

There is **no `Armor` type, enum, or class** anywhere in the assembly (zero types match `*Armor*`).
Armor is represented entirely by:
- Slots: `OcEquipSlot.EqHead` (4), `OcEquipSlot.EqBody` (5)
- Categories: `ItemCategory._09_Head` (9), `ItemCategory._55_BodyEquipment` (55)

There are no separate armor "types" (no light/medium/heavy, no legs/boots/gloves slots).
**Determinable:** head + body are the only armor slots.
**NOT determinable:** whether any armor has set bonuses or sub-types (would be in item data text/assets).

---

## 6. Tiers / levels / rarity — `Oc.Item.ItemData` fields (128 fields total)

Relevant verified fields on `Oc.Item.ItemData` (the per-item-type static data):

| Field | Type | Notes |
|---|---|---|
| `id` | `int` | Item ID |
| `rank` | `int` | Rank (progression tier candidate) |
| `rarity` | `int` | Rarity |
| `categoryId` | `int` | → `ItemCategory` value |
| `equipSlot` | `Oc.OcEquipSlot` | Verified valuetype |
| `wpCategory` | `Oc.OcWpCategory` | Verified valuetype |
| `handler` | `Oc.Item.ItemHandlerSO` | Behavior handler |
| `atk` / `def` / `matk` | `int` | Base stats |
| `hp` / `mp` | `int` | Bonus stats |
| `durabilityValue` | `float` | Verified float |
| `motionSpeed`, `movementSpeed`, `jumpPow` | (numeric) | |
| `resistFireLv`, `resistColdLv`, `resistPoisonLv`, `resistHellFireLv` | (numeric) | |
| `resistElementFireRate`, `resistElementIceRate`, `resistElementElectroRate` | (numeric) | |
| `hellFireValue`, `poisonValue`, `frozenVal`, `chillVal`, `electroVal` | (numeric) | Element/status |
| `elementFireValue`, `elementIceValue`, `elementElectroValue` | (numeric) | |
| `enchantID`, `lootEnchantId_00`–`_03`, `enchantInfluences` | (ids) | Enchant linkage |
| `equipmentActiveSkillId` / `equipmentActiveSkillLevel` | (ids) | |
| `equipmentPassiveSkillId1`–`4` / `equipmentPassiveSkillLevel1`–`4` | (ids) | |
| `workbenchId`, `workbenchLv`, `playerCraftCount`, `carftTimeCost` | (ids/ints) | Craft gating; note `carftTimeCost` typo is verbatim |
| `materialA_Id`–`materialE_Id`, `materialA_count`–`materialE_count` | (ids/ints) | Recipe (5 slots) |
| `_materialID`, `_materialNum` | (collections) | |
| `_Refine_MaxLv` | `int` | **Refinement level cap** (max upgrade level for this item type) |
| `_SoGunParam` | `Oc.SoGunParam` | Gun tuning asset |
| `_SoGunAssistEquipParam` | `Oc.SoGunAssistEquipParam` | |
| `maxStack`, `price`, `probInTreasureBox`, `probInGacha` | (numeric) | |
| `cooldownTime`, `effectiveTime` | (numeric) | Usables |
| `restoreHealth/Mana/Satiety` (+Rate), `addStamina` | (numeric) | |
| `buffAtk`, `buffMatk`, `buffDef`, `buffMaxHealth`, `buffMaxMana` | (numeric) | |
| `buffResist*Lv`, `buffAtkBoss`, `buffMatkBoss`, `buffDefBoss` | (numeric) | |
| `buffFinalDamageRate` + 9 per-weapon variants (Bow/Sword/Dual/Lance/TwoHandSword/NoWeapon/Katana/Gun/MagicStaff) | (numeric) | |
| `buffIncreaseAtkByDefRate`, `buffIncreaseMAtkByDefRate`, `buffIncreaseAtkByHpRate`, `buffIncreaseMAtkByHpRate`, `buffIncreaseAtkByMpRate`, `buffIncreaseMAtkByMpRate` | (numeric) | Scaling buffs |
| `renewBoundaryLevel` | (numeric) | |
| `CookedFoodList`, `existPrefab`, `buildingDecoIcon` | (misc) | |
| `activeSkillType`, `passiveSkillType`, `cantUse`, `deliveryCategory` | (misc) | |
| `inventoryTabId`, `familyId`, `status`, `isOldData`, `StatusMapOnly`, `devPriority` | (misc) | |
| `NO_CRAFT_FAMILY_ID`, `PLAYER_CRAFT_FAMILY_ID`, `WORKBENCH_CRAFT_FAMILY_ID` | (constants) | |

**There is NO `level` or `tier` field on `ItemData`.** Tiering signals available statically:
`rank` (int), `rarity` (int), `_Refine_MaxLv` (int), `workbenchLv` (int).
**NOT determinable:** the numeric ranges/semantics of `rank`/`rarity` (data-driven);
the actual per-item values (asset data).

### Item instance — `Oc.Item.OcItem` (25 fields, relevant subset)

| Field | Type | Notes |
|---|---|---|
| `_level` | `int32` | **Instance level** (verified) |
| `_enchantNum` | `int32` | Enchant count |
| `enchantSlots` | (collection) | Rolled enchants |
| `MaxEnchantSlot` | `int32` | Enchant slot cap |
| `EnchantCoolDownMin` / `EnchantCoolDownMax` | (numeric) | |
| `<EquipSlot>k__BackingField` | (property) | Instance's equip slot |
| `<BulletNum>` / `<BulletId>` | (properties) | Guns |
| `_additionalData` / `_additionalDataJson` | (blob) | Opaque per-instance extras |

**NOT determinable:** where the *current* refine level is stored on the instance
(`_Refine_MaxLv` is the cap on static data; no `refineLevel` field exists on `OcItem` or
`OcItemStack_Equip` — it may live in `_additionalData`/`_additionalDataJson`, or `_level`
may double as refine level for equipment). **Requires runtime evidence.**

---

## 7. Enchantments

- `Oc.Item.EnchantRarity` (enum): `Common`=0, `Uncommon`=1, `Rare`=2, `Epic`=3, `Legendary`=4
- Structures: `Oc.Item.ItemEnchantment`, `Oc.Item.ItemEnchantment\`2`, `Oc.Item.StEnchantment`,
  `Oc.Item.SoEnchantment`, `Oc.Item.SoEnchantment\`1`, `Oc.Item.BlankEnchantment`,
  `Oc.Item.EnchantFragmentHolder`, `Oc.Item.OcItemHandler_EnchantScroll`
- Category capability sets on `Oc.Item.StItemCategory` (6 fields): `RefinableSet`,
  `EnchantSet`, `ShowDurabilitySet`, `NoPreviewModelSet`, `Seed`, `EnchantTableSet`
  — these decide which categories can be refined/enchanted/show durability.
  **NOT determinable:** set *contents* (runtime static data, not metadata).

---

## 8. What is determinable vs. NOT — summary for the randomizer design

### Determinable statically (verified above)
1. Full equip-slot list with values (`Oc.OcEquipSlot`, 10 usable + 2 sentinels).
2. Full weapon/tool category list with values (`Oc.OcWpCategory`, 45 usable + `None`/`Max`).
3. Weapon combat styles as bitflags (`Oc.OcAttackWeaponAttr`, 9 styles + Unarmed + Magic).
4. Item type/category enums with values, including equipment categories
   (`_01_Weapon`, `_02_Tool`, `_04_Shield`, `_09_Head`, `_10_Glider`, `_11_Accessory`,
   `_12_Ammo`, `_55_BodyEquipment`, `_84_Weapon_CantDeliver`).
5. Every stat field on `ItemData` (atk/def/matk/hp/mp, resists, elements, durability,
   per-weapon damage buffs, equip skills 1 active + 4 passive, enchant IDs).
6. Tier signals on static data: `rank`, `rarity`, `_Refine_MaxLv`, `workbenchLv`.
7. Instance level exists: `OcItem._level` (int32); enchant slots/count on instance.
8. Enchant rarity ladder (Common→Legendary, 0–4).
9. Armor = head + body only; no armor types/classes exist in code.

### NOT determinable statically (needs runtime evidence or asset dumps)
1. **Item catalog**: which item IDs exist, their names, and their per-field values
   (`rank`, `rarity`, `atk`, `equipSlot`, `wpCategory`, …) — lives in `ScriptableObject`
   assets / `Oc.SoItemDataList`, not in IL metadata.
2. **Numeric semantics**: what `rank`/`rarity` ranges mean; which rank = which "tier"
   for randomizer progression; `_Refine_MaxLv` values per weapon.
3. **Current refine level storage** on instances (candidate: `_level` or
   `_additionalData`/`_additionalDataJson`).
4. **`RefinableSet` / `EnchantSet` contents** (which categories are refinable/enchantable).
5. **Slot usability details**: is `Accessory_Dummy01` (9) actually equipped in-game?
   Which `WpCategory` values are legal in `WpMain` vs `WpSub` vs `WpTwoHand` vs `WpDual`?
6. **Drop/loot tables** (`probInTreasureBox`, `probInGacha` exist as fields, but the
   tables and their item pools are asset data).
7. **Craft unlock groupings**: `workbenchId`/`workbenchLv`/`familyId` values and the
   workbench/level at which each weapon unlocks (asset data).

### Specific runtime evidence needed (for the operator's PC)
1. Dump `Oc.SoItemDataList` (or equivalent) item rows: `id`, name, `rank`, `rarity`,
   `categoryId`, `equipSlot`, `wpCategory`, `atk`, `_Refine_MaxLv`, `workbenchLv` —
   for all items with `categoryId` ∈ {1, 2, 4, 9, 11, 12, 55}.
2. For one weapon at refine levels 0..N, record `OcItem._level`, `_additionalDataJson`,
   and displayed stats — to locate current-refine storage.
3. Equip a 1H sword in each of `WpMain/WpSub/WpTwoHand/WpDual` via UI; record which
   slots accept it (slot-legality matrix per `WpCategory`).
4. Check in-game whether the second accessory slot (`Accessory_Dummy01`) is reachable.
5. Read `StItemCategory.RefinableSet` / `EnchantSet` contents at runtime (or from
   `BepInEx/config` dumps) to get refinable/enchantable category lists.

