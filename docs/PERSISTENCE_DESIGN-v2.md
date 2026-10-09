> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v2: AP-Earned Skill Points vs `GameStartCheckSkillPoint`

**Status:** Proposal only. No code changes, no DLL, no deployment.
**Supersedes:** `PERSISTENCE_DESIGN.md` (v1). v1 is retained for the record;
v2 corrects five blockers identified in review.

---

## 1. Mechanism (unchanged from v1; strongly supported)

`Oc.Skills.OcSkillManager.GameStartCheckSkillPoint()` enforces
`assigned + unspent == (level-1)*K + missionPoints`, resetting skills and
unspent points on mismatch. `OcPlMaster.LoadSaveData()` calls it after
restoring saved SP but before `_isLoaded=true`. AP-earned points are not
part of `allowed`, so the next load wipes them. Runtime branch tracing
(the Japanese reset line in Player.log) is still not captured.

---

## 2. Core principle (corrected)

**"Unknown" must never be confused with "zero," and uncertainty must never
trigger destructive validation.** v1's "fail closed to 0" was wrong: 0 is
a claim ("this character is entitled to nothing"), and the validator turns
an incorrect 0 into a wipe of legitimate points and allocated skills.

The entitlement query therefore returns a **status**, and every uncertain
status **defers** the validation for that load — it does not zero the
allowance, does not skip validation silently, and does not throw.

### 2.1 Statuses and policies

| Status | Condition (§2.2–2.4) | Validation | New grants |
|---|---|---|---|
| `OK` | Healthy store, entry for (character GUID, epoch) | Runs; `allowed += points` | Allowed |
| `NO_RECORD_CONFIRMED` | Positive evidence of no AP history (§2.2) | Runs; `allowed += 0` (vanilla) | Allowed |
| `UNCERTAIN_HISTORY` | Store/ledger cannot prove clean history | **Deferred** — see §2.5 | **Blocked** |
| `STORE_UNREADABLE` | Primary store corrupt or unexpectedly missing | **Deferred** — see §2.5 | **Blocked** |
| `EPOCH_MISMATCH` | Entry exists, epoch differs (new AP session) | **Deferred** — see §2.5 | **Blocked** |

Only `OK` and `NO_RECORD_CONFIRMED` let the validator run. All other
states preserve loaded points and allocated skills exactly as loaded,
block new grants (the pipeline refuses to apply or ack, logging the
reason), and require the reviewed recovery/reconciliation before the mod
does anything further.

### 2.2 `NO_RECORD_CONFIRMED` — strong evidence required

A readable file does **not** prove the character never earned points: a
legacy backup or pre-v0.3.7 ledger can lack entitlement records while the
save holds granted points. `NO_RECORD_CONFIRMED` requires ALL of:

1. Store file exists, parses, carries `formatVersion >= 1` (v0.3.7+ marker).
2. No entitlement entry for this character GUID.
3. Grant ledger (same file) has `applied` empty **and** `quarantined` empty.
4. No legacy indicators (no pre-v0.3.7 fields, no version mismatch).

If any check fails → `UNCERTAIN_HISTORY`, not `NO_RECORD`. First-ever run
(no files at all) is treated as `NO_RECORD_CONFIRMED`; deleting the files
is treated the same way, with the documented consequence that
unverifiable points in the save will be wiped by vanilla validation —
do not delete the files.

**Backups:** the `.bak` is written for human recovery only. It is never
used for live allowance decisions, because parsing successfully does not
prove freshness. Freshness (version/timestamp comparison against the
ledger and logs) is checked during the manual recovery procedure.

### 2.3 `STORE_UNREADABLE`

Primary store corrupt (fails to parse) or missing when the ledger shows
history. The mod cannot distinguish "no points" from "lost points," so:

- Validation is **deferred** (§2.5): loaded points and skills preserved.
- New grants **blocked** (we cannot record new entitlements either).
- FATAL log naming the file and the failure.
- Recovery: reviewed procedure rebuilds the store from the grant ledger,
  game logs, and the `.bak` (with freshness checks); grants resume only
  when a healthy store exists. If the data is truly unrecoverable, the
  safe state is to remain blocked until the operator consciously resets.

### 2.4 `EPOCH_MISMATCH` — hold, do not zero, do not rebind

Strict session binding is kept, but neither destructive zeroing nor
automatic cross-epoch rebinding is approved. On epoch mismatch:

- Validation is **deferred** (§2.5): previously earned points and skills
  are preserved as loaded.
