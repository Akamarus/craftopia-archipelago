> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design: AP-Earned Skill Points vs `GameStartCheckSkillPoint`

**Status:** Proposal only. No code changes, no DLL, no deployment.
**Scope:** v0.3.6 passed visible delivery (indices 2/3 → UI 0→1→2) and bridge
reconnect, but a save/exit/relaunch wiped the points (UI 0). This design
makes legitimate AP-earned points survive the game's load-time validator.

---

## 1. Mechanism (strongly supported by static IL + saved/UI evidence)

`Oc.Skills.OcSkillManager.GameStartCheckSkillPoint()` (from
`skill-check-il.txt`):

```
allowed = (Level - 1) * INCREASE_SKILLPOINT_BY_LEVEL_UP
        + OcMissionManager.Inst.GetTotalSkillPointTaken()   // IL_0027 stloc.0
actual  = OcSkillManager.Inst.CurrentAssignedSP            // assigned skills
        + LocalPl.SkillCtrl.SkillPoint                      // unspent
if (actual != allowed):                                     // IL_0045 beq.s
    log "スキルポイント数不正：取得スキル、スキルポイントをリセットします"
    OcSkillManager.ResetAllSkills()
    SkillCtrl.ResetAllSkills()
    SkillCtrl.SetSkillPoint(allowed)                        // wipes to vanilla
    EquipCtrl.OnSkillReset(); PlBuffCtrl.OnSkillReset()
    OcItemUI_HotkeyMng.OnResetSkill()
```

`OcPlMaster.LoadSaveData()` sequence (`readiness-il.txt`):
1. `IL_02A6`: `SkillCtrl.SetSkillPoint(savedValue)` — restores saved unspent
   (2 in the observed case; DB inspection confirmed `SkillPoint=2`).
2. `IL_0307`: `GameStartCheckSkillPoint()` — `actual` (0 assigned + 2
   unspent = 2) vs `allowed` ((1-1)*K + 0 missions = 0) → mismatch → reset
   to 0, skills wiped.
3. `IL_032C`: `_isLoaded = true` — our readiness gate opens only after.

So the v0.3.6 readiness gate worked (application happened post-validation),
and the points were saved — but the *next* load's validator wiped them
because AP points are not part of its `allowed` computation.

**Not yet observed:** runtime branch tracing (whether the Japanese reset
line appears in Player.log for that session). The static path + saved/UI
evidence strongly supports the explanation; the log line would confirm it.

---

## 2. Design: AP Entitlement Allowance

**Core idea:** add a verified AP entitlement to the validator's `allowed`
total, preserving the check itself:

```
allowed = (level-1)*K + missionPoints + apEntitlement
```

- The comparison still runs; real mismatches (memory editors, corrupt
  saves) still trigger reset. No blanket skip.
- Already-allocated skills are preserved: the validator compares
  `assigned + unspent` against the total, so spent AP points are covered
  by the same allowance. No regranting after loading — the points stay in
  the save; we only stop the validator from wiping them.

### 2.1 Seam (recommended)

**Primary: transpiler on `GameStartCheckSkillPoint`.** After the single
`stloc.0` at IL_0027 (matched by pattern:
`call GetTotalSkillPointTaken → add → stloc.0`), inject:

```
ldloc.0
call int32 CraftopiaArchipelago.SkillPointValidator::GetApEntitlement()
add
stloc.0
```

Precisely scoped to the validator; no other method is affected.

**Fallback: postfix on `OcMissionManager.GetTotalSkillPointTaken()`**
(`__result += GetApEntitlement()`). Simpler (no IL), but only viable if
Codex confirms the validator is its sole caller; otherwise it would
corrupt other displays/logic. Decision pending evidence §6.2.

### 2.2 `GetApEntitlement()` — explicit failure policy (corrected)

**Correction:** returning 0 on a read failure is NOT harmless. If the save
holds legitimate AP points but the entitlement cannot be read, 0 makes the
validator wipe those points and all allocated skills. "Unknown" must never
be confused with "zero." The function therefore returns a status, not a
bare int, and every non-OK status has an explicit policy:

| Status | Meaning | Allowance | Validator effect | Recovery |
|---|---|---|---|---|
| `OK` | Record found, character+epoch match | `points` | AP-aware check | None needed |
| `NO_RECORD` | Store healthy, no entry for this character | 0 | Vanilla check | None — character never earned AP points |
| `STORE_UNREADABLE` | File missing* or corrupt (primary + backup) | 0 | Vanilla check (may wipe) | **FATAL log + persistent `recoveryNeeded` flag + all new grants blocked** until the operator runs the reviewed recovery (rebuild from grant ledger + logs, clear flag) |
| `EPOCH_MISMATCH` | Record exists, epoch differs (new AP seed) | 0 | Vanilla check (may wipe old points) | **WARNING log at startup** naming both epochs; re-bind only via explicit reviewed procedure |

\* "File missing" is split: if the grant ledger shows applied indices but
no store exists, that is `STORE_UNREADABLE` (suspicious — grants happened,
record lost). If neither exists, it is first-run → treated as `NO_RECORD`.

- The store is written via temp-file + atomic rename, with a `.bak` kept
  from the last good write; "corrupt" means both failed to parse.
- `STORE_UNREADABLE` blocks new grants because we cannot record new
  entitlements either — applying without recording would compound the
  inconsistency. The pipeline refuses to apply and logs the reason.
- `EPOCH_MISMATCH` → 0 is a deliberate strict session binding (per the
  requirement), but it is LOUD, not silent: the wipe, if it happens, is
  attributable to a logged line, and recovery is explicit. If lenient
  cross-epoch honoring is preferred, it is a one-line policy change —
  confirm before implementation.
- The validator itself is never skipped in any status.

### 2.3 Character identity (found in DLL; runtime confirmation needed)

- `Oc.OcPlCharaSaveData` has a **public `Int32 GUID`** field.
- `Oc.OcPlCharacterManager` (singleton) exposes `public Int32 GetCharacterGUID()`
  and holds the selected character.
- Proposed identity: that GUID, as a string. It is persistent (saved per
  character), unlike Unity instance IDs, and unlike slot names it
  distinguishes the operator's characters from each other.

**Requires Codex runtime confirmation** (§6.3): that `GetCharacterGUID()`
returns the loading character's GUID when called during `LoadSaveData` /
`GameStartCheckSkillPoint`, and that the test character's GUID differs
from the main character's. **No build until this is verified** — a wrong
identity would confer points to the wrong character.

### 2.4 Entitlement record (new, explicit, never derived from `applied`)

Stored in the existing per-slot grant state file (one atomic write with
the grant ledger), as a per-character map:

```json
"entitlements": {
  "<characterGuid>": {
    "epoch": 8591074088996589973,
    "points": 2,
    "indices": [2, 3]
  }
}
```

**Population rules (hard):**
- Appended **only** by the v0.3.7+ applicator's Confirmed path (balance
  verified before/after). Never from `applied.Count`, never from acks.
- `indices 0/1` are **excluded**: acked by older code without visible
  delivery. Their recovery is a separate, explicitly reviewed procedure —
  not automatic.
- Indices 2/3 (visibly delivered 0→1→2, then wiped) are **not
  auto-migrated** either. Same reason: "applied" is not proof of lasting
  delivery. the operator reviews the 2/3 evidence and authorizes a one-time
  entitlement entry, or they are re-earned. No silent backfill.

