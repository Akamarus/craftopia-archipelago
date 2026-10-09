> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v3: AP-Earned Skill Points vs `GameStartCheckSkillPoint`

**Status:** Proposal only. No code, no DLL, no deployment, no migration,
no replay, no save edits.
**Supersedes:** v2 (and v1). Corrects four further blockers.

---

## 1. Mechanism (unchanged; strongly supported)

`Oc.Skills.OcSkillManager.GameStartCheckSkillPoint()` enforces
`assigned + unspent == (level-1)*K + missionPoints`, resetting skills and
unspent points on mismatch. `OcPlMaster.LoadSaveData()` calls it after
restoring saved SP but before `_isLoaded`. AP-earned points are not in its
`allowed` total. Runtime branch tracing still not captured.

---

## 2. Principles

1. **Unknown ≠ zero, and absence ≠ fresh.** Missing files cannot
   distinguish a fresh install from lost history.
2. **Uncertainty → preserve + block + reconcile.** For any character
   with AP history (or uncertain history), the mod never triggers
   destructive validation automatically — either discrepancy direction.
3. **Explicit initialization.** Only the operator's attestation marks a
   character as verified-fresh.
4. **Resolve at each boundary.** File readability is determined at
   startup; character/session eligibility is resolved fresh at every
   validation and application boundary. Unknown identity or epoch holds.
5. **Vanilla validation only where AP absence is proven.**

---

## 3. Statuses

| Status | Condition | Validation | New grants |
|---|---|---|---|
| `OK` | Entry for (guid, epoch); epoch matches | Gate runs; `actual == allowed` → pass; **any discrepancy → DEFER** | Allowed only while passing |
| `NO_RECORD_CONFIRMED` | Guid in explicit init list; no entry | Vanilla gate; discrepancy → original RESET | Allowed |
| `UNCERTAIN_HISTORY` | No entry and guid NOT in init list; or legacy/inconsistent records | DEFER | Blocked |
| `STORE_UNREADABLE` | Store file unreadable | DEFER | Blocked |
| `EPOCH_MISMATCH` | Entry exists; epoch differs | DEFER | Blocked |
| `HOLD` | Guid or epoch unresolvable at the boundary | DEFER | Blocked |

`DEFER` (all six uncertain rows, plus `OK`-with-discrepancy): the
validator body does not execute — no `ResetAllSkills`, no
`SetSkillPoint`, loaded values bit-identical — new grants are blocked
(pipeline refuses to apply or ack), and the condition is logged loudly
with the required recovery action. The hold clears only via explicit
recovery/reconciliation, never silently.

For `OK`, **both** discrepancy directions defer. `actual > allowed` is
not evidence that destructive correction is safe (Window A, §6); `actual
< allowed` is not proven to be a benign shortfall (Window B, §6). Normal
vanilla RESET exists only under `NO_RECORD_CONFIRMED`.

---

## 4. Explicit initialization (replaces absence-inference)

v2 treated missing files as first-run. That is removed: absence cannot
prove freshness.

`craftopia-archipelago-poc-init-<slot>.json`:
```json
{ "initializedCharacters": {
    "<guid>": { "at": "2026-10-06T…Z", "note": "fresh test character, verified by the operator" } } }
```

- At startup (and whenever blocked) the mod logs the current character
  GUID: `[AP] character GUID <guid> has no AP records; grants blocked
  until explicitly initialized.`
- the operator verifies the character is fresh (no AP history) and adds its GUID.
- Init-list membership + no entitlement entry → `NO_RECORD_CONFIRMED`.
- Characters with history (e.g. the current test character) do **not**
  go in the init list; they receive entitlement entries via the reviewed
  recovery procedure.
- The init file is read fresh at each resolution (tiny file); no stale
  cache.

---

## 5. Resolution at boundaries (not cached in Awake)

**Awake:** read the store file bytes; record `FileStatus`
(READABLE/UNREADABLE) and parse if readable. Cache the *data*, never the
*decision*.

