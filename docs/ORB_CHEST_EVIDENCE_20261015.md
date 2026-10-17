# Orb and chest evidence — 2026-10-15

## Check-pool decision

Respawning chests are excluded at the user's explicit request. `OcObj_RandomTreasure` observations are diagnostic-only: do not assign AP locations from their position hashes. Fixed chests with `_resetOnDayEnd=true` are also excluded. Unknown reset behavior does not establish permanence. No chest or orb locations were activated by this work.

## Soul orb

The observed GUID `-1595765594` retained its position across the user's menu/reload test. The four schema-5 snapshots show:

| Capture time (UTC) | `_firstTook` | `_taken` |
|---|---|---|
| 19:59:54 | false | false |
| 20:04:00 | false | true |
| 20:05:57 | true | false |
| 20:06:07 | true | false |

The single `SetTaken` observation at 20:03:56 recorded `_taken=false` to `true` with the same GUID. In the installed assembly, `SetTaken` sets that field. `OcSoulOrbMng.loadSaveData` obtains picked IDs from `OcPlCharacterManager.GetSoulOrbPicked` and calls `SetFirstTook(true)` on matching spawners. Thus `_firstTook` carries loaded character history; `_taken` alone is not a persistent collection test. Historical rows must not generate fresh collection events. This evidence does not prove cross-character identity stability or reward replacement.

## Chest mismatch resolved

The observation at 20:06:03 was `OcOWTreasureListHandler.OnOpenTreasure(fieldID=0, posHash=185247767066515153)`. The initial collector enumerated `OcGimmick_TreasureBoxBase`, but the overworld handler uses a distinct `OcObj_RandomTreasure` family. Its `Init` stores `_fieldID` and `_posHash`; `Open_FromRpc` looks up this type in `FieldTreasureData.treasureBoxBuff`. `FieldTreasureData.OnOpenTreasure` increments `_requireSpawn[2]` when the hash exists. The hash cannot be treated as a frozen permanent location.

Collector 1.4.1 adds a separate `random_chests` snapshot section and read-only `Pickup`/`Open_ForRpc` observers. This is for identifying excluded content, not adding checks. It preserves the existing 13 observers and all capture limits. Schema remains 5 because the sections are additive.

## Offline analysis and remaining work

`tools/analyze_world_evidence.py` reads schema-5 captures, inventories duplicate/invalid IDs, reports missing/truncated sections, distinguishes orb history from collection transitions, and explicitly excludes respawning-chest events. It retains file hashes and does not write game state, assign AP IDs, or submit checks. Regression tests cover reload history, incomplete fields, repeated orb collection, exact 64-bit chest IDs and duplicate catalog IDs.

Remaining before gameplay adapters: verify fixed-chest permanence and reward path; freeze a versioned location catalog; enforce session/character identity; deduplicate new versus historical orb collections durably; implement selected reward replacement without duplicate vanilla rewards; model access requirements. A collection observer by itself does not implement those contracts.

Assembly SHA-256 examined: `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.
