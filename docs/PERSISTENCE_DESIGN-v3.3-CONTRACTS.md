> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v3.3 — Implementation Contracts (no implementation)

Short addendum clarifying four contracts. No build, deployment, save
edits, recovery, or replay. Tests marked **[PROPOSED]** are specified,
not verified until run.

---

## 1. Intent ownership: the active operation may finish its commit

A pending intent blocks *other* operations, but the owning in-flight
operation must be able to retry its **persistence-only** commit. The two
are distinguished by an owner operation ID.

### Store

```json
"pendingIntents": {
  "<guid>": { "epoch": 123, "indices": [5], "attemptedAt": "...",
              "ownerOpId": "op-9f3a…" }
}
```

`ownerOpId` is generated when the intent is written and held in memory
by the owning pipeline job. After restart it is unrecoverable by design:
any intent seen after restart is unresolved → hold.

### Rules

- **Owner's retry:** the job holding `ownerOpId` may re-attempt the
  atomic commit (entitlement + `applied` + intent removal in one file
  write). It must **never** re-invoke the game mutation after a
  Confirmed result. The retry path is persist-only.
- **All others blocked:** any operation without the matching in-memory
  `ownerOpId` — including server replays of the intent's indices and new
  indices for the same character — is held while the intent exists.
- **Ack ordering:** acknowledgement is emitted only after the commit
  succeeds. A failed commit → no ack; the job stays
  `AppliedAwaitingPersist` for owner retry.

### [PROPOSED] T-11 — commit failure → retry → one mutation → ack after commit

Enrolled G; grant index 6; intent written (owner op-7); mutation 1→2;
Confirmed. Mock `SaveGrantState` to fail once, then succeed.
- First persist: fails. Assert no ack bytes; job remains awaiting
  persist; balance still 2.
- Owner retry (op-7): succeeds. Assert applicator **not** re-invoked
  (balance still 2, not 3); entitlement points=2, `applied` includes 6,
  intent removed — atomically; **then** ack emitted.
- Assert exactly one game mutation across the whole sequence.

---

## 2. Validation success belongs to the current player load

`validationRecord` keyed by (guid, epoch) is insufficient: a character
unload/reload (same guid, same session) must invalidate it.

### Binding

```csharp
// Set by the validation prefix on each evaluation (each
// GameStartCheckSkillPoint call):
lastValidationEventId++                       // monotonic, per evaluation
validationRecord = {
  eventId,                                    // == lastValidationEventId
  playerRef: WeakReference<OcPlMaster>,       // the specific loaded object
  guid, epoch,
  succeeded: bool
}
```

`CanApply` additionally requires:
- `validationRecord.eventId == lastValidationEventId` (no newer
  validation superseded it),
- `playerRef` still targets the current `OcPlMng.LocalPl`
  (`ReferenceEquals`; a reloaded character is a new object, a
  same-object `LoadSaveData` rerun bumps `eventId`),
- guid/epoch match, `succeeded == true`.

A reload of the same guid in the same session therefore invalidates
eligibility until re-validated. Readiness (`_isLoaded`, player valid)
remains necessary but insufficient.

### Soft re-validation waits for full load

Mod-initiated soft re-validation (after transient `HOLD_TRANSIENT`
clears) may run **only** when `OcPlMaster._isLoaded == true` — i.e.
after `LoadSaveData` fully restored the save. Evaluating mid-load would
read pre-restore values and fabricate a discrepancy. In practice it is
triggered from the readiness poll's NOT READY → READY transition, never
before.

### Vanilla path: postfix readback, not prefix assumption

The prefix must not claim success for a method it allowed to run. For
`NO_RECORD_CONFIRMED` the prefix returns `true` (run original) with
`succeeded=false` pending; a **postfix** then confirms:

```
Postfix (runs after prefix allowed execution):
  if prefix deferred: return                    # nothing to confirm
  try:
    (actual, allowed) = Evaluate()               # readback
    validationRecord.succeeded = (actual == allowed)
    if not succeeded: Log.Fatal("post-validation readback mismatch")
  catch:
    validationRecord.succeeded = false
```

A thrown or incomplete validation (prefix catch, postfix catch, or
postfix never running) leaves `succeeded=false`; grants stay blocked.
`LoadSaveData` always completes.

---

## 3. Two-file enrollment: write order and partial states

Enrollment touches the store (`characterHistory`) and the init file
(`enrolledAt`). **Order: store first, init file second.** Mutation
(application) is forbidden until both are durable.

### Why this order

