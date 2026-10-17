# Tower and Age Diagnostic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Build a read-only collector update that records tower-stage requests and observed progress changes, and age advancement requests with before/after world levels.

**Architecture:** Extend the existing bounded F9 snapshot and observation buffer. Tower construction is asynchronous: a request is not completion. Record load flags and independent sampled state changes, without interpreting any diagnostic event as an AP check. Keep all production AP code and current seed untouched.

**Tech Stack:** C#, netstandard2.1, Harmony, reflection, existing net8.0 CollectorTests, Mono.Cecil signature verification.

**Spec:** `docs/superpowers/specs/2026-10-15-regional-progression-design.md`, tower/age evidence subsection only. Region generation and cold/heat testing are separate later work.

## Global constraints

- No game-state writes, inventory operations, save mutation, AP checks, or reward replacement.
- Preserve the existing character and world. Cold/heat live tests are deferred until the user reaches suitable recipes.
- Field-only projections; unknown or missing fields remain explicitly unavailable.
- Loaded-object snapshots are incomplete catalogs, not proof that other towers do not exist.
- Bound buffers, polling and output sizes. Exceptions must not escape into gameplay.
- Do not replace an installed DLL while Craftopia is running.

## Review focus

- Async request returns before actual progress changes: no success claim at postfix.
- Load-triggered stage assignment: record `isLoadFromSaveData`, never label a repair.
- World switch/despawn: discard comparison baseline, never compare different instances/worlds.
- Repeated/same/lower age assignments: preserve raw observations, never infer new advancement.
- Missing fields or output failures: explicit status and bounded retry, no default zero or console spam.

## Confirmed static evidence

- `Oc.OcBldg_WorldHeritage.TrySetProgress(int requestedProgress, bool isLoadFromSaveData)` is async void.
- `get_CurrentProgress()` reads `_UnderBuildingNum` and converts to byte.
- `_IsTower` distinguishes towers from other world heritage structures; `_Data` points to `SoWorldHeritageData`.
- `SoWorldHeritageData._WorldHeritageItemId` and `requireItemsAllProgress` are catalog candidates.
- `WorldHeritageSaveData` contains `GUID` and `CurrentProgress`; live inherited GUID field must be verified before using it.
- `Oc.OcUI_AgeEvolution.EvoAndCloseUI()` calls `OcWorldManager.AddWorldLevel(1)` in single-player and a network request in multiplayer.
- `SetWorldLevel(int)` is already observed by collector 1.4.1, but its before/after snapshot currently lacks the world-level field.

## Task 1: tower/age projection and correlation

Files: add `mod/DiagnosticCollector/ProgressionObservation.cs`; modify `WorldEvidenceCapture.cs`, `EvidenceProjection.cs`; add `mod/CollectorTests/ProgressionEvidenceTests.cs` and include production helper in its project.

- [x] Inspect inherited GUID fields, nested repair-cost data, and async state machine to identify actual mutation points. Save a signature/field report tied to the game assembly hash.
- [x] Add failing tests for `ProgressionObservation.Observe(string context, int instance, int? stage)`: first sample establishes baseline, identical values produce nothing, changed values yield old/new without success interpretation, null/context change resets baseline, capacity is bounded.
- [x] Run CollectorTests and verify new tests fail for the missing helper.
- [x] Implement the helper and explicit tower snapshot fields using only verified fields. Add loaded tower/data sections to F9. Permit only the precise repair-cost nested types in EvidenceProjection, with existing depth/row limits.
- [x] Test missing fields, nested cost truncation, and no property getter invocation. Run CollectorTests to passing.

## Task 2: observers and packaging

Files: `ObservationHooks.cs`, `DiagnosticCollector.cs`, `DiagnosticCollector.csproj`, `verify-hooks.ps1`, new `docs/DIAGNOSTIC_COLLECTOR_v150.md`.

- [x] Add tests/verification for exact signatures of `TrySetProgress(int,bool)`, `TryProgressConstruction()`, `EvoAndCloseUI()`, and `AddWorldLevel(int)`; preserve the existing SetWorldLevel observer. Missing/ambiguous targets must report unavailable.
- [x] Record tower request arguments, selected tower identity and current stage. Use bounded main-thread sampling to observe delayed changes; never call setters, completion methods, or async state machines. Sampling interval: 0.5 seconds; cap tracked towers at 128; reset on world-context change.
- [x] Record world context immediately before and after age calls, including world level and available load-state fields. Capture UI selection and referenced age requirements with an explicit safe projection. Distinguish request origin from observed mutation; do not claim multiplayer authority.
- [x] Bump collector to 1.5.0 consistently, retaining schema compatibility where additions are optional; document each new evidence field and limitations. Ensure exception handling and buffer caps remain in force.
- [x] Run `dotnet run --project mod/CollectorTests`, build DiagnosticCollector in Release, and run `verify-hooks.ps1` against the installed assembly. Report signature verification separately from live verification.
- [x] Copy DLL and SHA-256 into `artifacts/diagnostic-v1.5.0/`. Inspect diff for game mutators or unrestricted object traversal. Do not stage unrelated modifications.

## Task 3: live capture handoff

- [x] Prepare backup/install instructions; install only after game closure and within deployment authorization.
- [ ] User loads the existing character, presses F9 near an unfinished tower, completes one stage, waits for construction to finish, then presses F9 again. A cancelled/failed attempt is useful if practical; do not require spending extra resources.
- [ ] For the next naturally available age, capture F9 before and after advancement. Do not force advancement or require a fresh character.
- [ ] Inspect output for stable identity, load flag, requested/observed stages, costs and actual age transition. Mark unverified paths explicitly, especially multiplayer and higher-region environmental requirements.

## Completion boundary

This plan delivers diagnostic evidence collection, not randomized tower or age checks. No user action is needed in-game until a built and verified DLL is ready. Remaining uncertainty must be reported rather than filled with inferred stages or age names.

## Execution result — 2026-10-15

Collector 1.5.0 built with zero warnings/errors. CollectorTests passed; all 20 observer signatures match the installed game assembly. Independent review found no blocking issues. Packaged and installed while Craftopia was closed; installed SHA-256 matches the artifact: `696A6B3B6E9315A5A5B3E755B5AEE50774A0A31ED0022936AC36AD9B9E78E3B7`.

Previous collector backed up in workspace evidence/tower-age-20261015/DiagnosticCollector-before-v150.dll. Live tower/age validation remains pending. Existing character/world and AP session preserved; cold/heat testing deferred.

Implementation adjustment: explicit bounded ProgressionEvidence field projections replace broadening the general EvidenceProjection traversal. Added changeUnderBuilding(bool) observation alongside independent polling to capture asynchronous follow-through. UI age data is labeled current_age_evo_field because destination-age semantics are not established.