**At each validation boundary** (the prefix, §7) **and each application
boundary** (before the applicator runs), resolve fresh:

```
Resolve(guid, epoch):
  if FileStatus == UNREADABLE:            return STORE_UNREADABLE
  if guid == null:                        return HOLD
  entry = entitlements.get(guid)
  if entry != null:
    if epoch == null:                     return HOLD
    return OK if epoch == entry.epoch else EPOCH_MISMATCH
  else:
    return NO_RECORD_CONFIRMED if guid in initList else UNCERTAIN_HISTORY
```

- `guid` comes from the character-identity API; `epoch` from the
  pipeline's current session. Both are read at the boundary.
- Character switches (new `LoadSaveData`) and epoch changes (reconnect
  to a new session) are therefore always reflected; no stale status.
- Unknown identity or epoch → `HOLD` → defer + block.

**Application boundary** additionally checks the `reconciliationRequired`
flag (set when validation defers on a discrepancy; cleared only by
explicit reconciliation). Grants are blocked while it is set, even if
`Resolve` returns `OK`.

---

## 6. Ordering windows (honest account)

**Window A — game saves the grant before our ledger persists.**
Applicator Confirms (delta verified) → game autosaves (save holds +1) →
crash before `SaveGrantState` → ledger still `applied=[]`. Restart:
server replays the unacked index; the delta check passes again (1→2);
**duplication is possible** — the pipeline's dedup is ledger-based, not
save-based. On the next load under `OK`, `actual > allowed` → **DEFER**
(§3): the duplicate is preserved, grants blocked, reconciliation
required. The reviewed procedure dispositions the duplicate
case-by-case; the mod does not auto-correct it.

**Window B — ledger persists before the game saves.**
`SaveGrantState` writes the entitlement → crash before game save → save
holds the old total. Next load under `OK`: `actual < allowed`. The
shortfall being ≤ the entitlement does **not** prove its cause, so the
v2 clamp is removed: **DEFER**, preserve skills, block grants, log the
discrepancy loudly. No auto top-up, no silent pass. Reconciliation is
manual via the reviewed procedure.

---

## 7. Seam: single gating prefix (contract clarified)

One Harmony prefix on `GameStartCheckSkillPoint`. Terminology, precisely:

- Helper `ShouldDeferValidation() -> bool`: pure decision logic;
  `true` means "defer."
- The Harmony prefix itself returns `false` to **skip** the original
  method, `true` to **run** it. These are not interchangeable.

```csharp
// Harmony prefix. false = skip original; true = run original.
static bool Prefix_GameStartCheckSkillPoint() {
    var (status, actual, allowed) = Evaluate();   // Resolve + replicate computation
    if (status == NO_RECORD_CONFIRMED && actual != allowed)
        return true;    // vanilla RESET only: run the original method
    if (ShouldDefer(status, actual, allowed)) {
        Log.Fatal("[AP] deferring skill-point validation: status=" + status +
                  " actual=" + actual + " allowed=" + allowed +
                  "; points/skills preserved, grants blocked, recovery required");
        if (actual != allowed) ReconciliationRequired = true;
        return false;   // skip original
    }
    return false;       // PASS (or certain-no-op): skip; nothing to do
}
```

- `Evaluate()` replicates the vanilla computation (level, mission
  points, assigned, unspent) and the allowance; it is pure and
  headless-testable.
- The original method body executes **only** for vanilla RESET under
  `NO_RECORD_CONFIRMED`. In every other case the prefix skips it.
- This is gating, not a blanket bypass: the validator runs exactly when
  its inputs are trustworthy and its behavior is vanilla.
- `LoadSaveData` otherwise completes normally (`_isLoaded` is set);
  only the destructive check is held back — never an exception, never
  an incomplete load.

---

## 8. Entitlement record

