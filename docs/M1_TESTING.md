> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# M1 PoC — Test Procedure

Goal: prove the full round trip — a mission reward claim in Craftopia shows up
as a checked location on an Archipelago server, and an item released on the
server lands in-game as a skill point.

## Prerequisites

1. **Archipelago installed** (https://archipelago.gg) — install with at least
   Generator, Server, and Text Client.
2. **BepInEx working in Craftopia.** When you launch the game you should see the
   BepInEx console window. If not, subscribe to the Steam Workshop item
   "Craftopia Mod Loader (BepInEx)" and enable it in the in-game ModHub, then
   restart the game. The mod goes in `<Craftopia>/BepInEx/plugins/`.
3. **Python 3** with `pip install websockets`.
4. PoC files (from the `client/` and packaged mod build):
   - `CraftopiaArchipelago.dll`
   - `craftopia_client.py`
   - `AP_PoC.yaml`

## A. Generate and host the seed

1. Copy `AP_PoC.yaml` into Archipelago's `Players` folder.
2. Archipelago Launcher → **Generate**. A zip appears in `output/`.
3. Launcher → **Host** → select that zip. Keep the server window open and note
   the port (default `38281`).

## B. Install the mod

1. Copy `CraftopiaArchipelago.dll` into `<Craftopia>/BepInEx/plugins/`.
2. Launch Craftopia once, then close it. This generates
   `BepInEx/config/com.craftopia.archipelago.poc.cfg` — open it and confirm
   `SlotName = Player1` (must match the YAML name).
3. Check `BepInEx/LogOutput.log` for:
   `[AP] CraftopiaArchipelago PoC 0.1.0 loaded`

## C. Run the bridge client

In a terminal:

```
python craftopia_client.py --server localhost:38281 --slot Player1
```

Expected output:

```
[mod] local bridge listening on 127.0.0.1:11341
[AP] connecting to server ws://localhost:38281 ...
[AP] connected as 'Player1' (team 0, slot 1)
```

## D. Test 1 — check flows game → server

1. In Craftopia, open the Missions menu and **claim any mission reward**.
2. Mod log (`LogOutput.log`) should show:
   `[AP] check: mission <id> '<title>'`
3. The client should print:
   `[AP] check: mission <id> '<title>' -> AP location 1`
4. The server window should log the LocationChecks packet. (The PoC maps every
   mission to APQuest location 1 — "Top Left Room Chest".)

**Pass criteria:** the check appears on the server.

## E. Test 2 — item flows server → game

1. Open the **Archipelago Text Client**, connect to `localhost:38281`, set your
   name to `Player1`.
2. Type: `!getitem Sword`
3. The bridge client should print `received 1 new item(s) from the multiworld`.
4. The mod log should show:
   `[AP] received grant: 1 skill point(s)`
   `[AP] granted 1 skill point(s)`
5. In Craftopia, open the skill menu and confirm you gained a skill point.

**Pass criteria:** the skill point lands in-game. (PoC maps every item to one
skill point; real item mapping is M4.)

## Troubleshooting

- **Mod log has nothing:** BepInEx didn't load the mod — check `plugins/` path
  and that the BepInEx console appears at game start.
- **"[AP] bridge connection failed, retrying":** the Python client isn't running
  or the port mismatches (mod config `LocalPort` vs client `--port`).
- **Client can't reach the server:** check the port in step A and that the
  server window is still open.
- **`!getitem` says cheats are disabled:** self-hosted servers enable item cheat
  by default; if yours doesn't, enable `item_cheat` in the server settings.
- **Claiming a reward sends nothing:** the mission must actually grant its reward
  (the patch fires on `TryGetReward` only when `IsRewardTaken` is true).

