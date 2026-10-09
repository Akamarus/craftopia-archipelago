> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v3.1 — Focused Corrections (no implementation)

Corrects four inconsistencies in v3. Returns only: state-transition
table, corrected `Resolve`/prefix pseudocode, hold lifetimes, and added
regression vectors. No full rewrite; no build, deployment, recovery, or
save edits.

---

## 1. State-transition table (per character GUID)

Init file = the operator's write-only attestation. Store's `characterHistory`
record = authority after enrollment. **Init-list membership never
restores freshness once history exists or is uncertain.**

| From | Event / Condition | To | Effect |
|---|---|---|---|
| UNSEEN | Guid added to init file; store healthy; first boundary | INITIALIZED | One-time enrollment; durable `firstSeen` (+ ledger counts) written before any mutation |
| UNSEEN | Boundary; guid not in init file | HELD (UNCERTAIN_HISTORY) | Defer validation; block grants/acks |
| UNSEEN | Store missing or corrupt | HELD (STORE_UNREADABLE) | Defer; block; never auto-create store |
| INITIALIZED | First grant Confirmed | ACTIVE | Entitlement entry created |
| INITIALIZED | Boundary; `firstSeen` intact; ledger counts unchanged; no entry | INITIALIZED | NO_RECORD_CONFIRMED; vanilla gate |
| INITIALIZED | Entry absent but ledger shows grants (or ledger inconsistent) | HELD (UNCERTAIN_HISTORY) | History lost; init file does NOT restore INITIALIZED |
| ACTIVE | Entry epoch == session epoch; `actual == allowed` | ACTIVE | OK; gate passes |
| ACTIVE | Entry epoch ≠ session epoch | HELD (EPOCH_MISMATCH) | Defer; block; explicit re-bind or zero |
| ACTIVE | `actual ≠ allowed` (either direction) | HELD (DISCREPANCY) | Defer; block; `reconciliationRequired`; explicit reconcile |
| ACTIVE | Store unreadable | HELD (STORE_UNREADABLE) | Defer; block |
| HELD (*) | Explicit recovery/reconciliation mutates persistent state | INITIALIZED or ACTIVE | Only via procedure; never via transient events |
| Any | Guid or epoch null at boundary | HOLD (transient) | This evaluation only; auto-clears; not a state change |

There is no transition from ACTIVE or HELD back to INITIALIZED via the
init file. Removing a guid from the init file does not revoke an
established `firstSeen` (revocation is via procedure).

---

## 2. Corrected `Resolve` pseudocode

Epoch-null is checked **before** entry classification (Issue 2).
Enrollment is one-time; history loss after enrollment never returns
`NO_RECORD_CONFIRMED` (Issue 1). Missing/corrupt/explicit states are
distinguished (Issue 4).

```
function Resolve(guid, epoch) -> (status, detail):
  # Called fresh at EVERY validation and application boundary.
  # Never cached across boundaries.

  if guid == null:
    return (HOLD_TRANSIENT, "character identity unavailable")
  if epoch == null:
    return (HOLD_TRANSIENT, "AP session epoch unavailable")
    # Applies whether or not an entry exists (Issue 2).

  if not Store.exists():
    return (STORE_UNREADABLE, "store file missing; create via explicit procedure only")
  if not Store.parses():
    return (STORE_UNREADABLE, "store file corrupt; backup is recovery-only, never a live input")

  history = Store.characterHistory.get(guid)  # durable firstSeen, or null
  entry   = Store.entitlements.get(guid)      # entitlement entry, or null
  initialized = InitFile.contains(guid)

  if entry != null:
    if epoch == entry.epoch:
      return (OK, "entitled to " + entry.points + " points")
    else:
      return (EPOCH_MISMATCH, "entry epoch " + entry.epoch + ", session epoch " + epoch)

  # No entitlement entry.
  if history == null:
    if initialized:
      # One-time enrollment. Durable evidence BEFORE any mutation.
      # Idempotent; the init file alone never re-triggers it.
      Store.characterHistory[guid] = {
        initializedAt: now(),
        appliedAtEnroll: Store.applied.count,
        quarantinedAtEnroll: Store.quarantined.count
      }
      Store.save()  # own file; atomic; safe at boundary
      return (NO_RECORD_CONFIRMED, "enrolled as verified-fresh")
    else:
      return (UNCERTAIN_HISTORY, "no entry and not initialized")
  else:
    # Was initialized. Init file membership is now irrelevant.
    if Store.isConsistent() \
       and Store.applied.count == history.appliedAtEnroll \
       and Store.quarantined.count == history.quarantinedAtEnroll:
      return (NO_RECORD_CONFIRMED, "still fresh; no grants since enrollment")
    else:
      return (UNCERTAIN_HISTORY,
              "history inconsistent or lost since " + history.initializedAt +
              "; init file does not restore freshness")
```

