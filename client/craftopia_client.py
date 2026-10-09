#!/usr/bin/env python3
"""CraftopiaArchipelago PoC bridge client (v0.3.0 protocol).

Sits between the Craftopia BepInEx mod (local websocket) and an Archipelago
server. For the PoC it *pretends to be an APQuest slot*: mission reward claims
in Craftopia are reported as APQuest locations, and every received item is
granted as one in-game skill point.

Delivery semantics (see docs/DELIVERY.md for the full strategy):
- Item grants carry AP ReceivedItems indices. An index counts as DELIVERED only
  when the mod acks it back after applying the skill point in-game AND the ack
  has been durably recorded in the ledger. Failed applies / failed persists are
  retried, never falsely acked.
- Delivery state is persisted to a per-session ledger file, so a bridge restart
  never re-grants items the server replays. A new seed requires an explicit new
  --session (or --reset-ledger); there is no automatic reset heuristic.
- The ledger epoch is a random session identifier; the mod rotates its
  applied-grant set on a new epoch, and stale-epoch acks are ignored.
- Grants that can't be sent yet (mod offline) stay queued and are re-sent when
  the mod reconnects. The mod dedups by index, so re-sends are safe.
- Location checks made while the server is offline are queued and reconciled
  on reconnect (already-checked locations are skipped).
- Each distinct mission maps to its own APQuest location (pool: 1,2,3,4,5,6,10),
  so duplicate claims of the same mission hit an already-checked location.
- Slot identity is validated: a mod slot-name mismatch aborts the bridge.

Requires: pip install websockets
Usage:    python craftopia_client.py --server localhost:38281 --slot Player1
"""
import argparse
import asyncio
import json
import os
import random
import sys
import uuid

try:
    import websockets
except ImportError:
    sys.exit("Missing dependency: pip install websockets")

AP_VERSION = {"major": 0, "minor": 6, "build": 8, "class": "Version"}

# APQuest locations we may use, in assignment order. Verified against the
# Archipelago source: 1=Top Left Room Chest, 2=Top Middle Chest,
# 3=Bottom Left Chest, 4=Bottom Left Extra Chest, 5=Bottom Right Room Left Chest,
# 6=Bottom Right Room Right Chest, 10=Right Room Enemy Drop.
APQUEST_LOCATION_POOL = [1, 2, 3, 4, 5, 6, 10]

LEDGER_DIR = os.path.dirname(os.path.abspath(__file__))


def sanitize_session(s):
    cleaned = "".join(c for c in (s or "") if c.isalnum() or c in "-_")
    return cleaned or "default"


def ledger_path(session):
    return os.path.join(LEDGER_DIR, f"craftopia_poc_ledger_{sanitize_session(session)}.json")


def load_ledger(path):
    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
        if isinstance(data, dict):
            return data
    except (OSError, ValueError):
        pass
    return {}


def save_ledger(path, data):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    os.replace(tmp, path)


