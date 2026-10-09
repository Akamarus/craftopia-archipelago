> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Diagnostic Specification: Load/Identity/Validator Observation (read-only)

**Purpose:** Establish runtime facts needed before persistence
implementation. This is observation-only: log, do not mutate. No grant
pipeline, no bridge, no rewards, no replays, no save edits, no ledger
writes. Standalone diagnostic DLL; install **instead of**
`CraftopiaArchipelago.dll` for the diagnostic run, then swap back.

**Character:** disposable test character only (`yyakxm5ryugv.db`,
GUID -123). Never the main character. No new points will be
granted, so the validator is expected to PASS (0 vs 0); the reset branch
is not expected to trigger on this run.

---

## Patch A — Load entry observer (load-start boundary candidate)

- **Target:** `public void Oc.OcPlMaster.LoadSaveData()` (AD__Overcraft).
- **Type:** Harmony prefix. **Always returns true** (never skips).
- **Captures at entry** (before any save restoration):
  - `Oc.OcAllSceneShareSingleton.Inst.StartDedicatedServer` (bool)
  - `Oc.OcGameSceneShareSingleton.IsOpening` (static bool)
  - `__instance._isLoaded` (bool) — if true at entry, this is a
    same-object reload or re-entry; the old value proves nothing about
    the new load.
  - `Oc.OcPlMng.LocalPl` object identity (`RuntimeHelpers.GetHashCode`
    or `GetHashCode()`) — distinguishes same-object vs new-object loads.
  - Mod-owned `loadSequence` (int, incremented per entry, starts at 1).
- **Logs:** `[DIAG] load entry seq={N} dedicatedServer={b} isOpening={b} _isLoadedBefore={b} localPlHash={H}`
- **Answers:** does entry fire on every load? Does the dedicated-server
  early return trigger? Does IsOpening skip the validator path?

## Patch B — Load completion vs failure

- **Target:** same method.
- **Postfix:** logs `[DIAG] load completed seq={N} _isLoadedAfter={b}`.
- **Finalizer:** `static void Finalizer(Exception __exception)` — if
  non-null, logs `[DIAG] load FAILED seq={N} exception={type}: {msg}`.
  Void return: does not suppress. (LoadSaveData has no catch/finally;
  a failure before the end need not complete the load.)
- **Answers:** entry-without-completion proves failure; entry-with-
  completion proves the full sequence ran (including `_isLoaded=true`).

## Patch C — Character identity (read-only; never call GetCharacterGUID)

**Forbidden:** `OcPlCharacterManager.GetCharacterGUID()`. Its IL proves
mutation: null selected character → returns 0; GUID==0 → calls
`issueGUID()` and assigns via `stfld`. Calling it for observation would
alter the save.

**Allowed reads** (both pure, verified by IL):
1. `OcPlMng.LocalPl` → if non-null, `.GUID` property
   (`get_GUID()` = `ldfld _GUID; ret`; no mutation).
2. `OcPlCharacterManager.Inst` → via reflection, private field
   `_selectedCharacter` → if non-null, its public `GUID` field
   (direct field read; no method call).

- **Capture at:** LoadSaveData entry (Patch A) and validator prefix
  (Patch D).
- **Sentinels:** object null → `int.MinValue`. Field value 0 →
  "unissued" (do not call the getter to fix it; log as 0).
- **Negatives are real:** GUID is signed Int32; -123 is valid. Do
  not filter, clamp, or assume positive. Log raw values.
- **Logs:** `[DIAG] identity seq={N} at={loadEntry|validator} plGuid={i} scGuid={i} match={b}`
- **Answers:** is `OcPl.GUID` populated at the validator boundary?
  Does it match `_selectedCharacter.GUID`? (Runtime facts; not yet
  confirmed.)

## Patch D — Validator before/after values

- **Target:** `public void Oc.Skills.OcSkillManager.GameStartCheckSkillPoint()`.
- **Prefix** (always returns true; never skips):
  - `level` = `OcPlMng.LocalPl.PlLevelCtrl.Level.Value` (byte → int)
  - `k` = `Oc.OcDefine.INCREASE_SKILLPOINT_BY_LEVEL_UP` (expect 2)
  - `mission` = `OcMissionManager.Inst.GetTotalSkillPointTaken()`
  - `assigned` = `OcSkillManager.Inst.CurrentAssignedSP`
  - `unspent` = `OcPlMng.LocalPl.SkillCtrl.SkillPoint`
  - `allowed` = `(level - 1) * k + mission`; `actual` = `assigned + unspent`
  - Store before-values in `__state`.
  - Log: `[DIAG] validator before seq={N} level={l} k={k} mission={m} assigned={a} unspent={u} allowed={x} actual={y} match={b}`
- **Postfix** (takes `__state`; readback only):
  - Re-read `assigned`, `unspent`.
  - Log: `[DIAG] validator after seq={N} assigned={a2} unspent={u2} changed={b}`
  - If changed, the reset branch executed — the first runtime
    confirmation (zero matches in archived logs so far).
- **Answers:** exact validator inputs; whether the reset branch runs.

---

## Static vs runtime facts

| Fact | Status | Source |
|---|---|---|
| `GameStartCheckSkillPoint()` signature/behavior | Static confirmed | IL |
| Sole direct caller: `LoadSaveData` IL0307 | Static confirmed | callers.csv (direct refs; reflection/dynamic not ruled out) |
| `GetTotalSkillPointTaken` has 2 callers (validator + skill-tree UI) | Static confirmed | callers.csv — postfix on getter rejected |
| K=2 | Static confirmed | cctor IL |
| `GetCharacterGUID()` mutates on GUID==0 | Static confirmed | IL (`issueGUID` + `stfld`) |
| `OcPl.GUID` is pure field read | Static confirmed | IL (`ldfld` + `ret`) |
| No catch/finally in `LoadSaveData` | Static confirmed | IL |
| `_isLoaded` only set true at end; never reset at entry | Static confirmed | IL |
| Load entry fires reliably | **Runtime: needs confirmation** | Patch A |
| `OcPl.GUID` populated at validator boundary | **Runtime: needs confirmation** | Patch C |
| `_selectedCharacter.GUID` matches `OcPl.GUID` | **Runtime: needs confirmation** | Patch C |
| Validator before/after values | **Runtime: needs confirmation** | Patch D |
| Reset branch executes at runtime | **Runtime: needs confirmation** | Patch D (absent from logs so far) |
| Same-object reload occurs | **Runtime: needs confirmation** | Patch A (`_isLoadedBefore`, hash) |

## Constraints

- Read-only: no game-state mutation, no save writes, no ledger/file
  writes except the BepInEx log. The `__state` and `loadSequence` live
  in memory only.
- Never invoke `GetCharacterGUID()` or any method not listed above.
- Handle nulls at every read (manager singletons, `LocalPl`,
  `_selectedCharacter`); log sentinels, do not throw.
- Diagnostic code must not throw: wrap each patch body in try/catch
  that logs and (for prefixes) returns true.
- Review patch source before any deployment. Controlled
  disposable-character run only.

## Deliverable from Codex

A single run: launch → main menu → load test character → reach
gameplay → exit. Return the `[DIAG]` log lines verbatim, plus explicit
answers to: (1) load entry count vs world loads; (2) `plGuid` and
`scGuid` values at both capture points and whether they match;
(3) validator before/after values and whether anything changed;
(4) any `_isLoadedBefore=true` or `load FAILED` lines; (5) whether
`IsOpening` or dedicated-server branches were observed.

