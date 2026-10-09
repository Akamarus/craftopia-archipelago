> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Multiplayer Protocol Design

## Modes

### Separate Slots
Each player has their own AP slot. The bridge connects once per slot.
- Each slot has independent unlocks, items, and victory state.
- Shared accomplishments (boss kills, age advancements) are reported by each
  participating slot's bridge as their own location check.
- The world binds slots to a compatible seed via `slot_data["world_id"]`.

### Shared Slot
One AP slot represents the team. One bridge connection is authoritative.
- Participants share unlocks, check history, and goal state.
- Each character receives skill-point entitlement once (tracked per character GUID).
- Materials/money delivered once to the team (first claimant).
- Late joiners inherit unlocks and receive point entitlement; no fresh consumables.

## Roster Binding

`slot_data` includes:
- `world_id`: Stable world identifier (from game save metadata).
- `roster`: List of registered character GUIDs (for shared slot).
- `multiplayer_mode`: 0 = separate, 1 = shared.

The bridge validates:
1. Slot name matches (existing).
2. World ID matches (new): refuse if the game world doesn't match the generated seed.
3. Character GUID is in roster (shared slot): refuse unknown characters.

## Shared Events (Separate Slots)

When a shared accomplishment occurs (e.g., boss kill):
1. Each participating player's mod reports the check via their bridge.
2. Each bridge maps it to their slot's corresponding location.
3. Server delivers each slot's placed item to that slot's player.

No cross-slot coordination needed; AP handles the fanout.

## Shared Slot Delivery

One bridge owns the AP connection. Multiple game clients (mods) connect to it.

### Check Reporting
- Any participant's mod can report a check.
- Bridge deduplicates by check identity (location ID).
- First report wins; subsequent duplicates are acked but not re-sent.

### Item Delivery
- Bridge receives items from AP server.
- **Skill points**: Each bound character gets entitlement once. Bridge tracks
  `delivered_points[character_guid]`. Late joiners receive catch-up.
- **Unlocks** (equipment tiers, production, skill trees): Applied team-wide.
  Bridge broadcasts to all connected mods.
- **Consumables** (materials, money): Delivered once to the team. First
  claimant's mod receives them; others see "already claimed".

### Offline Catch-up
- Bridge persists team state (unlocks, check history, per-character entitlements).
- Reconnecting character receives: current unlocks + any unclaimed point entitlement.
- No replay of consumables.

## Goal Synchronization

- Victory conditions are evaluated by the bridge from team state.
- Boss/age/objective events are recorded durably.
- When all enabled conditions are met, bridge sends StatusUpdate(CLIENT_GOAL).
- In separate-slots mode, each slot completes independently.
- In shared-slot mode, one StatusUpdate completes the team slot.

## Message Protocol (Mod <-> Bridge)

New commands:
- Mod → Bridge: `{"cmd": "hello", "world_id": "...", "character_guid": 123}`
  - Bridge validates world/character against roster.
- Bridge → Mod: `{"cmd": "unlock", "type": "equipment|production|skill", "id": "...", "tier": N}`
  - Broadcast to all connected mods (shared slot) or single mod (separate).
- Bridge → Mod: `{"cmd": "points", "amount": N, "reason": "catchup|grant"}`
- Mod → Bridge: `{"cmd": "claim", "item_id": "..."}`
  - For consumables; bridge responds with grant or "already claimed".

## Failure Modes

- **Bridge crash**: Team state is persisted; restart resumes.
- **Mod disconnect**: Character's pending deliveries are held; catch-up on reconnect.
- **World mismatch**: Bridge refuses connection with clear error.
- **Character not in roster**: Bridge refuses; requires explicit roster update
  (separate procedure, not automatic).