class Bridge:
    def __init__(self, args):
        self.args = args
        self.ap_ws = None
        self.mod_ws = None
        # A mod socket becomes eligible for grants only after its handshake
        # (connect with a matching non-blank slot name) has been validated.
        self.mod_validated = False
        self.ap_connected = False
        self.slot_data = {}
        self.checked_locations = set()
        # Indices sent to the mod but not yet acked (in-memory; the server
        # replay regenerates this set after a client restart).
        self.unacked = []
        # Persistent ledger, one file per --session. The epoch is a random
        # session identifier: the mod rotates its applied-grant set whenever it
        # sees a new epoch, which is how a new seed is told apart from the old
        # one. There is deliberately NO automatic reset heuristic: a new seed
        # requires an explicit new --session (or --reset-ledger).
        self.ledger_path = ledger_path(args.session)
        self.ledger = {
            "server": args.server,
            "slot": args.slot,
            "session": sanitize_session(args.session),
            "epoch": random.getrandbits(63),
            "delivered": [],
            "location_map": {},
            "pending_checks": [],
        }
        stored = load_ledger(self.ledger_path)
        if stored.get("server") == args.server and stored.get("slot") == args.slot:
            for key in ("epoch", "delivered", "location_map", "pending_checks"):
                if key in stored:
                    self.ledger[key] = stored[key]
            print(f"[AP] loaded ledger '{os.path.basename(self.ledger_path)}': "
                  f"epoch {self.ledger['epoch']}, "
                  f"{len(self.ledger['delivered'])} delivered indice(s), "
                  f"{len(self.ledger['pending_checks'])} pending check(s)",
                  flush=True)
        elif stored:
            print("[AP] ledger is for a different (server, slot); starting fresh",
                  flush=True)
        self.save()

    def save(self):
        """Persist the ledger. Returns False on IO failure (callers must not
        treat the operation as complete)."""
        try:
            save_ledger(self.ledger_path, self.ledger)
            return True
        except OSError as e:
            print(f"[AP] ERROR: could not persist ledger: {e}", flush=True)
            return False

    # ---------- Archipelago server side ----------

    async def ap_loop(self):
        while True:
            try:
                uri = f"ws://{self.args.server}"
                print(f"[AP] connecting to server {uri} ...", flush=True)
                async with websockets.connect(uri, max_size=2 ** 24) as ws:
                    self.ap_ws = ws
                    await self.send_ap({
                        "cmd": "Connect",
                        "password": self.args.password,
                        "game": self.args.game,
                        "name": self.args.slot,
                        "uuid": str(uuid.uuid4()),
                        "version": AP_VERSION,
                        "items_handling": 0b111,
                        "tags": [],
                        "slot_data": True,
                    })
                    async for raw in ws:
                        for packet in json.loads(raw):
                            await self.on_ap_message(packet)
            except Exception as e:
                print(f"[AP] server connection error: {type(e).__name__}: {e}; retrying in 3s", flush=True)
            finally:
                self.ap_ws = None
                self.ap_connected = False
            await asyncio.sleep(3)

    async def send_ap(self, msg):
        # Archipelago protocol: every transmission is a JSON *array* of packets.
        if self.ap_ws is not None:
            await self.ap_ws.send(json.dumps([msg]))

    async def on_ap_message(self, msg):
        cmd = msg.get("cmd")
        if cmd == "RoomInfo":
            pass
        elif cmd == "ConnectionRefused":
            print("[AP] connection refused:", msg.get("errors"), flush=True)
        elif cmd == "Connected":
            await self.on_connected(msg)
        elif cmd == "ReceivedItems":
            await self.on_received_items(msg)
        elif cmd == "PrintJSON":
            text = "".join(p.get("text", "") for p in msg.get("data", []))
            if text.strip():
                print(f"[AP] {text.strip()}", flush=True)
        # other packets ignored for PoC

    async def on_connected(self, msg):
        self.ap_connected = True
        self.slot_data = msg.get("slot_data", {})
        self.checked_locations = set(msg.get("checked_locations", []))
        print(f"[AP] connected as '{self.args.slot}' "
              f"(team {msg.get('team')}, slot {msg.get('slot')})", flush=True)
        print(f"[AP] already checked: {len(self.checked_locations)} location(s)",
              flush=True)

        # No automatic new-seed reset: if the ledger has history but the server
        # reports zero checks, that is either a new seed (user must restart with
        # --reset-ledger or a new --session) or something unexpected. Warn loudly
        # and keep the ledger — deleting legitimate state automatically is worse
        # than pausing for a human decision.
        if (self.ledger["delivered"] or self.ledger["location_map"]
                or self.ledger["pending_checks"]) and not self.checked_locations:
            print("[AP] WARNING: ledger has history but the server reports zero "
                  "checked locations. If this is a NEW seed, stop and restart with "
                  "--reset-ledger (or a new --session). Continuing with the "
                  "existing ledger.", flush=True)

        await self.send_ap({"cmd": "Sync"})
        await self.reconcile_pending_checks()
        if self.mod_ws is not None and self.mod_validated:
            await self.send_mod({"cmd": "connected"})

    async def reconcile_pending_checks(self):
        pending = self.ledger["pending_checks"]
        if not pending:
            return
        fresh = [c for c in pending
                 if c["location_id"] not in self.checked_locations]
        stale = len(pending) - len(fresh)
        if stale:
            print(f"[AP] dropping {stale} queued check(s) already checked on server",
                  flush=True)
        if fresh:
            locations = [c["location_id"] for c in fresh]
            print(f"[AP] reconciling {len(fresh)} queued check(s): locations "
                  f"{locations}", flush=True)
            await self.send_ap({"cmd": "LocationChecks", "locations": locations})
        self.ledger["pending_checks"] = []
        self.save()

    async def on_received_items(self, msg):
        index = msg.get("index", 0)
        items = msg.get("items", [])
        delivered = set(self.ledger["delivered"])
        to_grant = []
        for i, item in enumerate(items):
            g = index + i
            if g in delivered or g in self.unacked:
                continue
            # AP 0.6.8 encodes NetworkItem as a JSON object. Retain support
            # for the positional representation used by earlier test fixtures.
            if isinstance(item, dict):
                item_id, from_player = item["item"], item["player"]
            elif isinstance(item, (list, tuple)) and len(item) == 4:
                item_id, from_player = item[0], item[2]
            else:
                raise ValueError("Invalid ReceivedItems NetworkItem representation")
            print(f"[AP]   item_id={item_id} from player slot {from_player} "
                  f"(index {g})", flush=True)
            to_grant.append(g)
        if not to_grant:
            return
        print(f"[AP] received {len(to_grant)} new item(s) from the multiworld",
              flush=True)
        self.unacked.extend(to_grant)
        await self.send_grant(to_grant)

    async def send_grant(self, indices):
        # PoC: every item becomes one skill point. Indices are only marked
        # delivered when the mod acks them (see on_grant_ack). Grants go only
        # to a handshake-validated mod socket — never to an unknown socket.
        if self.mod_ws is None or not self.mod_validated:
            print(f"[mod] not connected/validated; {len(indices)} item(s) queued "
                  f"for delivery on reconnect", flush=True)
            return
        await self.send_mod({
            "cmd": "grant_items",
            "epoch": self.ledger["epoch"],
            "indices": indices,
        })

    async def on_grant_ack(self, msg):
        # Acks carry the epoch they were applied under; ignore stale ones so a
        # delayed ack from a previous session can't corrupt this ledger.
        if msg.get("epoch") != self.ledger["epoch"]:
            print(f"[AP] WARNING: ignoring grant_ack for stale epoch "
                  f"{msg.get('epoch')} (current {self.ledger['epoch']})", flush=True)
            return
        indices = msg.get("indices", [])
        if not isinstance(indices, list):
            return
        newly = []
        for g in indices:
            try:
                g = int(g)
            except (TypeError, ValueError):
                continue
            if g not in self.ledger["delivered"] and g not in newly:
                newly.append(g)
        if not newly:
            return
        # Commit only after the ledger write succeeds: on IO failure the
        # indices stay unacked, the mod dedups the re-send, and nothing is
        # falsely marked delivered.
        candidate = dict(self.ledger)
        candidate["delivered"] = sorted(set(self.ledger["delivered"]) | set(newly))
        try:
            save_ledger(self.ledger_path, candidate)
        except OSError as e:
            print(f"[AP] ERROR: could not persist delivery of indices "
                  f"{sorted(newly)}: {e}; keeping them unacked", flush=True)
            return
        self.ledger["delivered"] = candidate["delivered"]
        for g in newly:
            if g in self.unacked:
                self.unacked.remove(g)
        print(f"[AP] delivery confirmed for item indices {sorted(newly)} "
              f"(total delivered: {len(self.ledger['delivered'])})", flush=True)

    # ---------- location mapping & checks ----------

    def location_for_mission(self, mission_id):
        """Distinct APQuest location per mission; repeats reuse the same one."""
        key = str(mission_id)
        known = self.ledger["location_map"].get(key)
        if known is not None:
            return known
        used = set(self.ledger["location_map"].values())
        for loc in APQUEST_LOCATION_POOL:
            if loc not in used:
                self.ledger["location_map"][key] = loc
                self.save()
                return loc
        print("[AP] WARNING: location pool exhausted; reusing location 1",
              flush=True)
        self.ledger["location_map"][key] = APQUEST_LOCATION_POOL[0]
        self.save()
        return APQUEST_LOCATION_POOL[0]

    async def report_check(self, mission_id, mission_title):
        location_id = self.location_for_mission(mission_id)
        if not self.ap_connected:
            self.ledger["pending_checks"].append({
                "mission_id": mission_id,
                "mission_title": mission_title,
                "location_id": location_id,
            })
            if not self.save():
                print(f"[AP] ERROR: queued check for mission {mission_id} could "
                      f"NOT be persisted; it will be lost if the bridge restarts "
                      f"before reconnecting", flush=True)
            else:
                print(f"[AP] server offline; check queued: mission {mission_id} "
                      f"'{mission_title}' -> AP location {location_id}", flush=True)
            return
        print(f"[AP] check: mission {mission_id} '{mission_title}' "
              f"-> AP location {location_id}", flush=True)
        await self.send_ap({"cmd": "LocationChecks",
                            "locations": [location_id]})

    # ---------- Craftopia mod side (local bridge) ----------

    async def mod_handler(self, ws, path=None):
        # Connection ownership: this PoC serves exactly one game. A second
        # socket must never overwrite the owner's socket or validation state —
        # not even while the owner's handshake is still pending. Extra sockets
        # are rejected outright.
        if self.mod_ws is not None:
            print("[mod] REJECTED: a game connection already owns this bridge; "
                  "closing the extra socket", flush=True)
            await self._close_ws(ws)
            return
        self.mod_ws = ws
        self.mod_validated = False
        print("[mod] socket connected, awaiting handshake", flush=True)
        try:
            async for raw in ws:
                try:
                    msg = json.loads(raw)
                except Exception:
                    print("[mod] bad message, ignoring", flush=True)
                    continue
                if not self.mod_validated:
                    # The first message must be a valid handshake; until then
                    # this socket is not eligible for grants.
                    if not await self.try_handshake(ws, msg):
                        break
                    continue
                await self.on_mod_message(msg)
        except Exception as e:
            print(f"[mod] disconnected: {e}", flush=True)
        finally:
            # Only the owner clears the slot, and only for its own socket.
            if self.mod_ws is ws:
                self.mod_ws = None
                self.mod_validated = False

    async def try_handshake(self, ws, msg):
        """Validate a mod handshake. Returns True iff the socket is now
        eligible for grants. Missing/blank slot names are rejected (socket
        closed, no abort — it may be a stray connection); a mismatching
        non-blank name is a genuine misconfiguration and aborts."""
        if not isinstance(msg, dict) or msg.get("cmd") != "connect":
            print("[mod] REJECTED: first message was not a handshake", flush=True)
            await self._close_ws(ws)
            return False
        name = (msg.get("name", "") or "").strip()
        if not name:
            print("[mod] REJECTED: handshake with missing/blank slot name", flush=True)
            await self._close_ws(ws)
            return False
        if name != self.args.slot:
            print(f"[mod] FATAL: mod slot name '{name}' != client slot "
                  f"'{self.args.slot}'. Fix one of them and restart.", flush=True)
            raise SystemExit(1)
        self.mod_validated = True
        print(f"[mod] handshake validated (slot '{name}')", flush=True)
        if self.ap_connected:
            await self.send_mod({"cmd": "connected"})
        if self.unacked:
            print(f"[mod] re-sending {len(self.unacked)} unacked item(s)", flush=True)
            await self.send_grant(list(self.unacked))
        return True

    @staticmethod
    async def _close_ws(ws):
        try:
            await ws.close()
        except Exception:
            pass

    async def on_mod_message(self, msg):
        cmd = msg.get("cmd")
        if cmd == "connect":
            # Handshake already validated in try_handshake; a repeat is harmless.
            print("[mod] duplicate handshake ignored", flush=True)
        elif cmd == "check":
            await self.report_check(msg.get("mission_id", -1),
                                    msg.get("mission_title", ""))
        elif cmd == "grant_ack":
            await self.on_grant_ack(msg)
        else:
            print(f"[mod] unknown cmd: {cmd}", flush=True)

    async def send_mod(self, msg):
        if self.mod_ws is not None:
            try:
                await self.mod_ws.send(json.dumps(msg))
            except Exception as e:
                print(f"[mod] send failed: {e}", flush=True)

    async def run(self):
        server = await websockets.serve(self.mod_handler, "127.0.0.1", self.args.port)
        print(f"[mod] local bridge listening on 127.0.0.1:{self.args.port}", flush=True)
        await asyncio.gather(server.wait_closed(), self.ap_loop())