- New grants **blocked** for this session.
- Loud startup log: `entitlement bound to epoch X, current session epoch
  Y — reconciliation required; grants suspended`.
- Reconciliation is explicit and manual: the operator either re-binds the
  entitlement to the new epoch (points carry over, consciously) or
  zeroes it (points wiped, consciously). Until then the mod holds.

### 2.5 Narrowly scoped deferral (not a bypass, not an exception)

Mechanism (specified, not built): a Harmony **prefix** on
`GameStartCheckSkillPoint` that returns `false` — skipping only that
method's body — when `EntitlementStore.Status` is uncertain
(`UNCERTAIN_HISTORY`, `STORE_UNREADABLE`, `EPOCH_MISMATCH`).

- **Narrow:** only this method, only while uncertain. When certain
  (`OK`, `NO_RECORD_CONFIRMED`), the method runs normally with the
  transpiler-adjusted allowance (§3).
- **Not a blanket bypass:** the validator is not disabled; this load's
  invocation is deferred because the correct `allowed` total cannot be
  computed. The condition persists until recovery, so every load defers
  until resolved — there is no silent path to unvalidated points.
- **Not an exception:** `LoadSaveData` continues normally (`_isLoaded`
  is set, the world finishes loading). Only the destructive check is
  held back.
- **Loud:** FATAL (unreadable/uncertain) or WARNING (epoch) each time it
  defers, naming the status and the required action.

### 2.6 Ordering windows (honest account; v1's "self-healing" claim removed)

The game save and our ledger cannot be made atomic. Two windows exist:

**Window A — game saves the grant before our ledger persists.**
Sequence: applicator Confirms (delta 0→1 verified) → game autosaves
(save now holds +1) → crash before `SaveGrantState` → our file still
shows `applied=[]`. On restart the server replays the unacked index; the
pipeline treats it as fresh; the applicator verifies the delta (1→2,
correct as a delta) and Confirms again. **Result: duplication** — the
player holds 2, entitled to 1. The applicator's delta check cannot detect
this; pipeline dedup is ledger-based, not save-based. On the next load
the validator sees `actual > allowed` and resets to the entitled total —
correcting the count but wiping assigned skills. Reconciliation: the operator
reviews logs to confirm the duplication; skill re-allocation is manual.
This window is inherent to the architecture; v0.3.7 does not close it.

**Window B — our ledger persists before the game saves.**
Sequence: applicator Confirms → `SaveGrantState` writes
`applied=[idx]`, entitlement=1 → crash before game save → save still
holds 0. On restart: `actual (0) < allowed (0+1)`. Policy: the shortfall
is attributable to our bookkeeping running ahead of the save, so the
validator **must not** run its destructive reset (which would wipe
allocated skills to grant points already owed). Instead the seam clamps
`allowed` to `actual` for this load — the check passes, skills are
preserved — and logs the owed-points discrepancy loudly. Reconciliation
of the owed points is via the reviewed procedure. (A future enhancement
may top-up unspent points without touching skills; not specified here.)

If `actual < allowed` and the shortfall exceeds the entitlement (vanilla
points also missing), the cause is unclear → `DEFER` + manual recovery.

### 2.7 Decision logic (pure function; headless-testable)

```
Decide(status, actual, vanillaAllowed, entitlementPoints):
  if status in {UNCERTAIN_HISTORY, STORE_UNREADABLE, EPOCH_MISMATCH}:
    return DEFER            // §2.5: preserve, block, recover
  allowed = vanillaAllowed + (entitlementPoints if status == OK else 0)
  if actual == allowed:     return PASS
  if actual > allowed:      return RESET     // game's normal reset
  // actual < allowed: save behind
  if status == OK and (allowed - actual) <= entitlementPoints:
    return CLAMP            // §2.6 Window B: force pass, preserve skills, log owed
  if status == NO_RECORD_CONFIRMED:
    return RESET            // vanilla behavior; no AP involved
  return DEFER
```

`RESET` is the game's own branch (unchanged). `DEFER`, `CLAMP`, and the
status gate are the mod's additions.

---

## 3. Seam specification (corrected for the status interface)

v1's sketch called an int-returning `GetApEntitlement()`; the status
interface requires two hooks on `GameStartCheckSkillPoint`:

1. **Prefix** — `ShouldDeferValidation() -> bool`. Reads the cached
   `EntitlementStore.Status` (determined in `Awake`, before any
   `LoadSaveData`). Returns true → Harmony skips the original method
   (§2.5). Returns false → execution proceeds.
