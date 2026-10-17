# General-chest runtime correction — 2026-10-16

The earlier twelve positive general-chest pool matches require a runtime qualification: **only Iron Gauntlet and Iron Glove retain positive selection at the normal runtime IslandLevel of zero**. The other ten cannot be selected by this particular general-chest path without an override. This is not global unobtainability: all twelve occur in the captured live crafting lists.

`OcTreasureLottery_General.Lottery` passes `OcWorldManager.IslandLevel` to `RandomTreasureData` (installed decompile lines 170435–170439). The [ItemOffer runtime audit](itemoffer.md) establishes that normal value is zero, separately from world age. This table filters the earlier decoded positive combinations to that actual value.

| Item | ID | Normal general-chest pool selectable | Live bench ID | Display age requirement |
|---|---:|---|---:|---:|
| Iron Gauntlet | 3270 | Yes, rarity indices 1/2 | 20017 | 3 |
| Iron Glove | 3271 | Yes, rarity indices 1/2 | 20017 | 3 |
| Gold Gauntlet | 3272 | No | 20017 | 5 |
| Gold Glove | 3273 | No | 20017 | 5 |
| Adamantite Gauntlet | 3274 | No | 20022 | 7 |
| Adamantite Glove | 3275 | No | 20022 | 7 |
| Mithril Gauntlet | 3276 | No | 20022 | 8 |
| Mithril Glove | 3277 | No | 20022 | 8 |
| High Mithril Gauntlet | 3278 | No | 20022 | 9 |
| High Mithril Glove | 3279 | No | 20022 | 9 |
| Orichalcum Gauntlet | 3280 | No | 20027 | 10 |
| Orichalcum Glove | 3281 | No | 20027 | 10 |

All twelve have `IsBlueprintRequired=false`. Exact ingredient quantities, raw age requirements, source hashes and code excerpt are retained in `chests.json`. Recipe membership was joined against the build-matched 2026-10-16 diagnostic capture rather than inferred solely from an item workbench field.

This correction concerns the general lottery, not every fixed or specialized chest. Positive pool selection still requires an applicable chest instance. Crafting evidence does not by itself resolve station construction or the complete ingredient chain. No game, workbook or production changes were made.
