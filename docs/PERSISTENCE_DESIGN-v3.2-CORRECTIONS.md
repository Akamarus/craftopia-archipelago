> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v3.2 — Counterexample Corrections (no implementation)

Focused corrections to v3/v3.1 for three counterexamples and two
consistency fixes. No build, deployment, save edits, recovery, or replay.
These are proposed regression tests, not verified results until run
against an implementation.

---

## 1. Grant-intent before mutation (Counterexample 1)

**Bug:** enroll → apply → game saves → crash before persist leaves no
trace; restart returns `NO_RECORD_CONFIRMED` and permits a destructive
vanilla reset. The enrollment's "no grants since enrollment" check is
blind to mutations the game saved but we never recorded.

**Fix:** persist a character/session-bound grant-intent **before**
mutation. If the intent write fails, do not mutate.

### Store section

```json
"pendingIntents": {
  "<guid>": { "epoch": 123, "indices": [5], "attemptedAt": "..." }
}
```

### Lifecycle

1. Applicator, immediately before game mutation: durably write intent.
   - Write fails → return `Blocked_IntentWriteFailed`; **no mutation**.
     Pipeline keeps the job queued (a later retry re-attempts the write;
     safe — nothing happened).
   - Write succeeds → proceed to mutate.
2. On Confirmed: `SaveGrantState` atomically converts intent →
   entitlement entry + `applied`, and removes the intent.
3. On Ambiguous: intent is **retained** (possible unrecorded mutation);
   quarantine recorded alongside. Both require reconciliation.
4. On verified-no-mutation: applicator may best-effort clear the intent;
   a failed clear → hold (never silent retry).
5. On restart with unresolved intent: `PENDING_INTENT` → defer + block.
   **Never** restores freshness, **never** auto-replays.

### Pipeline no-replay rule

Before processing any job for a character, the pipeline checks
`pendingIntents`. If an intent exists for that guid, **no job for that
character is applied or acked** — including server replays of the
intent's indices and unrelated new indices. This closes the v3 Window A
duplication hole: the crash-before-persist case now leaves a durable
intent instead of a silent gap.

### Resolve integration

New status, checked after `STORE_UNREADABLE`, before entry lookup:

```
if pendingIntents.has(guid):
  return (PENDING_INTENT, "unresolved grant intent for indices [...]")
```

### Init attestation vs re-enrollment

The init file entry carries `enrolledAt` (written by the mod on
enrollment). Corrected enrollment branch:

```
if history == null:
  initEntry = InitFile.get(guid)
  if initEntry != null and initEntry.enrolledAt == null:
    # Genuine first contact: record durable firstSeen, mark consumed.
    ...
    return NO_RECORD_CONFIRMED
  elif initEntry != null and initEntry.enrolledAt != null:
    # Was enrolled; history record lost. DO NOT re-enroll.
    return UNCERTAIN_HISTORY ("enrollment record lost; enrolled at ...")
  else:
    return UNCERTAIN_HISTORY ("not initialized")
```

### Transitions (intent)

| From | Event | To |
|---|---|---|
| INITIALIZED | Intent write succeeds | INITIALIZED (intent pending) |
| INITIALIZED (intent pending) | Write fails | INITIALIZED (no mutation; job held for retry) |
| INITIALIZED (intent pending) | Confirmed + persist | ACTIVE (intent cleared; entry created) |
| INITIALIZED (intent pending) | Crash before persist | HELD (PENDING_INTENT) on restart |
| HELD (PENDING_INTENT) | Reconcile: save reflects grant | ACTIVE (manual; entry created; intent cleared) |
| HELD (PENDING_INTENT) | Reconcile: save lacks grant | Prior state (manual; intent cleared) |

---

## 2. Validation-gated application eligibility (Counterexample 2)

**Bug:** a deferred/skipped validation does not block later
applications. The catch only logged; a later-appearing epoch could yield
`OK` without the loaded balance ever being validated. Readiness
(`_isLoaded`, player valid) is necessary but **insufficient**.

**Fix:** applications and acknowledgements require a **successful
validation for the current (guid, epoch)** — not just a non-held
`Resolve`.

### State

Session-scoped: `validationRecord = {guid, epoch, succeeded: bool}`.

- Set by the validation prefix: `succeeded=true` only when the prefix
  evaluated without deferring (PASS, or vanilla gate ran).
- Set `succeeded=false` on defer, on catch, and initially (no
  validation yet).
- Keyed by (guid, epoch): a character switch or epoch change never
  inherits a prior success.

### Application boundary

```
function CanApply():
  guid = CurrentGuid(); epoch = Pipeline.Epoch
  if guid == null or epoch == null: return false
  if ReconciliationRequired: return false
  if pendingIntents.has(guid): return false
  if recoveryHolds.has(guid): return false
  (status, _) = Resolve(guid, epoch)
  if status != OK and status != NO_RECORD_CONFIRMED: return false
  vr = validationRecord
  if vr == null or vr.guid != guid or vr.epoch != epoch or not vr.succeeded:
    return false
  return true
```

### Invalidation

- Character/session change → record mismatch → ineligible until
  re-validated.
- Evaluation failure (prefix catch) → `succeeded=false` → ineligible.
- No path grants eligibility without a successful validation for the
  exact current (guid, epoch).

### Safe re-evaluation after transient waiting