2. **Transpiler** — after the single `stloc.0` (pattern
   `call GetTotalSkillPointTaken → add → stloc.0`), injects
   `allowed += GetApEntitlementPoints()`, where the latter returns the
   points for `OK`, else 0. Only reached when the prefix allowed
   execution, so 0 here is safe (`NO_RECORD_CONFIRMED` only).
3. **Ahead-case handling** — the `actual < allowed` clamp (§2.6/§2.7)
   requires observing `actual` after it is computed (post-`stloc.1`
   region). Mechanism TBD in implementation; behavior is specified
   above.

Uncertainty is therefore handled **before** the destructive comparison:
the prefix defers, and the transpiler only ever sees certain states.

---

## 4. Entitlement record (unchanged from v1, plus version marker)

Per-character map in the per-slot grant state file, one atomic write with
the grant ledger; adds `formatVersion: 1`:

```json
{ "formatVersion": 1, "epoch": 8591074088996589973,
  "applied": [], "quarantined": [],
  "entitlements": { "<characterGuid>": {
      "epoch": 8591074088996589973, "points": 2, "indices": [2, 3] } } }
```

Population rules (unchanged): only by the new Confirmed path, never from
`applied.Count`; indices 0/1 excluded; indices 2/3 not auto-migrated —
both await the separate reviewed recovery. Staged by the applicator,
merged by `SaveGrantState`, stage cleared only on successful write.

Character identity: `OcPlCharaSaveData.GUID` via
`OcPlCharacterManager.GetCharacterGUID()` — **still unverified at the
load-time boundary** (see §7.3). No build until confirmed.

---

## 5. Test plan (assertions, not just warnings)

Headless tests target `Decide()` and the store/status logic; Harmony
wiring is verified in-game. For every uncertain status
(`UNCERTAIN_HISTORY`, `STORE_UNREADABLE`, `EPOCH_MISMATCH`), tests assert
**all** of: validator body not executed (no `ResetAllSkills`, no
`SetSkillPoint`, `SkillPoint` and `CurrentAssignedSP` bit-identical
before/after), no grant application attempted, no acknowledgment sent,
and the block/recovery log emitted — not merely that a warning appears.

Vectors:
1. `OK`, assigned=0, unspent=2, entitlement=2 → PASS, no reset.
2. `OK`, assigned=1, unspent=1, entitlement=2 → PASS, skills preserved.
3. `OK` repeated → entitlement unchanged, no growth.
4. `OK` for charA, queried as charB → not `OK` (no cross-character entry).
5. `NO_RECORD_CONFIRMED`, vanilla-consistent → PASS, allowance 0.
6. `NO_RECORD_CONFIRMED`, actual > allowed → RESET (vanilla).
7. Legacy ledger (no `formatVersion`) with applied=[0,1,2,3], no entry →
   `UNCERTAIN_HISTORY` → DEFER; assert no mutation, no application, no ack.
8. Corrupt primary → `STORE_UNREADABLE` → DEFER; assert the same.
9. Backup parses but primary corrupt → still `STORE_UNREADABLE`
   (backup never drives live decisions).
10. Entry epoch ≠ session epoch → `EPOCH_MISMATCH` → DEFER; assert the
    same; assert no auto-rebind and no zeroing.
11. `OK`, actual < allowed, shortfall ≤ entitlement → CLAMP; assert no
    `ResetAllSkills`, skills preserved, owed-points logged.
12. `OK`, actual < allowed, shortfall > entitlement → DEFER.
13. Blocked pipeline: grant arrives while status uncertain → assert
    applicator never invoked, no ack bytes emitted.

In-game (Codex, after implementation): with `OK`/entitlement=2, reload →
no reset line, UI 2, skills intact; spend 1, reload → still no reset.

---

## 6. What this preserves

Readiness gate, corrected Python 0.6.8 client, current ledgers, save
files: untouched. No migration, recovery, reset, or deployment. Ordinary
validation intact for certain states. Main character out of tests.
Indices 0/1 and 2/3 unchanged; no automatic migration or replay.

---

## 7. Known limitations & open items

- Window A duplication (inherent; §2.6). Window B owed-points
  reconciliation is manual.
- New-seed flow requires explicit reconciliation; the mod holds until
  the operator decides (§2.4).
- Mod removed → vanilla validation wipes AP points (correct).
- Entitlement file is local-trust (as is the save).
- **Character identity unverified at the load-time boundary** — blocking
  (see handoff v9.1 evidence request 3).
- The ahead-case clamp mechanism (§3.3) is specified but not wired;
  implementation must show the exact injection before review.