**Missing vs corrupt vs explicit (Issue 4):**
- *Missing:* `STORE_UNREADABLE` (detail: missing). The mod never
  creates the store file automatically — not on first run, not when the
  init file names a guid. Log directs the operator to the explicit procedure.
- *Corrupt:* `STORE_UNREADABLE` (detail: corrupt). `.bak` is never
  consulted for live decisions.
- *Explicitly initialized:* the operator manually writes the documented minimal
  template (`formatVersion: 1`, empty sections). Next boundary enrolls
  normally. Manually writing an empty store over a lost history is an
  explicit reset attestation — documented as such, never automatic.

---

## 3. Corrected prefix pseudocode (contract precise)

Helper returns `true` for "defer." Harmony prefix returns `false` to
SKIP the original method, `true` to RUN it. Not interchangeable.

```
# Pure decision helper.
function ShouldDefer(status, actual, allowed) -> bool:
  switch status:
    case UNCERTAIN_HISTORY, STORE_UNREADABLE, EPOCH_MISMATCH, HOLD_TRANSIENT:
      return true
    case NO_RECORD_CONFIRMED:
      return false   # original runs: vanilla PASS (no-op) or RESET
    case OK:
      return actual != allowed   # defer on either discrepancy
  return true  # unknown: conservative

# Harmony prefix on GameStartCheckSkillPoint.
function Prefix_GameStartCheckSkillPoint() -> bool:
  try:
    guid  = CharacterIdentity.CurrentGuid()  # may return null or throw
    epoch = Pipeline.Epoch                   # may be null
    (status, detail) = Resolve(guid, epoch)

    actual = null; allowed = null
    if status == OK or status == NO_RECORD_CONFIRMED:
      (actual, allowed) = Evaluate()
      # Evaluate replicates vanilla computation + allowance; pure & testable.

    if ShouldDefer(status, actual, allowed):
      Log.Fatal("[AP] validation deferred: status=" + status +
                " detail=" + detail +
                " | points/skills preserved, grants blocked, recovery required")
      if status == OK:
        ReconciliationRequired = true  # session-scoped; blocks applications
      return false  # SKIP original

    if status == NO_RECORD_CONFIRMED:
      return true   # RUN original: vanilla gate only

    return false    # OK and actual == allowed: PASS; original would no-op

  catch e:
    # Conservative: never interrupt character loading, never fall through
    # to destructive validation.
    Log.Fatal("[AP] validation gate failed (" + e + "); deferring to preserve character")
    return false  # SKIP original
```

The original method body executes **only** for vanilla RESET under
`NO_RECORD_CONFIRMED`. All other paths skip it. `LoadSaveData` always
completes (`_isLoaded` is set); only the destructive check is gated.

---

## 4. Hold lifetimes, scopes, and clearing (Issue 3)

