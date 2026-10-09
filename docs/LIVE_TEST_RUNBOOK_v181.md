> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# v1.8.1 Controlled Live Test Runbook (Corrected)

## Critical Corrections from v1.8.0

- **Attestation is ONE-TIME**: Once consumed (first entitlement committed),
  it can never authorize freshness again. If the entitlement store is lost
  after a grant, the result is HOLD (UncertainHistory), not fresh.
- **Enrollment requires explicit action**: There is no automatic transition.
  The runbook below specifies the exact manual steps.
- **Indices**: Historical indices 0-3 (the operator's v1.2.1 test) must remain untouched.
  A NEW seed legitimately starts at index 0. The protection is per (seed, slot),
  not a blanket ban on index 0.
- **Unresolved intents**: After restart, unresolved intents require
  hold/reconciliation, NOT automatic replay. Do NOT deliberately restart
  mid-grant for the first live test.

## Prerequisites
- Game closed. Active plugin: CraftopiaArchipelago v1.8.1 ONLY.
- Fresh Archipelago seed (instance mode, 125 locations). Record seed/slot.
- Disposable test character. Record name and GUID (from F8 overlay).
- Bridge configured. Use NEW ledger directory (not historical).

## Phase 1: Explicit Enrollment

1. Launch game, load disposable character. Note GUID from F8 overlay.
2. Connect bridge. Verify: `[mod] handshake validated ... character <guid>`
3. Bridge auto-enrolls the GUID (ledger). Verify in bridge log.
4. **Manual attestation** (explicit user approval):
   - Create attestation file at:
     `BepInEx/config/craftopia-archipelago-attestations-<slot>.json`
   - Format:
     ```json
     {
       "schema": 1,
       "attestations": {
         "<guid>": {
           "epoch": <epoch>,
           "slot": "<slot>",
           "character_name": "<name>",
           "attested_at": "<ISO8601 UTC>",
           "note": "Fresh character for test seed <seed>"
         }
       }
     }
     ```
   - Get epoch from bridge log (`[AP] session applied: epoch=...`).
5. **Restart game** (attestation is loaded at startup; no hot-reload).
6. Reload character. Verify in mod log:
   - `[AP] loaded 1 enrollment attestation(s)`
   - `[AP] validation: NoRecordConfirmed (verified fresh)`
   - NOT UncertainHistory.

Stop if: Attestation not loaded, validation stays UncertainHistory, or
ValidationGate hold persists (this is expected; the hold is for the
pre-attestation state. The revalidation after restart should clear it
for the attested GUID/epoch).

## Phase 2: One Point (Controlled)

1. From the seed spoiler, select a mission that:
   - Is naturally unfinished on the disposable character, AND
   - Has a Skill Point placed (verify in spoiler BEFORE completing).
   Document: mission ID, title, AP location ID, placed item.
2. Complete the mission in-game.
3. Expected:
   - Mod: `[AP] check: mission <id> -> location <ap_id>`
   - Bridge: sends to server, receives Skill Point (index N).
   - Mod: grant received, transaction intent created.
   - Game: +1 skill point (verify in character UI).
   - Mod: entitlement committed (attestation now CONSUMED).
   - Bridge: ack received, intent cleared.
4. Verify: exactly +1 point. No more, no less.

Stop if: Check not recognized, grant not received, mutation fails, or
attestation not marked consumed (check file).

## Phase 3: Save/Reload (Persistence)

1. Save game normally. Exit to main menu.
2. Reload same character.
3. Expected:
   - Validation: Ok, 1 point (not UncertainHistory, not NoRecordConfirmed).
   - Attestation shows `consumed_at` timestamp.
   - No duplicate grant. No re-enrollment.

Stop if: Points lost, duplicate, or attestation still unconsumed.

## Phase 4: Attestation Consumption Proof

1. (Do NOT do this on the live test character.)
2. Conceptual: If the entitlement file were deleted now, with attestation
   retained, the result MUST be UncertainHistory (HOLD), not fresh.
3. This is verified in headless tests, not live.

## Expected Log Strings (exact)

- `[AP] loaded 1 enrollment attestation(s)`
- `[AP] attested enrollment: guid=..., epoch=..., slot=...`
- `[AP] validation: NoRecordConfirmed (verified fresh)`
- `[AP] check: mission <id> -> location <ap_id>`
- `[AP] grant received: index=..., items=[Skill Point]`
- `[AP] attestation consumed for guid=... (one-time use)`
- `[AP] entitlement committed: guid=..., epoch=..., points=1`

## Stop Conditions

- Save file modified outside normal game save.
- Historical ledgers touched.
- Attestation bypassed or holds generically deleted.
- Duplicate mutations.
- Validation shows RunVanilla (should never happen in test).

## What This Proves / Does Not Prove

Proves: Explicit enrollment -> attested freshness -> one point -> persistence.
Does NOT prove: Full catalog, multiplayer, enforcement, other categories.

