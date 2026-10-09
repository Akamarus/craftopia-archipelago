> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# End-to-End Test Plan — Craftopia Archipelago v1.0

## Scope
Controlled testing on the disposable test character only. No main characters.
No Steam Auto-Cloud. Backups retained.

## Prerequisites
- [ ] Mod v1.0.0 DLL built and hash-verified
- [ ] Bridge `craftopia_bridge.py` ready
- [ ] Archipelago 0.6.8 server available
- [ ] World `worlds/craftopia/` with populated `missions.json`
- [ ] Test character backup (save DB copied)

## Phase 1: Mod Load & Hook Verification
1. Install mod DLL to `BepInEx/plugins/` (remove diagnostic DLL).
2. Launch game to main menu.
3. **Verify**: `LogOutput.log` contains `[AP] all hooks registered and verified`.
4. **Verify**: No `[AP] FATAL` lines.
5. **Verify**: Config file created at `BepInEx/config/com.craftopia.archipelago.cfg`.

## Phase 2: Bridge Connection
1. Generate a single-player game with the Craftopia world.
2. Start the AP server.
3. Start bridge: `python craftopia_bridge.py --server localhost:38281 --slot <name>`.
4. **Verify**: Bridge logs `Connected to AP server` and `Mod connected`.
5. Load test character in-game.
6. **Verify**: Bridge receives `slot_data` with `game_id_to_location` mapping.
7. **Verify**: Mod log shows `[AP] slot data: epoch=<n>`.

## Phase 3: Check Reporting (Mission -> AP)
1. In-game, open Missions menu.
2. Claim a mission reward.
3. **Verify**: Mod log `[AP] check: mission <id> '<title>'`.
4. **Verify**: Bridge log shows location check sent.
5. **Verify**: AP server shows the location as checked (via server console or client).
6. Claim the same mission again (if possible).
7. **Verify**: Duplicate is ignored (already checked).

## Phase 4: Reward Application (AP -> Game)
1. From the AP server console, grant a Skill Point item to the slot
   (or use another player's check that sends a skill point).
2. **Verify**: Bridge log shows grant received for indices [...].
3. **Verify**: Mod log `[AP] granted skill point(s) for item indices [...]`.
4. **Verify**: In-game skill point UI increases by `skill_points_per_check`.
5. **Verify**: Bridge sends ack; mod log confirms.

## Phase 5: Reconnect & Dedup
1. Kill the bridge (Ctrl+C). Keep game running.
2. Restart the bridge with the same `--session`.
3. **Verify**: No duplicate grants (mod dedups by index).
4. **Verify**: Ledger file unchanged for already-acked indices.
5. Disconnect the mod (close game). Reconnect.
6. **Verify**: Grants resume; no duplicates.

## Phase 6: Persistence (Validator Gating)
1. With AP skill points granted (e.g., 2 points), save and exit.
2. Reload the character.
3. **Verify**: Mod log `[AP] validation: outcome=Pass` (or `Defer` if uncertain).
4. **Verify**: Skill points are preserved (not reset to 0).
5. **Verify**: If `Defer`, grants are blocked until recovery.

## Phase 7: Victory
1. Complete the goal mission (or all missions per option).
2. **Verify**: Victory item received.
3. **Verify**: AP server marks the slot as complete.

## Failure Injection (must not corrupt)
- [ ] Kill bridge mid-grant: mod must not ack unapplied grants.
- [ ] Kill game mid-grant: on reload, unacked grants are retried, not duplicated.
- [ ] Delete entitlement store: validator must Defer (not reset).
- [ ] Corrupt grant state: pipeline must fail closed.

## Sign-off
Each phase requires explicit pass/fail. Any failure stops the test;
do not proceed until resolved. All logs retained.

