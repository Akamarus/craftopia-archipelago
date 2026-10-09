> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v3.4 — Contract Corrections (no implementation)

Corrects two claims and two clarifications. No build, deployment, save
edits, recovery, or replay. Tests remain **[PROPOSED]**.

---

## 1. Exception containment corrected (retracts "LoadSaveData always completes")

**Retracted:** our gate cannot guarantee `LoadSaveData` completes. Once
the prefix returns `true` and the game's validator runs, a throw inside
the game's method propagates per game behavior. A postfix readback does
not contain it.

**Contract:**

- **Our code** (prefix evaluation: `Resolve`, `Evaluate`, decision
  logic — all before the original runs): wrapped in try/catch. On
  throw → defer (return false), `succeeded=false`, grants blocked.
  Safe: the original never ran, nothing mutated.
- **The game's code** (original `GameStartCheckSkillPoint`): **never
  wrapped, never suppressed.** We do not set `__exception = null` and
  do not catch around it. If it throws — including after partially
  mutating skills — the exception propagates as the game intends.
- **Postfix:** declared with `__exception`. If non-null → log FATAL,
  `succeeded=false`, rethrow (do not suppress). If null → readback
  (`actual == allowed`) to confirm; mismatch → `succeeded=false` +
  FATAL.
- **No blanket suppression**, specifically because a mid-method throw
  may follow partial skill mutation; hiding that would corrupt
  silently. A throw from the game's validator is game behavior to
  report, not to paper over.

---

## 2. Invalidation at a verified load-start boundary (not validator-call count)

**Retracted:** `lastValidationEventId` counted validator invocations. A
same-object reload that skips the validator (`IsOpening`) or fails
before reaching it would not invalidate a prior success.

**Contract:** validation success is invalidated at a **load-start
boundary** — a game event that reliably fires at the beginning of every
player-character load, before save data is applied.

- **Candidate:** `OcPlMaster.LoadSaveData` entry (a non-skipping
  observer prefix that only bumps `loadSequence`). Every validator call
  occurs inside a `LoadSaveData`; a load that skips the validator still
  bumps the sequence, voiding the prior record.
- **Requirement for Codex (not asserted here):** verify this boundary
  fires on every load path — normal load, same-object reload,
  character switch, and the `IsOpening` path — or identify the correct
  one. Do not invent it.
- `CanApply` requires `validationRecord.loadSeq == currentLoadSeq`
  (plus the v3.3 `playerRef`/`guid`/`epoch`/`succeeded` checks). Any
  load start without a subsequent successful validation → ineligible.

### Soft validation trigger corrected

v3.3 tied soft re-validation to the NOT READY → READY transition. If
the epoch becomes available **after** the player is already READY
(late bridge connect), no new transition fires → grants permanently
blocked.

**Corrected trigger:** the per-frame poll runs soft re-validation
whenever **all** hold simultaneously, regardless of transition history:
1. player READY (`_isLoaded`, valid local player — save fully loaded),
2. last validation deferred specifically on `HOLD_TRANSIENT` from null
   epoch (never for recovery holds),
3. epoch now non-null,
4. no successful validation yet for the current load.

It uses `Evaluate()` (pure, read-only); a discrepancy follows the
normal defer+hold path. It never bypasses recovery holds.

---

## 3. Clarification: persisted `ownerOpId` confers no authority

The `ownerOpId` string is readable from the JSON after restart. **Reading
it grants nothing.** Ownership authority is the in-memory continuity of
the live operation that performed the intent write — the fact of being
that execution, not knowledge of the string.

- After restart there is, by definition, no live owner; every intent is
  unresolved → hold, even though its `ownerOpId` is legible.
- Any code path that matches a persisted op ID to *claim* authority is
  a bug. The persisted ID exists for diagnostics (logs, manual
  reconciliation) and as a defense-in-depth check by the live owner
  (verifying the file it is about to commit is the one it wrote).
- State explicitly: **authority is not reconstructible from persisted
  data.**

---

## 4. Clarification: partial enrollment completion requires same-operation evidence

v3.3 allowed retrying a failed init-file `enrolledAt` mark. Corrected:
the retry must prove it belongs to the same pending enrollment.

- **Same process run:** the enrolling code keeps in-memory
  `pendingEnrollmentMark = {guid}` when the store write succeeded but
  the init mark failed. On subsequent boundaries it may retry **iff**
  a fresh re-read shows the init entry still present without
  `enrolledAt` (the operator may have revoked it meanwhile — never re-add a
  removed entry).
- **After restart:** `pendingEnrollmentMark` is empty. A
  history-without-`enrolledAt` state is **unexplained** → do not
  silently repair into freshness → `UNCERTAIN_HISTORY`, hold, explicit
  reconciliation (which can verify and complete it manually).
- An unexplained missing consumed marker is never auto-repaired. The
  only automatic completion is the same-operation retry above.

---

## 5. For Codex: lifecycle facts to establish from the installed game

No deployment, save edits, or recovery of existing indices. Establish:

1. **Load-start boundary:** what reliably fires at the beginning of
   every player-character load (normal, same-object reload, character
   switch, `IsOpening` path)? Is `OcPlMaster.LoadSaveData` entry
   sufficient, or is there an earlier/correct hook?
2. **Character identity at load time:** does
   `OcPlCharacterManager.Inst.GetCharacterGUID()` return the loading
   character's stable GUID during `LoadSaveData`? Test vs main
   character GUIDs differ?
3. **Complete validator behavior:** all `GameStartCheckSkillPoint` call
   sites; `GetTotalSkillPointTaken` callers; whether the game's method
   can throw mid-execution and what partial state that leaves;
   `INCREASE_SKILLPOINT_BY_LEVEL_UP` value; Japanese reset line in
   Player.log from the reload session.

