# Equipment and prerequisite foundation

This continues the approved separate-category progressive equipment design. It does not enable an equipment-randomized playable seed yet.

## Implemented

- Append-only AP equipment IDs 26–36: Sword, Bow, Shield, Lance, SwordTwoHand, Gun, Katana, MagicStaff, VacuumGun, Head, Body. Existing IDs 1–25 and the generated pool stay unchanged.
- Bridge materializes each progressive receipt into a durable absolute tier. Starter ownership counts; receipt replay does not advance again; failed writes retry; categories advance separately. Tier cap is 10. Starter selection must finish first. These are permissions, never direct equipment grants.
- `ProgressionModel` evaluates explicit alternative resource/age/recipe dependencies plus AP item counts to a least fixed point. Unsupported cycles remain inaccessible. Unknown references, malformed counts, unreviewed profiles and missing mission coverage are rejected.
- `logic_mode: candidate` remains default. `verified` requires numeric mission instances and a reviewed prerequisite profile. No complete profile is bundled, so verified generation currently reports that limitation instead of silently using flat logic.
- Real AP 0.6.8 integration attaches the evaluator to network locations; objective events inherit those access rules. Synthetic tests demonstrate gating, not actual game beatability.

The model describes repeatable access, not consumable stock accounting. Profiles must establish renewable sources, quantity feasibility, combat access and station availability before marking a source accessible. Starter-aware equipment requirements still need to be encoded in the future reviewed profile. Equipment use/crafting hooks are not added by this change.

## Installed assembly evidence

Assembly SHA-256: `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.

- `OcItemUI_CraftMng.CanCraft` compares the recipe's `WorkBenchLv` with `OcWorldManager.WorldLevel` before crafting. The candidate extractor proposes tier `workbenchLv + 1`; rank, rarity and refinement are not used as tiers. This projection is not a final per-item unlock catalog.
- `NO_CRAFT_FAMILY_ID` is 20001; player crafting is 20002 and the opening workbench family is 20011. Candidates exclude no-craft rows, unsupported categories and ordinary-unavailable statuses.
- `SetupAgeEvoItemList` can return early on an out-of-range age. Its output must not be treated as a complete list of craftable items.
- Recipe selection reads the installed station item's family. The earlier Refiner/smelting label was wrong: that class processes weapon refinement. Positive furnace operation does not establish which permission gates it.

## Collector 1.3, schema 4

Adds item inventory-tab/prefab flags, localized item names and active loaded station type/item/family bindings. Station identity reads backing fields; it does not traverse inventories, serialize positions/owners, load item prefabs, issue GUIDs or change game progress. The one allowlisted item-name getter calls localization. Missing data and exceptions are reported explicitly. Active loaded stations are a partial snapshot, not a complete world/catalog scan.

Next capture: load the existing test character/world, stand near the furnace and workbench, and press F9 once. Existing stations are sufficient; no save reset, enrollment change or item injection is needed. Review `loaded_stations`, resolve names/families, then repair station dispatch and test locked and unlocked behavior separately.

## Verification

- Seven actual-bridge regressions cover distinct receipts, restart/replay, independent categories, failed writes, starter waiting, receipt mismatch, tier cap and interrupted batches.
- All 77 client tests pass with the real AP 0.6.8 checkout configured.
- Five prerequisite/candidate tests and three real-AP integration tests pass. All world tests pass (22), including unchanged legacy generation.
- Collector field-only tests include station backing fields and absent-data behavior. Collector build has zero warnings/errors.
- Independent source and installed-IL review found no concrete blocker; it did not perform a runtime collector capture. No equipment enforcement or fully beatable seed claim follows from these tests.

Collector 1.3 was installed after backing up 1.2 while the game was closed. Installed SHA-256: `9190b193d429b29b9744a2d7756fa41e5de1a35b6d1101e0784993f8d8ca2948`. Its first runtime capture is pending. Gameplay DLL and live bridge were not replaced by this follow-up.