def main():
    ap = argparse.ArgumentParser(description="CraftopiaArchipelago PoC bridge client")
    ap.add_argument("--server", default="localhost:38281",
                    help="Archipelago server address (default localhost:38281)")
    ap.add_argument("--slot", default="Player1", help="slot name (must match YAML)")
    ap.add_argument("--game", default="APQuest",
                    help="game to pretend to be for the PoC (must match the slot's game)")
    ap.add_argument("--password", default=None)
    ap.add_argument("--port", type=int, default=11341,
                    help="local bridge port for the game mod")
    ap.add_argument("--session", default="default",
                    help="delivery session name; the ledger file is per-session. "
                         "Use a NEW session (or --reset-ledger) when testing a NEW seed.")
    ap.add_argument("--reset-ledger", action="store_true",
                    help="discard this session's previous delivery ledger and start "
                         "fresh with a new epoch (do this when testing a NEW seed)")
    args = ap.parse_args()
    path = ledger_path(args.session)
    if args.reset_ledger and os.path.exists(path):
        os.remove(path)
        print("[AP] ledger reset by --reset-ledger", flush=True)
    print(f"PoC client: slot='{args.slot}' game='{args.game}' server={args.server}",
          flush=True)
    bridge = Bridge(args)
    try:
        asyncio.run(bridge.run())
    except KeyboardInterrupt:
        print("bye", flush=True)


if __name__ == "__main__":
    main()
