> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# M1 — Client/Mod Architecture Notes (prep)

Researched 2026-10-05 while waiting on the assembly dump. Pattern taken from
Archipelago's own `worlds/ahit/Client.py` (A Hat in Time), which solves the
exact same problem: a BepInEx-style game mod talking to a Python AP client.

## Chosen architecture: client hosts, mod connects

- The **Python client** runs a websocket *server* on localhost (AHIT uses port
  11311; we'll pick our own, e.g. 113XX, and make it configurable).
- The **BepInEx mod** opens a websocket *client* connection to it when the game
  reaches a suitable state (main menu / save loaded).
- Rationale: the Python side is the hub — it already maintains the AP server
  connection, the received-item index, and reconnect logic. The mod stays
  thin: detect checks, grant items, nothing more.

## Message protocol (JSON over the local socket)

Mod → client:
- `{"cmd": "Connect", "game": "Craftopia", "name": "<slot name>", "seed_name": "<seed>"}` —
  client validates game name, seed name, and player name match the AP session
  (AHIT aborts the proxy connection on mismatch; we do the same).
- `{"cmd": "LocationChecks", "locations": [<ap location ids>]}` — forwarded to
  the AP server.
- Later: `{"cmd": "StatusUpdate", ...}` for goal completion (client sends the
  real `StatusUpdate` packet to the server; mod just reports the goal event).

Client → mod:
- `Connected` (slot_data: goal + enabled check sets + any options the mod needs)
- `ReceivedItems` (`{"index": N, "items": [...]}`; `index == 0` means "clear and
  resync everything" — this is the offline/reconnect recovery story)
- `PrintJSON` (chat / item notifications to show in-game)
- Later: DeathLink bounce messages (optional toggle).

The client also forwards relevant server packets (`RoomInfo`, `RoomUpdate`)
so the mod can display connection state.

## Client responsibilities (from Archipelago's "Adding Games" hard requirements)

- Secure + insecure websocket to the AP server, auto-reconnect, user-changeable
  port (`items_handling = 0b111`: the client handles all item types).
- Keep the ordered received-item index; on mod (re)connect, replay anything the
  mod hasn't confirmed.
- Checks earned while the mod was disconnected must be backfilled on connect —
  the mod must be able to enumerate "already done" checks from game state/save
  (this is why M0 includes save-format research).
- Send the goal-completion `StatusUpdate`.

## Mod responsibilities (C#, BepInEx 5, Harmony)

- Harmony postfix patches on the hook points from `HOOK_INVENTORY.md`
  (mission claim proven patchable already; enemy death / dungeon clear /
  inventory add TBD from the dump).
- Maintain a persistent local queue of unconfirmed checks; confirm on client ACK.
- Grant items idempotently: inventory add, `SkillCtrl.AddSkillPoint(n)`
  (proven one-liner), slates/gold, gate unlocks, traps.
- Websocket client: `System.Net.WebSockets.ClientWebSocket` (.NET 4.5+) is the
  first choice — no extra dependency. Fallback: WebSocketSharp if the game's
  Mono is too old (check `MonoBleedingEdge` version in M1).

## M1 proof-of-concept scope (minimal, per official guidance)

1. Mod detects **one** check: a mission reward claim
   (`[HarmonyPatch(typeof(Mission), "TryGetReward")]` postfix — already proven
   in a public mod).
2. Mod grants **one** item: a skill point via `OcPlMaster.Inst.SkillCtrl`.
3. Python client bridges both to a real AP server (temporarily posing as another
   game is acceptable for the first test).
4. **Exit:** in-game action → check visible on server; server-released item →
   skill point lands in-game. Validated on the operator's machine.

Full check/item coverage, persistence, and DeathLink are M4, not M1.

