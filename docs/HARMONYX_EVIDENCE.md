> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# HarmonyX Compatibility Evidence — shipped binary verification

**DLL:** `repo/mod/refs/0Harmony.dll`
**SHA-256:** `1a21cc03424fc82c3dd1346905d16494536b9595ae4162228d99fb7c285c1031`
**Assembly version:** 2.9.0.0 (identifies as HarmonyX)

## Inspection output (this binary, via IL metadata scan)

Scanned all method IL bodies for `ldstr` references:

- `ldstr "__state"` references: **1** — the binary recognizes the
  `__state` patch parameter name.
- `ldstr "__exception"` references: **1** — the binary recognizes the
  `__exception` patch parameter name.
- `ldstr "*finalizer*"` references: **3** — finalizer handling exists.
- String heap contains `<>1__state` (compiler-generated use of the
  identifier within Harmony's own patch machinery).

## Functional test limitation (honest)

A live patch-application test (prefix `out __state` → postfix
`__state` → `void Finalizer(Exception __exception)`) was attempted
against this binary on Linux .NET 8. It **cannot execute**: Harmony's
`AccessTools` static initializer throws NRE in `MethodInvoker.GetHandler`
— the same known HarmonyX-on-.NET-8 incompatibility documented in this
project's history ("HarmonyX PatchAll cannot execute in this Linux .NET 8
environment; Unity Mono remains the runtime validation target").

Therefore: signature *recognition* is verified in this binary via the
metadata above; signature *behavior* (patch application with these exact
signatures) must be verified at implementation time on the Unity Mono
runtime, not inferred from general Harmony 2.x documentation.

## Carried-forward implementation requirements

From the v3.5 review, retained as binding for implementation:

1. **Invalidate eligibility before any logging.** The prefix bumps the
   load/validation identity and sets `succeeded=false` before emitting
   any diagnostic output.
2. **Diagnostics non-throwing.** Every `catch` block, the postfix, and
   the finalizer must be internally exception-safe; diagnostic code that
   throws is a defect.
3. **Separate identities retained.** Load-start identity (load-start
   boundary sequence) and validation-invocation identity (per-evaluation
   event ID + player object reference) are distinct and both checked.
4. **Resolve/capture/recheck before success.** `succeeded=true`
   requires: fresh `Resolve` (guid + epoch), capture of the specific
   player object and load event, and a post-execution readback
   (`actual == allowed`) matching the same event ID. No step may be
   skipped or assumed from a prior load.