**Write ordering (crash safety):**
- The applicator stages confirmed entries in memory; `SaveGrantState`
  (the pipeline's durability point) merges them into the file atomically
  with the grant ledger, then clears the stage only on successful write.
- Crash between Confirmed and persist: grant not acked → server replays
  → re-applied → re-staged. Self-healing, no duplication.
- Crash after our persist but before the game's save: entitlement is
  ahead of the save. Next load, the validator sees
  `actual < allowed` and resets unspent to the entitled total (game's own
  reset logic; may wipe assigned skills — same as the game's own crash
  recovery). Documented limitation, narrow window.

### 2.5 Why not the alternatives

- **Blanket-skip validation:** forbidden by requirement; would also hide
  real corruption.
- **Regrant points after loading:** forbidden ("blindly regrant"); fights
  the validator every load instead of informing it.
- **Derive from `applied.Count`:** 0/1 prove acked ≠ delivered.
- **Route grants through fake mission completions** (so
  `GetTotalSkillPointTaken` includes them): corrupts game progression
  state; fragile; dishonest.

---

## 3. Test plan (headless where possible; in-game where required)

The transpiler cannot run headless (HarmonyX), so tests target the
allowance logic and the store; the validator seam is verified in-game
via the Japanese reset line's absence/presence.

1. **Unspent points:** entitlement=2 for (charA, epochE); simulated
   validator inputs assigned=0, unspent=2 → allowance=2 → no reset.
2. **Spent points:** assigned=1, unspent=1, entitlement=2 → total 2 ==
   allowed 2 → no reset; skills preserved (no `ResetAllSkills` call).
3. **Repeated reloads:** entitlement unchanged across simulated
   validations; allowance stable; no growth, no reset.
4. **Another character:** entitlement for charA; `GetApEntitlement` with
   charB → 0 (vanilla check applies; no leakage).
5. **Inconsistent save/ledger:**
   - save implies 5 AP points, entitlement=2 → allowance 2 < actual 5 →
     validator resets (fail closed; save not trusted).
   - grant ledger `applied=[0,1,2,3]`, entitlement empty → allowance 0
     (`NO_RECORD`; never derived from `applied`).
   - entitlement epoch ≠ pipeline epoch (new seed) → `EPOCH_MISMATCH`:
     allowance 0, warning logged naming both epochs.
   - store file corrupt (primary + backup) → `STORE_UNREADABLE`:
     allowance 0, FATAL logged, `recoveryNeeded` flag set, new grants
     blocked until flag cleared.
   - store file missing but ledger has applied indices → treated as
     `STORE_UNREADABLE` (not first-run).
   - store file missing and ledger empty → first-run, `NO_RECORD`.
6. **In-game (Codex):** with entitlement=2 recorded, reload → expect NO
   Japanese reset line, UI shows 2, skills intact. Then spend 1 point,
   reload → still no reset, assigned skill preserved.

---

## 4. What this preserves

- Readiness gate, corrected Python 0.6.8 client, current ledgers, save
  files: untouched. No migration, recovery, reset, or deployment.
- Ordinary validation: any `actual != allowed + entitlement` still resets.
- Main character: never in tests; per-character binding prevents leakage.

---

## 5. Known limitations

- New AP seed (new epoch) with the same character: old entitlement does
  not carry over (fail closed); the validator will wipe old AP points on
  next load. Seed changes need a reviewed procedure.
- Mod removed/disabled: validator reverts to vanilla; AP points wiped on
  next load (correct — without the mod they are unverifiable).
- Entitlement file is local-trust (like the save itself); forging it is
  no easier than editing the save directly. Out of scope for the PoC.
- Crash window (§2.4): process death between our persist and the game's
  save can cause a validator reset on next load (restores entitled
  unspent total via the game's own logic).

---

## 6. Precise evidence requested from Codex (no build until 6.3)

1. **Validator identity:** confirm `GameStartCheckSkillPoint` is
   `public void Oc.Skills.OcSkillManager.GameStartCheckSkillPoint()`
   (instance, no parameters), and list every call site — is
   `LoadSaveData` IL_0307 the only one?
2. **`GetTotalSkillPointTaken` callers:** full caller list, to decide
   transpiler (primary) vs postfix (fallback).
3. **Character identity (blocking):** at runtime during `LoadSaveData`,
   what does `OcPlCharacterManager.Inst.GetCharacterGUID()` return?
   Is it the loading character's stable GUID? Confirm the test
   character's GUID differs from the main character's. If this API is
   unsuitable, report the correct one — do not guess.
4. **Branch confirmation:** is the line
   `スキルポイント数不正：取得スキル、スキルポイントをリセットします`
   present in Player.log from the reload session?
5. **`INCREASE_SKILLPOINT_BY_LEVEL_UP`** numeric value (for test vectors).

