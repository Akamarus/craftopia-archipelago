> Historical v1.0 guide: mission counts and enrollment instructions below are superseded. Use the repository [README](../../../README.md) and [development guide](../../../docs/DEVELOPMENT.md).

# Craftopia Setup Guide

## Requirements

- Craftopia (Steam)
- BepInEx 5.4.x (64-bit)
- Python 3.10+ with `websockets` (`pip install websockets`)
- Archipelago 0.6.8 server (for multiworld) or generate a single-player game

## Installation

1. Install BepInEx 5.4.x into your Craftopia folder (see BepInEx docs).
2. Copy `CraftopiaArchipelago.dll` to `Craftopia/BepInEx/plugins/`.
3. Copy `craftopia_bridge.py` and `Craftopia.yaml` to a folder of your choice.
4. Edit `Craftopia.yaml`: set your slot name and options.
5. Generate the multiworld (or single-player) via Archipelago, then start the server.

## Running

1. Start Craftopia. The mod loads automatically; check `BepInEx/LogOutput.log`
   for `[AP] all hooks registered and verified`.
2. Start the bridge: `python craftopia_bridge.py --server localhost:38281 --slot YourSlot`
3. In Craftopia, load your character. The bridge connects when the mod is ready.
4. Claim mission rewards in-game to send checks. Received skill points are
   applied automatically.

## Configuration (mod)

In `BepInEx/config/com.craftopia.archipelago.cfg`:
- `SlotName`: must match your YAML slot name.
- `LocalPort`: bridge websocket port (default 11341).

## Troubleshooting

- **Hooks not verified**: Check `LogOutput.log` for `[AP] FATAL`. The game may
  have updated; report the game version.
- **Bridge can't connect to mod**: Ensure the game is running and the mod is
  loaded. Check the port matches.
- **Checks not sending**: Claim a mission reward in the Missions menu. The
  mod logs `[AP] check: mission <id>`.
- **Points reset on load**: This is the vanilla validator. The v1.0 mod gates
  it via the entitlement store. If points vanish, check the log for
  `[AP] validation: outcome=` — `Defer` means the entitlement is uncertain;
  see the persistence docs.

## Scope (v1.0)

- **Checks**: Mission reward claims only (142 mission types; see data/missions.json
  for the verified list).
- **Rewards**: Skill Points only.
- **Excluded**: Dungeon bosses, towers, field bosses, inventory items, XP/gold.
  These have hook candidates but are not in v1.0.