Per-character map in the per-slot grant state file, one atomic write
with the grant ledger; `formatVersion: 1`. Populated only by the new
balance-verified Confirmed path; never from `applied.Count`; indices
0/1 excluded; indices 2/3 not auto-migrated. Staged by the applicator,
merged by `SaveGrantState`, stage cleared only on successful write.
`.bak` is recovery-only and never drives live decisions.

Character identity: `OcPlCharaSaveData.GUID` via
`OcPlCharacterManager.GetCharacterGUID()` — **still an evidence blocker**
(§10.3), and per §5 it must resolve at the load-time boundary, not just
exist as an API.

---

## 9. Regression vectors

Headless tests target `Evaluate`/`ShouldDefer`/`Resolve`; Harmony wiring
is verified in-game. Every DEFER/HOLD vector asserts **all** of: no
`ResetAllSkills`, no `SetSkillPoint`, `SkillPoint` and
`CurrentAssignedSP` bit-identical before/after, applicator never
invoked, zero ack bytes emitted, and the recovery log emitted.

1. Missing all files (no store, no ledger, no init) → `UNCERTAIN_HISTORY`;
   assert defer + block (this is the corrected §4 behavior).
2. Missing all files + guid in init list → `NO_RECORD_CONFIRMED`;
   assert vanilla gate runs.
3. Readable but stale ledger (legacy, no `formatVersion`, applied
   non-empty, no init) → `UNCERTAIN_HISTORY`; assert defer + block.
4. `OK`, `actual > allowed` → DEFER; assert preservation + block + no ack.
5. `OK`, `actual < allowed` → DEFER; assert preservation + block + no ack
   (clamp removed).
6. `OK`, `actual == allowed` → PASS; assert no interference.
7. `NO_RECORD_CONFIRMED`, `actual > allowed` → original RESET runs;
   assert vanilla behavior.
8. Character change after startup: resolve(guid=A) → `OK`; resolve
   (guid=B, no entry, not in init) → `UNCERTAIN_HISTORY`; assert no
   stale status.
9. Epoch change after startup: epoch=X → `OK`; epoch=Y → `EPOCH_MISMATCH`;
   assert re-resolution, no auto-rebind, no zeroing, grants blocked.
10. Null guid at boundary → `HOLD`; assert defer + block.
11. Null epoch with existing entry → `HOLD`; assert defer + block.
12. Grant arrives while `UNCERTAIN_HISTORY` → applicator not invoked.
13. Grant arrives while `reconciliationRequired` set → blocked, no ack,
    hold not silently cleared.

In-game (Codex, post-implementation): `OK`/entitlement=2 → reload shows
no reset line, UI 2, skills intact; spend 1, reload → still no reset;
delete store file → next load defers with the init log line (not a wipe).

---

## 10. Preserves, limits, open blockers

**Preserves:** readiness gate, corrected Python 0.6.8 client, current
ledgers, save files. No migration, recovery, reset, deployment, replay,
or save edits. Main character out of tests. Indices 0/1 and 2/3
unchanged.

**Limits:** Window A duplication is possible and reconciled manually;
Window B shortfalls are manual; new-seed flow holds until the operator
reconciles; mod removal → vanilla wipes AP points (correct); files are
local-trust — do not delete them.

**Open blockers (all must clear before implementation):**
1. Explicit-initialization workflow approval (init-list JSON, §4).
2. Single-prefix seam review (§7), including the replicated-computation
   approach.
3. Character identity evidence at the load-time boundary: does
   `OcPlCharacterManager.Inst.GetCharacterGUID()` return the loading
   character's stable GUID during `LoadSaveData`? Does the test
   character's GUID differ from the main's? If unsuitable, the correct
   API (not a guess).
4. Validator call sites; `GetTotalSkillPointTaken` callers (seam
   uniqueness); Japanese reset line in Player.log;
   `INCREASE_SKILLPOINT_BY_LEVEL_UP` value.
5. The reviewed recovery/reconciliation procedure itself (separate
   document; covers duplication disposition, 0/1, 2/3, epoch re-bind).