**Transient waiting** (`HOLD_TRANSIENT`): guid or epoch not yet
available. Evaluation-scoped; auto-clears when the info appears; never
requires explicit reconciliation; never recorded.

**Recovery holds** — derived from persistent state, so they survive
restart by re-derivation. They clear **only** when persistent state
changes via the explicit procedure. Invariant: *no transient event
(reconnect, re-read, re-resolve) may transition a recovery hold to
clear; re-derivation must return the same hold unless the underlying
persistent condition was explicitly resolved.*

| Hold | Scope | Clears when |
|---|---|---|
| STORE_UNREADABLE | Slot (file is per-slot) | Healthy store exists via recovery/explicit creation |
| UNCERTAIN_HISTORY | Character (guid) | Guid enrolled (init + firstSeen) or entitlement created via recovery |
| EPOCH_MISMATCH | Character + session | Explicit re-bind or zero via procedure. Reconnecting re-derives: same entry vs new epoch → still held (possibly different epochs); vs original epoch → OK (condition genuinely satisfied, not "accidental") |
| OK-discrepancy (`reconciliationRequired`) | Character + session | Explicit reconciliation. Re-derived on every validation; restart re-detects rather than clearing |

`reconciliationRequired` is session-scoped (in-memory), set by the
prefix on OK-discrepancy, checked by the application boundary alongside
`Resolve`. It is never cleared by reconnects or file re-reads.

---

## 5. Added regression vectors

Each asserts: no `ResetAllSkills`, no `SetSkillPoint`, `SkillPoint` and
`CurrentAssignedSP` bit-identical, applicator never invoked, zero ack
bytes, recovery log emitted.

- **V-A (Issue 1):** enroll G (init file; first boundary →
  NO_RECORD_CONFIRMED) → grant index 5 Confirmed (entry created,
  applied=[5] → OK) → delete `entitlements[G]` (keep `characterHistory[G]`,
  init file, applied=[5]) → `Resolve(G, epoch)` must be
  UNCERTAIN_HISTORY, **not** NO_RECORD_CONFIRMED. Assert defer + block +
  no vanilla reset; log cites history loss.
- **V-B (Issue 2a):** entry exists for G (epoch X); `Resolve(G, null)` →
  HOLD_TRANSIENT. Assert defer + block.
- **V-C (Issue 2b):** G enrolled (`firstSeen` recorded), no entry;
  `Resolve(G, null)` → HOLD_TRANSIENT. Assert defer + block (epoch
  checked before classification).
- **V-D (Issue 4):** delete store file; init file retains G →
  STORE_UNREADABLE (detail: missing). Assert store file is NOT
  re-created; defer + block; log names the explicit procedure.
- **V-E (Issue 4):** write garbage to store file → STORE_UNREADABLE
  (detail: corrupt). Assert `.bak` is not consulted; defer + block.
- **V-F (Issue 4):** the operator writes the documented empty template;
  init file has G → enroll → NO_RECORD_CONFIRMED. Assert vanilla gate
  runs; grants allowed.
- **V-G (exception safety):** `CharacterIdentity.CurrentGuid()` throws →
  prefix catch → return false. Assert no exception propagates
  (`LoadSaveData` completes); validation deferred; applications blocked
  for the session.
- **V-H (Issue 3):** EPOCH_MISMATCH (entry X, session Y) → reconnect
  with epoch Z → still EPOCH_MISMATCH (X vs Z); assert hold not cleared.
  Transient: `Resolve(G, null)` → HOLD; then `Resolve(G, X)` → OK;
  assert auto-clear is limited to the transient case.
- **V-I (both discrepancy directions, OK):** `actual > allowed` and
  `actual < allowed` → DEFER in both; assert preservation + block +
  `reconciliationRequired` set; assert no clamp, no auto-reset.
- **V-J (v3 vector, updated):** missing-all-files (no store, no ledger,
  no init) → UNCERTAIN_HISTORY (not first-run allowance); assert defer
  + block.

