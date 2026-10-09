> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Durable Enrollment Design

## Problem

The v1.0.8 baseline intentionally does not treat an enrollment hint plus an
empty ledger as proof of a fresh character. Missing history returns
`UncertainHistory`, blocking delivery. This is correct but requires an explicit
enrollment procedure for new characters.

## Requirements

1. **Durable**: Enrollment survives process death and restart.
2. **Explicit**: The player (or operator) must confirm enrollment; never automatic.
3. **Non-reusable**: A used character cannot be re-enrolled as fresh.
4. **Auditable**: Enrollment records who/when/what for review.

## Proposed Mechanism

### Enrollment File

Per-character file: `craftopia-archipelago-enrollment-<guid>.json`

```json
{
  "character_guid": 12345,
  "enrolled_at": "2026-10-06T20:00:00Z",
  "enrolled_by": "player",  // or "operator"
  "slot_name": "Player1",
  "world_id": "abc123",
  "epoch": 987654321,
  "attestation": "manual"  // How enrollment was verified
}
```

### Enrollment Flow

1. **Detection**: Mod sees character GUID with no enrollment file and no
   entitlement history. Sets `UncertainHistory`, blocks delivery.

2. **Player action**: In-game UI (or bridge command) offers "Enroll as new
   character". Player must explicitly confirm.

3. **Verification**: Before writing the enrollment file, the mod verifies:
   - Character is currently loaded and playable.
   - No AP items have been received for this GUID (check entitlement store).
   - The player confirms they understand this is a one-time action.

4. **Atomic write**: Write to temp file, flush, then atomic rename.

5. **Post-enrollment**: Character is now `NoRecordConfirmed` (fresh).
   Normal validation and delivery proceed.

### Safety Properties

- **Partial write**: If the process dies during enrollment, the temp file
  is ignored on restart. The character remains `UncertainHistory`.
- **Re-enrollment**: If an enrollment file exists, the character cannot be
  re-enrolled. To reset, the file must be manually deleted (operator action).
- **Wrong character**: The GUID in the file must match the loaded character.
  Mismatch → block.

### Recovery Procedure

For a character stuck in `UncertainHistory`:

1. Operator inspects the entitlement store and grant history.
2. If the character genuinely has no AP history, operator may manually
   create the enrollment file (with `enrolled_by: "operator"`).
3. If the character has history but it's missing/corrupt, do NOT enroll.
   Follow the recovery hold procedure instead.

## Implementation Status

**Not yet implemented.** This is a design proposal for review.

The v1.0.8 baseline correctly blocks; this design provides the path forward
without compromising the safeguard.

## Runtime Evidence Needed

- How to present enrollment UI in-game (or via bridge console).
- Character GUID stability across sessions (already verified: `OcPl.GUID`).
- Whether multiple characters per save are common (affects UX).

