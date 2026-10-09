> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Persistence Design v3.5 — Harmony Contract Correction (no implementation)

Corrects v3.4 §1's Harmony contract per review. Verified against the
shipped HarmonyX 2.9.0.0 (`repo/mod/refs/0Harmony.dll`): `__state`
(prefix `out` → postfix) and `void Finalizer(Exception __exception)`
are both supported. No build, deployment, save edits, or recovery.

---

## Corrected contract

Three separate methods. Each has one job.

### Prefix — invalidate, evaluate, decide; catch only our failures

```csharp
// Returns false to SKIP original, true to RUN it.
// __state carries per-invocation data to the postfix.
static bool Prefix_GameStartCheckSkillPoint(out PrefixState __state)
{
    __state = new PrefixState();
    // 1. Invalidate eligibility for this load event (unconditional).
    lastValidationEventId++;
    validationRecord = new ValidationRecord {
        eventId = lastValidationEventId,
        playerRef = new WeakReference<OcPlMaster>(OcPlMng.LocalPl),
        guid = null, epoch = null, succeeded = false
    };
    __state.EventId = lastValidationEventId;

    try
    {
        // 2. Evaluate the gate. OUR code only; the original has not run.
        var (status, detail) = Resolve(guid, epoch);
        __state.Deferred = ShouldDefer(status, ...);
        if (__state.Deferred)
        {
            Log.Fatal(...);                       // + reconciliation hold if OK-discrepancy
            return false;                         // skip original
        }
        var (actual, allowed) = Evaluate();
        if (status == NO_RECORD_CONFIRMED && actual != allowed)
        {
            __state.RunOriginal = true;
            return true;                          // run original (vanilla gate)
        }
        __state.RunOriginal = false;
        return false;                             // PASS: skip (original would no-op)
    }
    catch (Exception e)
    {
        // Catches ONLY our evaluation failures. The original method has
        // not executed at this point; there is nothing of its to catch.
        Log.Fatal("[AP] validation gate evaluation failed: " + e);
        __state.EvaluationFailed = true;
        return false;                             // defer; eligibility already invalidated
    }
}
```

### Postfix — successful-path readback only

```csharp
// Runs after the prefix (whether the original ran or was skipped).
// Uses __state, NOT __exception. Never observes or throws exceptions.
static void Postfix_GameStartCheckSkillPoint(PrefixState __state)
{
    try
    {
        if (__state.Deferred || __state.EvaluationFailed)
            return;                               // nothing to confirm
        // Successful path: confirm via readback.
        var (actual, allowed) = Evaluate();
        if (actual == allowed
            && validationRecord.eventId == __state.EventId)
            validationRecord.succeeded = true;
        else
            Log.Fatal("[AP] post-validation readback mismatch");
        // succeeded stays false on mismatch; grants remain blocked.
    }
    catch (Exception e)
    {
        Log.Fatal("[AP] postfix readback failed: " + e);
        // Diagnostic code must not itself throw; succeeded stays false.
    }
}
```

The postfix never takes `__exception` and never rethrows. Exception
observation belongs to the finalizer below.

### Finalizer — exception diagnostics only, no suppression

```csharp
// Separate method. Void return = cannot suppress or replace.
// Runs even if the original or postfix threw.
static void Finalizer(Exception __exception)
{
    try
    {
        if (__exception != null)
        {
            Log.Fatal("[AP] exception in validation path: " + __exception);
            if (validationRecord != null)
                validationRecord.succeeded = false;   // invalidate eligibility
            // Deliberately no suppression: the exception propagates
            // per game behavior. See v3.4 §1 (no blanket suppression;
            // a mid-method throw may follow partial skill mutation).
        }
    }
    catch (Exception e)
    {
        try { Log.Fatal("[AP] finalizer failed: " + e); } catch { }
    }
}
```

---

## What changed from v3.4 §1

- Postfix no longer takes `__exception`; it does readback only,
  guarded by `__state` (which records whether this invocation deferred,
  failed evaluation, or allowed the original).
- Exception diagnostics moved to a dedicated `void Finalizer(Exception
  __exception)`: logs a non-null exception and invalidates eligibility
  without suppressing or replacing it. It is never invoked manually
  from the postfix.
- Prefix catch scope stated precisely: our evaluation code only. The
  invalidation step precedes it and is not undone by it.
- All diagnostic paths (`catch` blocks, finalizer) are themselves
  exception-safe.

The lifecycle proposals (load-start boundary, soft validation trigger,
ownership, enrollment) remain candidates pending installed-game
evidence. Regression cases remain **[PROPOSED]**.