- Crash after store write, before init mark: `history` exists →
  `Resolve` sees partial enrollment → `UNCERTAIN_HISTORY` (hold). It
  does **not** treat the character as fresh, and does not re-enroll.
- Reverse order would leave init-marked-but-historyless state that
  v3.1 correctly refuses to re-enroll, stranding a fresh character.

### Eligibility gate

`IsEnrollmentComplete(guid)` ≡ `Store.characterHistory.has(guid)` **and**
`InitFile.get(guid).enrolledAt != null`. For a character with no
entitlement entry, `CanApply` requires this (in addition to §2). The mod
may retry the pending init-file mark on subsequent boundaries
(idempotent); eligibility waits for both writes.

### Partial states (all hold; none re-enroll)

| Store history | Init enrolledAt | Resolve | Notes |
|---|---|---|---|
| absent | absent | enroll (first contact) or UNCERTAIN | per v3.1 |
| present | present | proceed (NO_RECORD_CONFIRMED if clean) | complete |
| present | absent | UNCERTAIN_HISTORY | store-first crash; hold, may retry mark |
| absent | present | UNCERTAIN_HISTORY | defensive; should not occur with store-first order |

### [PROPOSED] T-12 — crash between enrollment writes

Guid in init file (no `enrolledAt`). Simulate: store
`characterHistory` write succeeds; init-file mark fails (crash).
Restart: history present, `enrolledAt` absent. Assert `Resolve` →
`UNCERTAIN_HISTORY` (not `NO_RECORD_CONFIRMED`, not re-enroll);
validation defers; applications blocked.

### [PROPOSED] T-13 — write failure between enrollment writes

As T-12 but the init write throws (caught). Assert eligibility still
requires both durable; the mod may retry the mark on the next boundary;
no grant is applied meanwhile.

---

## 4. Uncertain write outcomes (T-1 corrected)

A failed write does not prove the destination stayed unchanged (partial
bytes may have landed). T-1 is corrected: assert **no game mutation**
and **no acknowledgement** — not "no partial intent record."

### Conservative retry

```
WriteIntent(guid, epoch, indices):
  try:
    Store.pendingIntents[guid] = {...}
    Store.save(); return SUCCESS
  catch:
    # Outcome uncertain: re-read before concluding.
    try:
      Store.reload()
      if Store.pendingIntents.has(guid): return UNCERTAIN_HOLD
      else: return FAILED_CLEAN
    catch:
      return UNCERTAIN_HOLD
```

- `UNCERTAIN_HOLD`: do not mutate; treat as pending intent (hold).
- `FAILED_CLEAN`: confirmed absent; safe to retry the write later; still
  no mutation has occurred.

### [PROPOSED] T-1 (revised)

Mock intent write to fail. Attempt application. Assert: game balance
unchanged (no mutation); zero ack bytes; pipeline does not proceed.
Then re-read: if a partial intent is observed → `PENDING_INTENT` hold;
if confirmed absent → job remains queued for write-retry. Either way,
no mutation and no ack precede a confirmed durable intent.

---

## 5. Extended success coverage (all [PROPOSED])

- **T-14 — second grant for ACTIVE character:** entitlement points=1,
  indices=[5], applied=[5]. Grant index 6 → intent (owner op) →
  mutate 1→2 → Confirmed → commit (points=2, indices=[5,6],
  applied=[5,6], intent removed) → ack. Assert no duplication;
  exactly one mutation for index 6.
- **T-15 — lost ack + replay:** index 6 applied + persisted; ack bytes
  lost (simulated). Server replays index 6 → pipeline dedups via
  `applied` (no re-mutation); re-emits ack without touching the game.
  Assert one mutation total; ack eventually delivered.
- **T-16 — intent preserves dedup:** pending intent for index 5;
  server replays 5 → held (no re-apply, no ack). Server sends new
  index 6 → held (character-level block). Separately, with no intent:
  applied=[5], replay 5 → dedup skip + re-ack (existing behavior
  unchanged).

---

## 6. Consistency fixes (confirmed)

- **V-J:** missing store with known guid/epoch → `STORE_UNREADABLE`
  (v3.2 §5 updated accordingly).
- **Prefix prose:** the original method body executes for the **vanilla
  gate** under `NO_RECORD_CONFIRMED` — it may PASS (no-op) or RESET,
  exactly as vanilla. Section 3's "only for vanilla RESET" is corrected.

---

Character-identity evidence at the load-time boundary and review of the
actual game method remain prerequisites for Unity integration. No
deployment, save changes, or recovery of existing indices.

