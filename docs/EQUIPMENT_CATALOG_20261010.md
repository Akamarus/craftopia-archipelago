# Equipment catalog and evaluator checkpoint

The October 10 schema-4 capture has been converted into an explicit, reproducible decision table. This is a tested permission evaluator, **not deployed equipment enforcement or a playable equipment-randomized seed**. No save, live ledger, ownership, installed DLL, or generated seed was changed.

## Evidence and scope

- Source capture SHA-256: `9630d555339e48a85f62dac620f31a3b60229b651458d2154c31fbbd561b6233`.
- Installed assembly SHA-256: `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.
- Catalog: `worlds/craftopia/equipment_catalog_20261010_r1.json`, schema 1.
- Exporter: `tools/export_equipment_catalog.py <private-capture> <catalog-output>`.
- Catalog contains only selected item metadata and provenance; no character/world/position/account identifiers or game binaries.

| Decision | Rows | Meaning |
| --- | ---: | --- |
| Progressive candidate | 237 | Nine weapon categories plus head/body; recipe age proposes tier = age + 1 |
| Unsupported | 429 | Combat equipment without a supported ordinary recipe/metadata, including all 27 AssistEquip rows |
| Vanilla | 6,386 | Outside these gates; original game checks still apply |

The 429 unsupported rows include disabled/old definitions as well as loot-only gear. They are not 429 obtainable items. Ammo, accessories and utility gear intentionally remain outside this first equipment scope. AssistEquip combat gloves are explicitly unsupported rather than mistaken for tools. Neither rarity nor refinement is used to invent a progression tier.

Some categories have no age-zero recipe. A tier-one starter therefore does not imply immediately craftable gear in every category. Starter-aware generation and prerequisite profiles must resolve that before equipment seeds are advertised as playable.

## Implemented policy

`EquipmentEnforcement.cs` parses the whole catalog or rejects it. It verifies identity/schema, hashes, exact scalar types, enum bounds, duplicate IDs and consistency of category/age/tier decisions. Runtime snapshots must match the recorded metadata; callers cannot mutate the private catalog through returned snapshots.

When enabled, unknown IDs, unsupported combat items, changed metadata, missing permissions, unknown age and exceptions deny. Both AP ownership and normal world age are required for a progressive candidate. A successful decision only permits the original game operation: it does not skip resource, recipe, mastery or other vanilla requirements. Disabled evaluation does not access game state.

`AllowLoadout` evaluates every proposed item before the caller performs any transfer. One denied item denies the whole loadout. An empty, successfully read loadout permits removal; a missing/unreadable loadout denies. Diagnostics cannot turn a denial into permission.

## Installed-IL boundary findings

| Path | Finding and integration requirement |
| --- | --- |
| Manual craft | `OcItemUI_CraftMng.CanCraft(ItemData,int)` precedes resource costs. Delayed player-craft completion calls the checked `TryCraftItem(ItemData,int)` again. |
| Enchant-selected craft | `OcItem_CraftEnchantSelect.TryCraftItem(ItemData,int)` has its own resource-consumption path; gate at its entry as well. |
| Connector auto-craft | `canAutoCraftNum(ItemData)` is checked before `consumeAutoCraft`; preserve connector discovery/cleanup and never deny only after consumption. |
| Click/drag equipment | Equipment manager/slot permission checks precede destination clearing and stack transfer. |
| Low-level equipment | `OcPlEquipCtrl.setEquip` is too late for an inventory permission gate and also serves other paths. Do not insert a skipping prefix there. |
| Equipment loadout | `ApplyMyset` opens a confirmation dialog. Its callback first unequips current gear, then tries replacements. Re-evaluate the complete target loadout at the callback before its first unequip; dialog-time or per-item-only validation is insufficient. |

## Remaining integration work

1. Read live item/loadout snapshots without invoking item creation, prefab loading, inventory mutation or GUID issuance. Missing target data must deny the complete loadout.
2. Wire and verify the exact pre-cost/pre-transfer hooks above, including deferred callbacks. Confirm actual control flow on Unity Mono.
3. Bind activation to an explicit seed/catalog contract; preserve it across disconnects/holds. Define fresh-character admission and pre-existing equipped gear handling before claiming a use restriction. No automatic gear removal or history reset.
4. Keep equipment out of existing seeds. Add starter-aware pools and complete prerequisite coverage before a supported playable seed.

## Verification

Eight exporter tests pass. The actual C# parser/evaluator passes targeted denial, independent-category, age, immutable-snapshot and whole-loadout tests plus a sweep of every non-progressive catalog row (6,843 total assertions; most are row coverage, not independent scenarios). Independent review found no remaining blocker for this pure-policy checkpoint and identified the combat-glove exclusion that was corrected before completion.

All 12 C# executable suites, 77 bridge tests and 22 world tests pass. Mod build: zero warnings/errors. Existing seed generation remains unchanged. No runtime hook, reconnect/loadout integration or beatability claim follows from these automated tests.