If validation deferred on `HOLD_TRANSIENT` (epoch unavailable) and the
epoch later appears, the mod **may** perform a soft re-validation:
`Evaluate()` (pure read-only computation, no game-method invocation).
- Pass (`actual == allowed` under the resolved status) →
  `validationRecord.succeeded=true` for the current (guid, epoch).
- Discrepancy or uncertain status → hold (and `reconciliationRequired`
  if OK-discrepancy), exactly as the prefix would.
This is optional; without it, the next load validates normally.
Readiness alone never substitutes for it.

---

## 3. Durable recovery holds (Counterexample 3)

**Bug:** an in-memory flag plus balance recheck does not survive
gameplay that makes totals coincidentally match, a different save
snapshot, or a restart. The explicit-recovery requirement was not
guaranteed.

**Fix:** persist the hold with character/session identity and reason.

### Store section

```json
"recoveryHolds": {
  "<guid>": { "epoch": 123, "reason": "discrepancy actual=5 allowed=2", "recordedAt": "..." }
}
```

- Written by the prefix on OK-discrepancy (defer), before returning.
- `Resolve` checks `recoveryHolds.has(guid)` early (after
  `STORE_UNREADABLE`, alongside `PENDING_INTENT`) → `RECOVERY_HOLD` →
  defer + block.
- Cleared **only** by the explicit reconciliation procedure (which
  addresses the cause and removes the record in one durable write).
- A reconnect, restart, or coincidentally matching balance **never**
  clears it: `Resolve` returns the hold regardless of current totals.

### Failed hold write

If the hold write fails: retain the block via the in-memory flag,
log FATAL naming the failure, and **document the residual window**:
should the process crash before a successful persist *and* the totals
coincidentally match on next load, the hold is lost. The window
requires both a failed durable write and an intervening total change;
the block is retained for the current session regardless.

### Transitions (holds)

| From | Event | To |
|---|---|---|
| ACTIVE | OK-discrepancy at validation; hold persisted | HELD (RECOVERY_HOLD) |
| ACTIVE | OK-discrepancy; hold write fails | HELD (in-memory block; FATAL log; residual window documented) |
| HELD (RECOVERY_HOLD) | Restart / reconnect / totals match | HELD (unchanged; re-derived) |
| HELD (RECOVERY_HOLD) | Explicit reconciliation | ACTIVE or INITIALIZED (procedure removes hold + fixes cause) |

---

## 4. Consistency fixes

- **V-J corrected:** missing store with known guid/epoch →
  `STORE_UNREADABLE` (not `UNCERTAIN_HISTORY`). `UNCERTAIN_HISTORY`
  requires a readable store that cannot prove clean history.
- **Prefix prose corrected:** the original method body executes for the
  **vanilla gate** under `NO_RECORD_CONFIRMED` — i.e. it may PASS
  (no-op) or RESET, exactly as vanilla. (Not "only for RESET.")

---

## 5. Regression vectors (proposed; not verified until run)

Each asserts: no `ResetAllSkills`, no `SetSkillPoint`, loaded values
bit-identical, applicator never invoked unless stated, zero ack bytes,
recovery log emitted.

- **T-1 (intent write fails):** mock store write failure on intent.
  Attempt application → applicator does not mutate (balance unchanged);
  result `Blocked_IntentWriteFailed`; job not acked; no partial intent
  record; pipeline may retry the write later.
- **T-2 (unresolved intent → hold, no freshness, no replay):** enroll G;
  write intent for index 5; simulate restart. `Resolve(G, epoch)` →
  `PENDING_INTENT` (not `NO_RECORD_CONFIRMED`). Server replays index 5
  → pipeline holds; assert no mutation, no ack.
- **T-3 (intent closes Window A):** enroll G → intent written → mutate
  (0→1) → game saves → crash before `SaveGrantState`. Restart: intent
  present, no entry, `applied=[]`. `Resolve` → `PENDING_INTENT`;
  assert no vanilla reset. Replay of index 5 → not re-applied (no
  duplication).
- **T-4 (no auto re-enroll):** init file retains G **with**
  `enrolledAt`; delete `characterHistory[G]` and `entitlements[G]`
  (ledger `applied=[]`). `Resolve` → `UNCERTAIN_HISTORY`; assert defer
  + block; log cites lost enrollment record.
- **T-5 (defer ≠ later eligibility):** validation defers at load
  (`HOLD_TRANSIENT`, epoch null); epoch later appears and `Resolve`
  returns OK. Assert applications still blocked
  (`validationRecord.succeeded` false). Then soft re-validation passes
  → assert eligible.
- **T-6 (evaluation failure):** prefix `Evaluate()` throws → catch
  returns false. Assert `LoadSaveData` completes; `succeeded=false`;
  applications blocked; next clean load restores eligibility.
- **T-7 (durable discrepancy hold):** OK, actual=5, allowed=2 → prefix
  defers; `recoveryHolds[G]` persisted with (guid, epoch, reason).
  Simulate balance change to actual=2; restart. `Resolve` → hold
  persists (not cleared by matching totals). Assert explicit
  reconciliation still required; applications blocked.
- **T-8 (hold write fails):** mock `recoveryHolds` write failure on
  OK-discrepancy. Assert in-memory block retained for session; FATAL
  log documents the residual crash window; no silent clear.
- **T-9 (V-J corrected):** no store file; guid/epoch known → 
...[truncated 633 chars]
