# Diagnostic Collector 1.5.0 — tower repair and age evolution

Read-only evidence collection. No AP checks, game saves, inventory, repair costs or progression are modified by this collector. No new character is required. Cold/heat survival tests are deferred until later progression.

## New evidence

- F9 `world_evidence.towers`: active loaded towers, candidate GUID, scene position, raw construction stage, definition item ID, and bounded per-stage material IDs/counts.
- Action observations: `TryProgressConstruction`, `TrySetProgress(int requestedProgress, bool isLoadFromSaveData)`, and `changeUnderBuilding(bool)`. Raw arguments retain the load flag.
- Age observations: `EvoAndCloseUI`, `AddWorldLevel(int)` and the existing `SetWorldLevel(int)` now include before/after world context. Age UI evidence includes its raw selection index and `_currentAgeEvo` definition; that field name does not prove a destination age.
- Every 0.5 seconds, compare up to 128 active tower instances. Emit only changed stages after establishing a baseline; discard missing/despawned instances and reset on world-selection reference changes. Samples are observations, not inferred completions.
- Existing schema 5 is retained with additive fields and collector version 1.5.0. The observation buffer remains capped at 256 rows, 10,000 total observations and 64 MiB written output. Repair definitions are capped at 32 stages and 32 costs per stage, with truncation flags.

## Interpretation

`TrySetProgress` is async void. Its initial progress assignment precedes an await; later visual/notification work happens in the continuation. Neither method return nor a sampled field change proves paid repair success. Preserve request, load flag and subsequent observations together.

The same-level/lower-level/load age assignments are intentionally recorded without awarding anything. World comparison fingerprints are not global IDs and may match copied saves. Multiplayer request authority and stage semantics still require verification. Only active loaded towers are captured; absent entries do not mean towers do not exist.

## Current-character test

1. Load the current character/world and approach an unfinished tower. Press F9 and wait for the collection-complete message.
2. Complete **one** available repair stage normally. Let its construction sequence finish. Press F9 again.
3. When the next age is naturally available, press F9 before advancing, advance normally, then press F9 afterward.
4. Stop and report unexpected console errors or stalled game behavior. A cancelled or insufficient-material attempt is useful optional evidence; there is no need to spend extra materials for it.

Files appear in the collector plugin's `diagnostics` folder. Observation files flush automatically approximately every two seconds; F9 produces the fuller catalog capture. Do not delete or edit them during the test.

## Verification boundary

Collector unit tests exercise baseline/reset/change behavior, bounded capacity, missing values, despawn, lower levels, explicit cost projection and truncation. Static verification checks all 20 Harmony target signatures. Build and signature checks do not establish successful live hooks; the above runtime test remains required.
