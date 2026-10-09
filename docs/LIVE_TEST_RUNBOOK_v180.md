> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# v1.8.0 Controlled Live Test Runbook

## Prerequisites
- Game closed. Active plugin: CraftopiaArchipelago v1.8.0 ONLY.
  DiagnosticCollector must be REMOVED from BepInEx/plugins during reward test.
- Fresh Archipelago seed generated with `mission_catalog_version: 20261007_r1`
  (instance mode, 125 locations). Note the seed name and slot name.
- Disposable test character (NOT the operator's main). Record its name.
- Bridge configured for the seed. Do NOT use historical ledgers.

## Phase 1: Enrollment
1. Launch game, load disposable character.
2. Connect bridge. Verify handshake: `[mod] handshake validated`.
3. In-game, open the AP status overlay (F8). Note displayed character GUID.
4. **Explicit enrollment**: The bridge enrolls on handshake if GUID present.
   Verify: bridge log shows enrollment; ledger has the GUID.
5. **Attestation** (one-time, explicit):
   - This is a manual step. The mod does NOT auto-attest.
   - Codex: use the attestation file to record approval, OR
   - Future: in-game UI will prompt. For v1.8.0, attestation is via
     direct file creation (documented procedure).
   - Attestation binds (GUID, epoch, slot). Without it, validation stays
     UncertainHistory and rewards stay blocked.

Expected: After attestation + load completion, validation shows
NoRecordConfirmed (fresh, zero points) instead of UncertainHistory.

Stop if: Attestation file corrupt, GUID mismatch, or epoch changes.

## Phase 2: One Point
1. Select a naturally unfinished mission on the disposable character.
   Document: mission ID, title, and how we know its placed reward is
   a Skill Point (check the seed spoiler log BEFORE completing).
2. Complete the mission in-game.
3. Expected log sequence:
   - `[AP] check: mission <id> -> location <ap_id>`
   - Bridge sends to AP server; server returns Skill Point.
   - Mod receives grant; GrantTransaction creates intent.
   - Game mutation: +1 skill point (verify in UI).
   - Entitlement committed; ack sent to bridge.
4. Verify: skill point balance increased by exactly 1.

Stop if: Check not recognized, grant not received, or mutation doesn't occur.

## Phase 3: Save/Reload
1. Save game normally. Exit to menu (do NOT kill process).
2. Reload the same character.
3. Expected: Entitlement persists; validation shows Ok with 1 point.
   No duplicate grant. No re-enrollment prompt.

Stop if: Points reset, duplicate grant, or UncertainHistory returns.

## Phase 4: Bridge Reconnect
1. While game running, restart the bridge.
2. Expected: Reconnect succeeds; session re-established; no duplicate
   grants. Pending intents (if any) replay once.

Stop if: Duplicate grants or session mismatch.

## Phase 5: Duplicate Suppression
1. Complete another mission (different ID).
2. Force bridge restart mid-grant (if safe) OR verify via logs that
   ack was received and intent cleared.
3. Expected: Exactly one mutation per grant. Restart does not re-apply.

## Stop Conditions (abort test)
- Any save file modification outside normal game save.
- Historical ledger/entitlement files touched.
- Indices 0-3 referenced or replayed.
- Validation bypassed or holds cleared without attestation.
- Duplicate skill point mutations.

## Expected Log Events
- `[AP] attested enrollment: guid=..., epoch=..., slot=...`
- `[AP] validation: NoRecordConfirmed (verified fresh)`
- `[AP] check: mission <id> -> location <ap_id>`
- `[AP] grant received: index=..., items=[Skill Point]`
- `[AP] transaction: intent created`
- `[AP] transaction: mutation applied (+1 point)`
- `[AP] entitlement committed: guid=..., epoch=..., points=1`
- `[AP] ack sent: receipt_id=...`

## What This Test Proves
- Enrollment attestation enables validation (not bypassed).
- One mission -> one Skill Point -> one game mutation -> ack.
- Persistence across save/load and reconnect.
- No duplicates.

## What This Test Does NOT Prove
- Full catalog beatability (125 missions untested).
- Multiplayer (single character only).
- Production/skill enforcement (not enabled).
- Other check categories (ages, orbs, etc.).

