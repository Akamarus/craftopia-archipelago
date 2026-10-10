#!/usr/bin/env python3
"""CraftopiaArchipelago bridge client for the native Craftopia world (v1.0).

Sits between the Craftopia BepInEx mod (local websocket) and an Archipelago
0.6.8 server, as a native "Craftopia" game slot (not the APQuest proxy).

Wire protocol (Archipelago 0.6.8):
- Every transmission is a JSON *array* of packets. When the vendored
  AP 0.6.8 NetUtils is available it is used for encoding/decoding (it
  understands Version/NetworkItem typed packets); otherwise stdlib json is
  used, which is protocol-compatible for the packet types we send.

Local mod protocol (authoritative: mod/CraftopiaArchipelago/Bridge.cs):
- Mod -> bridge: {"cmd": "connect", "game": "Craftopia", "name": "<slot>"}
- Mod -> bridge: {"cmd": "check", "mission_id": <int>, "mission_title": "<str>"}
- Mod -> bridge: {"cmd": "grant_ack", "indices": [...], "epoch": <int>}
- Bridge -> mod: {"cmd": "grant_items", "indices": [...], "epoch": <int>}
- Bridge -> mod: {"cmd": "connected"} / {"cmd": "print", "text": "..."}

Delivery semantics (see docs/DELIVERY.md for the full strategy):
- Item grants carry virtual indices derived from AP ReceivedItems positions.
  An index counts as DELIVERED only when the mod acks it back after applying
  the skill point(s) in-game AND the ack has been durably recorded in the
  ledger. Failed applies / failed persists are retried, never falsely acked.
- A "Skill Point" item is worth slot_data["skill_points_per_check"] skill
  points. Because the mod applies exactly one point per grant index, each AP
  item index g expands to virtual indices [g*STRIDE .. g*STRIDE+N-1]
  (STRIDE=5 covers the option's range_end). The mod dedups by virtual index,
  so re-sends are safe and the ledger stays consistent.
- Legacy Victory items are display-only; configured objective evidence wins.
- Delivery state is persisted to a per-session ledger file, so a bridge
  restart never re-grants items the server replays. A new seed requires an
  explicit new --session (or --reset-ledger); there is no automatic reset.
- The ledger epoch is a random session identifier; the mod rotates its
  applied-grant set on a new epoch, and stale-epoch acks are ignored.
- Grants that can't be sent yet (mod offline) stay queued and are re-sent
  when the mod reconnects. The mod dedups by index, so re-sends are safe.
- Location checks made while the server is offline are queued and reconciled
  on reconnect (already-checked locations are skipped).
- Mission IDs map to AP location IDs via slot_data["game_id_to_location"]
  (game ID -> location name) resolved through the server's DataPackage
  (location name -> location ID). Unmapped game IDs are logged and ignored,
  never crash the bridge.
- Slot identity is validated: a mod slot-name mismatch aborts the bridge.

Requires: pip install websockets
Usage:    python craftopia_bridge.py --server localhost:38281 --slot Player1
          python craftopia_bridge.py --server localhost:38281 --slot Player1 --game-id-dump
"""
import argparse
import asyncio
import datetime
import json
import os
import random
import sys
import time

# Held for process lifetime to ensure single bridge per session.
_authority_lock_file = None
import uuid
from delivery_state import candidate, validate, valid_unlock, integer, text as valid_text, COMBAT

try:
    import websockets
except ImportError:
    sys.exit("Missing dependency: pip install websockets")

# Official AP 0.6.8 wire codec when available. Falls back to stdlib json,
# which is protocol-compatible for the packet shapes we use.
try:
    sys.path.insert(0, os.path.expanduser("~/workspace/vendor/ap-0.6.8"))
    from NetUtils import encode as ap_encode, decode as ap_decode
    HAVE_AP_NETUTILS = True
except Exception:
    HAVE_AP_NETUTILS = False

    def ap_encode(obj):
        return json.dumps(obj, separators=(",", ":"))

    def ap_decode(s):
        return json.loads(s)

GAME = "Craftopia"
AP_VERSION = {"major": 0, "minor": 6, "build": 8, "class": "Version"}

# Virtual grant-index stride. AP ReceivedItems index g holding a "Skill Point"
# item worth N points expands to virtual indices g*STRIDE .. g*STRIDE+N-1.
# STRIDE must exceed the maximum skill_points_per_check (world option
# range_end = 5) so virtual indices from different AP indices never collide.
POINTS_STRIDE = 5

CLIENT_GOAL = 30  # NetUtils.ClientStatus.CLIENT_GOAL

LEDGER_DIR = os.path.dirname(os.path.abspath(__file__))


class _DumpDone(BaseException):
    """Raised to unwind cleanly after --game-id-dump prints its output."""
    pass


def sanitize_session(s):
    cleaned = "".join(c for c in (s or "") if c.isalnum() or c in "-_")
    return cleaned or "default"


def ledger_path(session):
    return os.path.join(LEDGER_DIR,
                         f"craftopia_bridge_ledger_{sanitize_session(session)}.json")


def load_ledger(path):
    """Load ledger; returns (data, was_corrupt).
    
    If corrupt, preserves the file as .corrupt-<timestamp>, writes a .hold
    marker, and returns (empty_dict, True). The caller must NOT start a new
    history; instead, it must enter a recovery hold.
    The .hold marker persists across restarts until explicit recovery.
    """
    if os.path.exists(path + ".hold"):
        return {}, True
    try:
        with open(path, "r", encoding="utf-8") as f:
            return validate(json.load(f)), False
    except FileNotFoundError:
        return {}, False
    except (OSError, ValueError, TypeError) as e:
        # Preserve original evidence in place. Never turn an unreadable store
        # into a new epoch, even if writing the hold marker also fails.
        try:
            with open(path + ".hold", "w") as f: f.write(str(e))
        except OSError: pass
        print("[AP] RECOVERY HOLD: " + str(e), flush=True)
        return {}, True


def save_ledger(path, data):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        f.flush()
        os.fsync(f.fileno())
    os.replace(tmp, path)


class Bridge:
    def __init__(self, args):
        self.args = args
        self.ap_ws = None
        self.mod_ws = None
        # A mod socket becomes eligible for grants only after its handshake
        # (connect with a matching non-blank slot name) has been validated.
        self.mod_validated = False
        self.connection_token = None
        self.retry_item_messages = []
        self.ap_connected = False
        self.slot_data = {}
        self.checked_locations = set()
        # Recovery hold: set if ledger was corrupt at startup. Blocks all
        # delivery until explicit recovery.
        self.recovery_hold = False

        # Native world data, populated from slot_data + DataPackage.
        self.game_id_to_location = {}   # str/int game ID -> AP location name
        self.class_to_location = {}   # mission class name -> AP location name
        self.skill_points_per_check = 1
        self.goal = None
        self.location_name_to_id = {}   # AP location name -> location ID
        self.item_name_to_id = {}       # AP item name -> item ID
        self.item_id_to_name = {}       # reverse map for logging
        self.skill_point_item_id = None
        self.victory_item_id = None
        self.datapackage_ready = False
        # ReceivedItems messages that arrived before the DataPackage; they
        # cannot be classified until item IDs are known.
        self.pending_item_messages = []
        # Checks that arrived before the DataPackage; resolved once it lands.
        self.deferred_checks = []
        # Non-grant item indices seen (Victory/unknown), to avoid log spam on
        # every server replay. In-memory only; re-logging after a restart is
        # harmless.
        self.seen_non_grant = set()

        # Indices sent to the mod but not yet acked (in-memory; the server
        # replay regenerates this set after a client restart). These are
        # VIRTUAL indices (see POINTS_STRIDE).
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
            "game": GAME,
            "session": sanitize_session(args.session),
            "format_version": 2,
            "epoch": random.randrange(1, 2**63),
            "unlocks": [], "pending_unlocks": [],
            "starters_applied": False, "starter_choice": None,
            "goal_events": {"boss_defeated": False, "boss_id": None, "max_age_reached": 0, "completed_objectives": []},
            "delivered": [],
            "pending_checks": [],
            "enrollments": [],
            "char_grants": {},
            "unassigned_points": [],
            "pending_bundles": [],
            "team_consumables": [],
            "seen_events": [],
        }
        # Cooperative multiplayer state.
        # multiplayer_mode comes from slot_data on AP connect (0 = separate
        # slots sharing one world, 1 = one shared slot). Until then, 0.
        self.multiplayer_mode = 0
        # Shared-slot mode: extra validated mod connections beyond the primary
        # (self.mod_ws). token -> {"ws": ws, "guid": character_guid|None,
        # "validated": bool}.
        self.extra_mod_conns = {}
        # Primary connection's enrolled character guid (shared mode).
        self.primary_guid = None
        # Last multiplayer registry poll (monotonic seconds).
        self.mp_last_poll = 0.0
        # Single-AP-connection authority lock file (set in main()).
        self.authority_path = None
        self.authority_last_beat = 0.0
        stored, was_corrupt = load_ledger(self.ledger_path)
        if was_corrupt:
            # RECOVERY HOLD: do not start a new history. Delivery is stopped.
            # The user must explicitly recover (inspect the .corrupt file,
            # restore or reset with --reset-ledger).
            self.recovery_hold = True
            print("[AP] RECOVERY HOLD: corrupt ledger detected. Delivery STOPPED. "
                  "Inspect the .corrupt file and restart with --reset-ledger "
                  "to start fresh (after reviewing).", flush=True)
            # Do not save (would overwrite the corrupt evidence marker).
            return
        self.recovery_hold = False
        if (stored.get("server") == args.server
                and stored.get("slot") == args.slot
                and stored.get("game") == GAME
                and stored.get("session") == sanitize_session(args.session)):
            self.ledger = candidate(stored)
            # Restore goal events from ledger.
            self.restore_goal_events()
            print(f"[AP] loaded ledger '{os.path.basename(self.ledger_path)}': "
                  f"epoch {self.ledger['epoch']}, "
                  f"{len(self.ledger['delivered'])} delivered indice(s), "
                  f"{len(self.ledger['pending_checks'])} pending check(s)",
                  flush=True)
        elif stored:
            self.recovery_hold = True
            print("[AP] RECOVERY HOLD: ledger identity mismatch; original preserved", flush=True)
            return
        if not self.save(): self.recovery_hold = True

    def save(self):
        return self.commit(candidate(self.ledger))

    def commit(self, proposed):
        if self.recovery_hold: return False
        try:
            validate(proposed)
            save_ledger(self.ledger_path, proposed)
        except (OSError, ValueError) as e:
            print("[AP] persistence failed: " + str(e), flush=True)
            return False
        self.ledger = proposed
        self.restore_goal_events()
        return True

    def can_deliver_unlocks(self):
        return (not self.recovery_hold and self.ap_connected
                and self._any_mod_validated()
                and bool(self._all_conn_tokens()))

    def _any_mod_validated(self):
        if self.mod_ws is not None and self.mod_validated:
            return True
        return any(c.get("validated") for c in self.extra_mod_conns.values())

    def _all_conn_tokens(self):
        toks = set()
        if self.connection_token:
            toks.add(self.connection_token)
        toks.update(self.extra_mod_conns.keys())
        return toks

    def _valid_conn_token(self, token):
        return token is not None and token in self._all_conn_tokens()

    def _token_ws(self, token):
        if token == self.connection_token and self.mod_ws is not None and self.mod_validated:
            return self.mod_ws
        conn = self.extra_mod_conns.get(token)
        if conn is not None and conn.get("validated"):
            return conn["ws"]
        return None

    def _token_guid(self, token):
        # token None means the primary connection.
        if token is None or token == self.connection_token:
            return self.primary_guid
        conn = self.extra_mod_conns.get(token)
        return conn.get("guid") if conn else None

    def _iter_conns(self):
        # Yield (token, {"ws","guid","validated"}) for every validated mod
        # connection: primary first, then extras.
        if self.mod_ws is not None and self.mod_validated and self.connection_token:
            yield self.connection_token, {"ws": self.mod_ws, "guid": self.primary_guid,
                                          "validated": True}
        for token, conn in self.extra_mod_conns.items():
            if conn.get("validated"):
                yield token, conn

    def _first_validated_token(self):
        for token, _conn in self._iter_conns():
            return token
        return None

    def queue_unlock(self, record):
        if self.recovery_hold or not valid_unlock(record): return False
        old = next((u for u in self.ledger['unlocks'] if u['receipt_id']==record['receipt_id']), None)
        if old is not None:
            if old != record:
                self.recovery_hold = True
                print('[AP] RECOVERY HOLD: receipt payload changed', flush=True)
                return False
            return True
        proposed = candidate(self.ledger)
        proposed['unlocks'].append(record)
        proposed['pending_unlocks'].append(record)
        return self.commit(proposed)

    async def replay_unlocks(self, all_history=False, token=None):
        if not self.can_deliver_unlocks(): return
        if all_history or getattr(self, 'needs_unlock_replay', False):
            self.needs_unlock_replay = True
            proposed = candidate(self.ledger)
            proposed['pending_unlocks'] = candidate(self.ledger['unlocks'])
            if not self.commit(proposed): return
            self.needs_unlock_replay = False
        records = self.ledger['pending_unlocks']
        if token is not None:
            await self._replay_to(records, token)
        else:
            # Broadcast: stamp each connection's own token so its acks
            # validate (a shared-mode extra has no primary token).
            for tok, _conn in self._iter_conns():
                await self._replay_to(records, tok)

    async def _replay_to(self, records, token):
        for record in list(records):
            await self.send_mod(dict(record, epoch=self.ledger['epoch'],
                                     slot=self.args.slot,
                                     connection_token=token), token=token)

    async def accept_unlock_ack(self, msg):
        if not self.can_deliver_unlocks(): return
        if (type(msg.get('epoch')) is not int or msg['epoch'] != self.ledger['epoch']
                or msg.get('slot') != self.args.slot
                or not self._valid_conn_token(msg.get('connection_token'))
                or not integer(msg.get('receipt_id'), -7)): return
        proposed=candidate(self.ledger)
        proposed['pending_unlocks']=[u for u in proposed['pending_unlocks'] if u['receipt_id']!=msg['receipt_id']]
        self.commit(proposed)  # History is NEVER removed by an ack.

    async def maintenance(self):
        while True:
            await asyncio.sleep(1)
            if self.recovery_hold: continue
            try:
                # Retain work until processing completes; exceptions must not
                # terminate retries or drop the rest of this batch.
                for message in list(self.retry_item_messages):
                    self.retry_item_messages.remove(message)
                    try: await self.process_received_items(message)
                    except Exception:
                        if message not in self.retry_item_messages: self.retry_item_messages.append(message)
                        raise
                await self.apply_starters()
                await self.replay_unlocks()
                await self.reconcile_pending_checks()
                await self.check_goal_completion()
                now = time.monotonic()
                if now - self.mp_last_poll >= 5:
                    self.mp_last_poll = now
                    self.mp_register()
                    await self.mp_poll()
                if self.authority_path and now - self.authority_last_beat >= 30:
                    self.authority_last_beat = now
                    self._authority_beat()
            except Exception as e:
                print('[AP] retry deferred: ' + str(e), flush=True)

    def _authority_beat(self):
        # Authority is the lifetime OS lock, never a PID in diagnostic JSON.
        # Windows denies reads through a second handle to the locked byte.
        # Refresh only the unlocked diagnostic sidecar.
        if not self.authority_path:
            return
        try:
            with open(self.authority_path + ".info", "w", encoding="utf-8") as f:
                json.dump({"pid": os.getpid(), "ts": time.time()}, f)
        except OSError as e:
            print(f"[AP] authority beat failed: {e}", flush=True)

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
                        "game": GAME,
                        "name": self.args.slot,
                        "uuid": str(uuid.uuid4()),
                        "version": AP_VERSION,
                        "items_handling": 0b111,
                        "tags": [],
                        "slot_data": True,
                    })
                    async for raw in ws:
                        for packet in ap_decode(raw):
                            await self.on_ap_message(packet)
            except _DumpDone:
                print("[AP] dump complete, exiting", flush=True)
                return
            except Exception as e:
                print(f"[AP] server connection error: {type(e).__name__}: {e}; "
                      f"retrying in 3s", flush=True)
            finally:
                self.ap_ws = None
                self.ap_connected = False
                self.datapackage_ready = False
            await asyncio.sleep(3)

    async def send_ap(self, msg):
        # Archipelago protocol: every transmission is a JSON *array* of packets.
        if self.ap_ws is not None:
            await self.ap_ws.send(ap_encode([msg]))

    async def on_ap_message(self, msg):
        cmd = msg.get("cmd") if isinstance(msg, dict) else None
        if cmd == "RoomInfo":
            seed = msg.get('seed_name')
            if not valid_text(seed):
                self.recovery_hold = True
                return
            previous = self.ledger.get('seed_name')
            if previous is not None and previous != seed:
                self.recovery_hold = True
                print('[AP] RECOVERY HOLD: seed identity changed; use a reviewed new session', flush=True)
                return
            proposed = candidate(self.ledger)
            proposed['seed_name'] = seed
            if not self.commit(proposed): self.recovery_hold = True
        elif cmd == "ConnectionRefused":
            print("[AP] connection refused:", msg.get("errors"), flush=True)
        elif cmd == "Connected":
            await self.on_connected(msg)
        elif cmd == "DataPackage":
            await self.on_datapackage(msg)
        elif cmd == "ReceivedItems":
            await self.on_received_items(msg)
        elif cmd == "RoomUpdate":
            await self.on_room_update(msg)
        elif cmd == "PrintJSON":
            text = "".join(p.get("text", "") for p in msg.get("data", []))
            if text.strip():
                print(f"[AP] {text.strip()}", flush=True)
        # other packets ignored

    async def on_room_update(self, msg):
        self.checked_locations.update(msg.get('checked_locations',[]))
        await self.reconcile_pending_checks()
        await self.check_goal_completion()

    async def check_goal_completion(self):
        if self.ap_connected and not self.recovery_hold and self.check_goal_complete():
            if not getattr(self, 'goal_announced', False):
                await self.send_ap({'cmd':'StatusUpdate','status':CLIENT_GOAL})
                self.goal_announced = True

    async def on_connected(self, msg):
        self.goal_announced = False
        self.ap_connected = True
        self.slot_data = msg.get("slot_data", {}) or {}
        self.checked_locations = set(msg.get("checked_locations", []))
        print(f"[AP] connected as '{self.args.slot}' "
              f"(team {msg.get('team')}, slot {msg.get('slot')}) game={GAME}",
              flush=True)
        print(f"[AP] already checked: {len(self.checked_locations)} location(s)",
              flush=True)

        # Native world slot_data.
        self.game_id_to_location = self.slot_data.get("game_id_to_location", {}) or {}
        self.class_to_location = self.slot_data.get("class_to_location", {}) or {}
        try:
            self.skill_points_per_check = int(
                self.slot_data.get("skill_points_per_check", 1))
        except (TypeError, ValueError):
            self.skill_points_per_check = 1
        if self.skill_points_per_check < 1:
            print(f"[AP] WARNING: skill_points_per_check="
                  f"{self.skill_points_per_check} < 1; clamping to 1", flush=True)
            self.skill_points_per_check = 1
        self.goal = self.slot_data.get("goal")
        mm = self.slot_data.get("multiplayer_mode", 0)
        self.multiplayer_mode = 1 if mm == 1 else 0
        print(f"[AP] multiplayer_mode={self.multiplayer_mode} "
              f"({'shared slot' if self.multiplayer_mode == 1 else 'separate slots'})",
              flush=True)
        print(f"[AP] slot_data: {len(self.game_id_to_location)} game ID(s) mapped, "
              f"skill_points_per_check={self.skill_points_per_check}, "
              f"goal={self.goal}", flush=True)

        # No automatic new-seed reset: if the ledger has history but the server
        # reports zero checks, that is either a new seed (user must restart with
        # --reset-ledger or a new --session) or something unexpected. Warn loudly
        # and keep the ledger — deleting legitimate state automatically is worse
        # than pausing for a human decision.
        if (self.ledger["delivered"] or self.ledger["pending_checks"]) \
                and not self.checked_locations:
            print("[AP] WARNING: ledger has history but the server reports zero "
                  "checked locations. If this is a NEW seed, stop and restart with "
                  "--reset-ledger (or a new --session). Continuing with the "
                  "existing ledger.", flush=True)

        # Resolve location/item names to IDs before classifying anything.
        await self.send_ap({"cmd": "GetDataPackage", "games": [GAME]})
        await self.send_ap({"cmd": "Sync"})
        await self.reconcile_pending_checks()
        if self.multiplayer_mode == 1:
            # Mods may have connected before AP: flush per-character grants
            # and pending team bundles now that the session is live.
            for token, conn in self._iter_conns():
                guid = conn.get("guid")
                if guid:
                    await self.flush_char_grants(guid, token)
            await self.flush_pending_bundles()
        if self.mod_ws is not None and self.mod_validated:
            await self.send_mod({"cmd": "connected"})
            # Session must precede grants; send on AP-first order too.
            await self.send_session()
            # Apply starter unlocks based on starting_combat option.
            await self.apply_starters()
            await self.replay_unlocks(all_history=True)
        await self.check_goal_completion()

    async def apply_starters(self):
        if self.recovery_hold or not self.ap_connected or self.ledger['starters_applied']: return
        starting=self.slot_data.get('starting_combat',0)
        if starting == 0:
            chosen=getattr(self.args,'starter_choice',None)
            if chosen not in COMBAT:
                if not getattr(self,'choice_warning',False):
                    print('[AP] Choose starting combat using --starter-choice; no unlock has been recorded.',flush=True)
                    self.choice_warning=True
                return
            categories=[chosen]
        elif starting == 1:
            import hashlib
            seed=f"{self.args.slot}:{self.ledger['epoch']}:{self.ledger.get('seed_name','')}"
            chosen=random.Random(int(hashlib.sha256(seed.encode()).hexdigest(),16)).choice(COMBAT)
            categories=[chosen]
        elif starting == 2:
            chosen='all_basic'; categories=COMBAT
        else: return
        proposed=candidate(self.ledger)
        for i, category in enumerate(categories):
            record={'cmd':'unlock_equipment','category':category,'tier':1,'receipt_id':-i-1}
            proposed['unlocks'].append(record); proposed['pending_unlocks'].append(record)
        proposed['starters_applied']=True; proposed['starter_choice']=chosen
        self.commit(proposed)

    async def on_datapackage(self, msg):
        games = (msg.get("data") or {}).get("games", {}) or {}
        craftopia = games.get(GAME, {}) or {}
        self.location_name_to_id = craftopia.get("location_name_to_id", {}) or {}
        self.item_name_to_id = craftopia.get("item_name_to_id", {}) or {}
        self.item_id_to_name = {v: k for k, v in self.item_name_to_id.items()}
        self.skill_point_item_id = self.item_name_to_id.get("Skill Point")
        self.victory_item_id = self.item_name_to_id.get("Victory")
        self.datapackage_ready = True
        print(f"[AP] datapackage: {len(self.location_name_to_id)} location(s), "
              f"{len(self.item_name_to_id)} item(s); "
              f"Skill Point id={self.skill_point_item_id}, "
              f"Victory id={self.victory_item_id}", flush=True)
        if self.skill_point_item_id is None:
            print("[AP] WARNING: 'Skill Point' not in datapackage; no items "
                  "can be granted until it is known", flush=True)

        if self.args.game_id_dump:
            self.print_mapping()
            raise _DumpDone()

        # Classify any items that arrived before the datapackage.
        pending = self.pending_item_messages
        self.pending_item_messages = []
        for m in pending:
            await self.process_received_items(m)
        await self.reconcile_pending_checks()
        await self.check_goal_completion()

    def print_mapping(self):
        print("=" * 70)
        print("Craftopia slot_data mapping")
        print(f"  server: {self.args.server}")
        print(f"  slot:   {self.args.slot}")
        print(f"  skill_points_per_check: {self.skill_points_per_check}")
        print(f"  goal: {self.goal}")
        print("=" * 70)
        print(f"{'Game ID':<10} {'AP Location Name':<48} {'AP ID'}")
        print("-" * 70)

        def sort_key(k):
            try:
                return (0, int(k))
            except (TypeError, ValueError):
                return (1, str(k))

        for gid in sorted(self.game_id_to_location, key=sort_key):
            name = self.game_id_to_location[gid]
            lid = self.location_name_to_id.get(name, "?")
            print(f"{str(gid):<10} {name:<48} {lid}")
        print("-" * 70)
        print(f"{len(self.game_id_to_location)} game ID(s)")

    async def reconcile_pending_checks(self):
        if self.recovery_hold or not self.datapackage_ready: return
        proposed=candidate(self.ledger)
        for record in proposed['pending_checks']:
            if record.get('location_id') is None:
                record['location_id']=self.location_id_for_mission(record['mission_id'],record.get('mission_class'))
                record['deferred']=record['location_id'] is None
            name=next((n for n,v in self.location_name_to_id.items() if v==record['location_id']),None)
            objective=self.slot_data.get('objective_locations',{}).get(name)
            if objective and objective not in proposed['goal_events']['completed_objectives']:
                proposed['goal_events']['completed_objectives'].append(objective)
        proposed['pending_checks']=[v for v in proposed['pending_checks'] if v['location_id'] not in self.checked_locations]
        if not self.commit(proposed): return
        ids=sorted({v['location_id'] for v in self.ledger['pending_checks'] if v['location_id'] is not None})
        if self.ap_connected and ids: await self.send_ap({'cmd':'LocationChecks','locations':ids})
        await self.check_goal_completion()

    # ---------- cooperative multiplayer: separate-slot world-event sharing --
    # In separate-slots mode each player runs their own bridge/AP slot against
    # the SAME Craftopia world (same seed). World accomplishments (boss kills,
    # age advancements, shared objective events) detected by any player's mod
    # are credited to every registered slot: the detecting bridge appends the
    # event to a per-seed registry file, and each bridge polls it, applying new
    # events to its own durable goal state. Deduplication is by event_id, which
    # is also recorded in the local ledger (seen_events) so restarts and
    # re-polls never double-apply.
    def mp_registry_path(self):
        seed = self.ledger.get("seed_name")
        if not seed:
            return None
        return os.path.join(LEDGER_DIR,
                             f"craftopia_mp_{sanitize_session(seed)}.json")

    def _mp_atomic(self, path, fn):
        # Read-modify-write under a lock file (O_EXCL creation works on both
        # POSIX and Windows). Operations are tiny; 5s timeout is generous.
        lock_path = path + ".lock"
        deadline = time.time() + 5
        while True:
            try:
                fd = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
                os.close(fd)
                break
            except FileExistsError:
                if time.time() >= deadline:
                    raise TimeoutError("mp registry lock timeout")
                time.sleep(0.05)
        try:
            reg = {}
            if os.path.exists(path):
                with open(path, "r", encoding="utf-8") as f:
                    reg = json.load(f)
            if not isinstance(reg, dict):
                reg = {}
            fn(reg)
            tmp = path + ".tmp"
            with open(tmp, "w", encoding="utf-8") as f:
                json.dump(reg, f)
                f.flush()
                os.fsync(f.fileno())
            os.replace(tmp, path)
        finally:
            try:
                os.remove(lock_path)
            except OSError:
                pass

    def _mp_read(self, path):
        if not os.path.exists(path):
            return {"seed_name": self.ledger.get("seed_name"), "peers": [], "events": []}
        with open(path, "r", encoding="utf-8") as f:
            reg = json.load(f)
        return reg if isinstance(reg, dict) else {}

    def mp_register(self):
        # Presence heartbeat so players can see who shares their world.
        if self.multiplayer_mode != 0 or self.recovery_hold:
            return
        path = self.mp_registry_path()
        if path is None:
            return
        slot = self.args.slot
        now = time.time()

        def _update(reg):
            peers = reg.setdefault("peers", [])
            for p in peers:
                if p.get("slot") == slot:
                    p["last_seen"] = now
                    break
            else:
                peers.append({"slot": slot, "last_seen": now})
            # Prune peers silent for >10 minutes and cap the event log.
            reg["peers"] = [p for p in peers if now - p.get("last_seen", 0) < 600]
            events = reg.setdefault("events", [])
            del events[:-500]
        try:
            self._mp_atomic(path, _update)
        except Exception as e:
            print(f"[AP] mp register failed: {e}", flush=True)

    def mp_broadcast(self, event_type, fields, event_id=None):
        # Append a world event to the shared registry. Returns the event_id.
        if self.multiplayer_mode != 0 or self.recovery_hold:
            return None
        path = self.mp_registry_path()
        if path is None:
            return None
        event_id = event_id or f"{self.args.slot}:{uuid.uuid4().hex}"
        event = {"event_id": event_id, "type": event_type,
                 "origin_slot": self.args.slot, "ts": time.time()}
        event.update(fields or {})

        def _update(reg):
            events = reg.setdefault("events", [])
            if all(e.get("event_id") != event_id for e in events):
                events.append(event)
            del events[:-500]
        try:
            self._mp_atomic(path, _update)
        except Exception as e:
            print(f"[AP] mp broadcast failed: {e}", flush=True)
            return None
        # Mark seen locally so our own poll never re-applies it.
        proposed = candidate(self.ledger)
        if event_id not in proposed.get("seen_events", []):
            proposed["seen_events"].append(event_id)
            self.commit(proposed)
        print(f"[AP] mp broadcast {event_type} {event_id}", flush=True)
        return event_id

    async def mp_poll(self):
        # Apply new shared-registry events to our own durable goal state.
        if self.multiplayer_mode != 0 or self.recovery_hold:
            return
        path = self.mp_registry_path()
        if path is None:
            return
        try:
            reg = self._mp_read(path)
        except Exception as e:
            print(f"[AP] mp poll failed: {e}", flush=True)
            return
        events = reg.get("events", []) if isinstance(reg, dict) else []
        seen = set(self.ledger.get("seen_events", []))
        fresh = [e for e in events
                 if isinstance(e, dict) and valid_text(e.get("event_id"))
                 and e["event_id"] not in seen]
        if not fresh:
            return
        for e in fresh:
            eid = e["event_id"]
            if not self._world_event_valid(e):
                # Permanently invalid for our goal config: mark seen so we
                # never spam about it, without touching goal state.
                proposed = candidate(self.ledger)
                if eid not in proposed["seen_events"]:
                    proposed["seen_events"].append(eid)
                    self.commit(proposed)
                print(f"[AP] mp ignoring event {eid} (invalid for our goals)",
                      flush=True)
                continue
            t = e["type"]
            if t == "boss_defeat":
                ok = self.record_boss_defeat(e.get("boss_id"), event_id=eid)
            elif t == "age_advance":
                ok = self.record_age_advance(e.get("age_level"), event_id=eid)
            else:
                ok = self.record_objective_complete(e.get("objective_id"), event_id=eid)
            if ok:
                print(f"[AP] mp applied {t} {eid} from {e.get('origin_slot')}",
                      flush=True)
        await self.check_goal_completion()

    # ---------- items: server -> game ----------

    def virtual_indices(self, ap_index, count=None):
        """Expand an AP ReceivedItems index into virtual grant indices.

        One "Skill Point" item is worth skill_points_per_check skill points,
        but the mod applies exactly one point per grant index. Virtual index
        g*N+i (N <= POINTS_STRIDE) keeps the mod's per-index dedup and the
        ledger's delivered set working without protocol changes.
        """
        n = self.skill_points_per_check if count is None else count
        base = ap_index * POINTS_STRIDE
        return [base + i for i in range(n)]

    async def on_received_items(self, msg):
        # Cannot classify items until the DataPackage maps item names to IDs.
        if not self.datapackage_ready:
            self.pending_item_messages.append(msg)
            return
        await self.process_received_items(msg)

    @staticmethod
    def _item_id(item):
        """Extract the AP item ID from a NetworkItem in any representation:
        NetUtils NetworkItem (namedtuple), 0.6.8 JSON object, or legacy tuple."""
        if hasattr(item, "item"):  # NetworkItem namedtuple from ap_decode
            return item.item
        if isinstance(item, dict):
            return item["item"]
        if isinstance(item, (list, tuple)) and len(item) == 4:
            return item[0]
        raise ValueError("Invalid ReceivedItems NetworkItem representation")

    async def process_received_items(self, msg):
        if self.recovery_hold: return
        index = msg.get("index", 0)
        items = msg.get("items", [])
        if not integer(index) or not isinstance(items, list):
            print('[AP] invalid ReceivedItems envelope; ignoring', flush=True)
            return
        delivered = set(self.ledger["delivered"])
        to_grant = []
        for i, item in enumerate(items):
            g = index + i
            if g in self.seen_non_grant:
                continue
            try:
                item_id = self._item_id(item)
            except ValueError:
                print(f"[AP] WARNING: invalid NetworkItem at index {g}; skipping",
                      flush=True)
                self.seen_non_grant.add(g)
                continue
            if item_id == self.skill_point_item_id:
                if self.multiplayer_mode == 1:
                    # Shared slot: per-character entitlement. Each enrolled
                    # character gets the points once; points arriving with no
                    # enrolled characters are held for the first enrollment.
                    if not await self.grant_skill_points_shared(g):
                        if msg not in self.retry_item_messages:
                            self.retry_item_messages.append(msg)
                    continue
                # Expand to virtual indices; skip any already delivered/unacked.
                new_virtual = [v for v in
                               self.virtual_indices(g)
                               if v not in delivered and v not in self.unacked]
                if new_virtual:
                    print(f"[AP] Skill Point x{self.skill_points_per_check} "
                          f"at AP index {g} -> virtual indices {new_virtual}",
                          flush=True)
                    to_grant.extend(new_virtual)
            elif item_id == self.victory_item_id:
                print("[AP] *** Victory received ***", flush=True)
                self.seen_non_grant.add(g)
                await self.on_victory()
            else:
                # Team consumables (shared slot): money/material bundles are
                # delivered ONCE to the team — never multiplied by players
                # joining or reconnecting.
                item_name = self.item_id_to_name.get(item_id, f"#{item_id}")
                bundle = self._parse_bundle(item_name)
                if bundle is not None:
                    await self._receive_team_bundle(g, item_name, bundle, msg)
                    continue
                # Check for progression items (equipment tiers, production, skills).
                # These use durable delivery: persist pending, send to mod,
                # require ack, retry on reconnect. See UNLOCK_DELIVERY.md.
                unlock_msg = None
                if item_name.startswith("Equipment Tier: "):
                    # Format: "Equipment Tier: <category> <tier>"
                    parts = item_name.split(": ", 1)[1].rsplit(" ", 1)
                    if len(parts) == 2:
                        category, tier_str = parts
                        try:
                            tier = int(tier_str)
                            unlock_msg = {
                                "cmd": "unlock_equipment",
                                "category": category,
                                "tier": tier,
                                "receipt_id": g,  # AP index as receipt ID
                            }
                        except ValueError:
                            pass
                elif item_name.startswith("Production: "):
                    station_id = item_name.split(": ", 1)[1]
                    unlock_msg = {
                        "cmd": "unlock_production",
                        "station_id": station_id,
                        "receipt_id": g,
                    }
                elif item_name.startswith("Skill Tree: "):
                    # Send the FULL canonical item name (e.g., "Skill Tree: Magic").
                    # The mod stores and checks against this exact string.
                    unlock_msg = {
                        "cmd": "unlock_skill_tree",
                        "tree_id": item_name,
                        "receipt_id": g,
                    }
                if unlock_msg:
                    if not self.queue_unlock(unlock_msg):
                        if msg not in self.retry_item_messages: self.retry_item_messages.append(msg)
                        continue
                    await self.replay_unlocks()
                    self.seen_non_grant.add(g)
                    continue
                print(f"[AP] WARNING: unexpected item '{item_name}' (id {item_id}) "
                      f"at index {g}; not granting", flush=True)
                self.seen_non_grant.add(g)
        if not to_grant:
            return
        print(f"[AP] received {len(to_grant)} new skill-point grant(s) "
              f"from the multiworld", flush=True)
        self.unacked.extend(to_grant)
        await self.send_grant(to_grant)

    async def grant_skill_points_shared(self, ap_index):
        """Queue a received Skill Point for every enrolled character.

        Returns True if the queueing commit succeeded. On commit failure
        returns False so the caller retries the ReceivedItems message.
        """
        if self.recovery_hold:
            return False
        virtual = self.virtual_indices(ap_index)
        proposed = candidate(self.ledger)
        guids = [e.get("character_guid")
                 for e in proposed.get("enrollments", [])]
        if not guids:
            holding = proposed.setdefault("unassigned_points", [])
            if ap_index not in holding:
                holding.append(ap_index)
                print(f"[AP] shared mode: no enrolled characters; holding Skill "
                      f"Point at AP index {ap_index} for first enrollment",
                      flush=True)
            return self.commit(proposed)
        for guid in guids:
            cg = proposed["char_grants"].setdefault(
                guid, {"delivered": [], "pending": []})
            new = [v for v in virtual
                   if v not in cg["delivered"] and v not in cg["pending"]]
            if new:
                cg["pending"].extend(new)
                print(f"[AP] Skill Point x{self.skill_points_per_check} at AP "
                      f"index {ap_index} -> character {guid}: {new}", flush=True)
        if not self.commit(proposed):
            return False
        for token, conn in self._iter_conns():
            guid = conn.get("guid")
            if guid:
                await self.flush_char_grants(guid, token)
        return True

    async def flush_char_grants(self, character_guid, token):
        """Send a character's pending (incl. unacked) grant indices to one
        mod connection. Pending stays durable until that character acks."""
        if self.recovery_hold:
            return
        # Normalize to canonical form for lookup.
        guid_str = self.normalize_guid(character_guid)
        if guid_str is None:
            return
        cg = self.ledger.get("char_grants", {}).get(guid_str)
        if not cg or not cg.get("pending"):
            return
        if self._token_ws(token) is None:
            return
        await self.send_mod({
            "cmd": "grant_items",
            "epoch": self.ledger["epoch"],
            "indices": list(cg["pending"]),
            "character_guid": character_guid,
        }, token=token)

    @staticmethod
    def _parse_bundle(item_name):
        # "Money Bundle: 500" / "Material Bundle: Iron x10" -> (kind, amount).
        # Returns None when the item is not a team bundle.
        for prefix, kind in (("Money Bundle:", "money"),
                             ("Material Bundle:", "material"),
                             ("Bundle:", "bundle")):
            if item_name.startswith(prefix):
                rest = item_name.split(":", 1)[1].strip()
                try:
                    amount = int(rest.split()[0])
                except (ValueError, IndexError):
                    amount = 1
                return kind, max(1, amount)
        return None

    async def _receive_team_bundle(self, ap_index, item_name, bundle, msg):
        kind, amount = bundle
        if ap_index in self.ledger.get("team_consumables", []) or any(
                p.get("receipt_id") == ap_index
                for p in self.ledger.get("pending_bundles", [])):
            self.seen_non_grant.add(ap_index)
            return
        proposed = candidate(self.ledger)
        proposed["pending_bundles"].append(
            {"kind": kind, "amount": amount, "receipt_id": ap_index})
        if not self.commit(proposed):
            if msg not in self.retry_item_messages:
                self.retry_item_messages.append(msg)
            return
        print(f"[AP] team bundle {item_name} queued (receipt {ap_index})", flush=True)
        self.seen_non_grant.add(ap_index)
        await self.flush_pending_bundles()

    async def flush_pending_bundles(self):
        # Team-once delivery: send each pending bundle to ONE validated mod
        # connection; it leaves pending only when a bundle_ack arrives, at
        # which point the receipt is recorded team-delivered and is never
        # re-sent (late joins and reconnects included).
        if self.recovery_hold:
            return
        pending = self.ledger.get("pending_bundles", [])
        if not pending:
            return
        token = self._first_validated_token()
        if token is None:
            return
        for record in list(pending):
            await self.send_mod({
                "cmd": "grant_bundle",
                "kind": record["kind"],
                "amount": record["amount"],
                "receipt_id": record["receipt_id"],
                "epoch": self.ledger["epoch"],
            }, token=token)

    async def _on_bundle_ack(self, msg):
        if msg.get("epoch") != self.ledger["epoch"]:
            return
        rid = msg.get("receipt_id")
        if not integer(rid):
            return
        proposed = candidate(self.ledger)
        if not any(p.get("receipt_id") == rid for p in proposed["pending_bundles"]):
            return
        proposed["pending_bundles"] = [
            p for p in proposed["pending_bundles"] if p.get("receipt_id") != rid]
        if rid not in proposed["team_consumables"]:
            proposed["team_consumables"].append(rid)
        if self.commit(proposed):
            print(f"[AP] team bundle receipt {rid} delivered (team-once)", flush=True)
        await self.flush_pending_bundles()

    async def on_victory(self):
        """The Victory item arrived: notify the mod.

        Per GOAL_EVENT_CONTRACT.md: Victory item possession does NOT trigger
        completion. Completion requires ALL enabled goal conditions to be met
        with actual event evidence (boss defeat, age advancement, objective
        completions). The bridge tracks these events and sends StatusUpdate
        only when the predicate is satisfied.
        """
        # Notify the mod that Victory was received (for display).
        if self.mod_ws is not None and self.mod_validated:
            await self.send_mod({"cmd": "print",
                                 "text": "*** Victory item received! ***"})
        # Check if the event-based predicate is satisfied.
        if self.check_goal_complete():
            print("[AP] GOAL COMPLETE: all enabled conditions met!", flush=True)
            if self.mod_ws is not None and self.mod_validated:
                await self.send_mod({"cmd": "print",
                                     "text": "*** VICTORY: all goals complete! ***"})
            await self.send_ap({"cmd": "StatusUpdate", "status": CLIENT_GOAL})
        else:
            print("[AP] Victory received but goal conditions not yet met; "
                  "waiting for boss/age/objective events", flush=True)

    def check_goal_complete(self):
        if self.recovery_hold: return False
        enabled=[self.slot_data.get(k,False) for k in ('goal_boss','goal_age','goal_objectives')]
        if not any(enabled): return False
        if enabled[0]:
            required=self.slot_data.get('goal_boss_choice',0)
            if not self.boss_defeated or (required != 0 and self.boss_id != str(required)): return False
        if enabled[1]:
            target=self.slot_data.get('goal_age_target')
            if not integer(target,1) or self.max_age_reached < target: return False
        if enabled[2]:
            required=self.slot_data.get('goal_objective_count')
            catalog=set(self.slot_data.get('objective_locations',{}).values())
            if not integer(required,1) or len(self.completed_objectives & catalog) < required: return False
        return True

    def record_boss_defeat(self, boss_id, event_id=None):
        if not valid_text(boss_id): return False
        required=self.slot_data.get('goal_boss_choice',0)
        # No wildcard evidence: an "any" goal needs an explicit verified catalog.
        if required == 0:
            if boss_id not in self.slot_data.get('goal_boss_ids',[]): return False
        elif boss_id != str(required): return False
        proposed=candidate(self.ledger)
        proposed['goal_events'].update(boss_defeated=True,boss_id=boss_id)
        if event_id and event_id not in proposed['seen_events']:
            proposed['seen_events'].append(event_id)
        return self.commit(proposed)

    def record_age_advance(self, age_level, event_id=None):
        if not integer(age_level,1): return False
        proposed=candidate(self.ledger)
        proposed['goal_events']['max_age_reached']=max(self.max_age_reached,age_level)
        if event_id and event_id not in proposed['seen_events']:
            proposed['seen_events'].append(event_id)
        return self.commit(proposed)

    def record_objective_complete(self, objective_id, event_id=None):
        if not valid_text(objective_id) or objective_id not in self.slot_data.get('objective_locations',{}).values(): return False
        proposed=candidate(self.ledger)
        if objective_id not in self.completed_objectives:
            proposed['goal_events']['completed_objectives']=sorted(self.completed_objectives | {objective_id})
        if event_id and event_id not in proposed['seen_events']:
            proposed['seen_events'].append(event_id)
        return self.commit(proposed)

    def _world_event_valid(self, event):
        # Same validation the record_* methods apply, for pre-screening
        # broadcast events before applying them.
        t = event.get("type")
        if t == "boss_defeat":
            boss_id = event.get("boss_id")
            if not valid_text(boss_id):
                return False
            required = self.slot_data.get('goal_boss_choice', 0)
            if required == 0:
                return boss_id in self.slot_data.get('goal_boss_ids', [])
            return boss_id == str(required)
        if t == "age_advance":
            return integer(event.get("age_level"), 1)
        if t == "objective_complete":
            oid = event.get("objective_id")
            return valid_text(oid) and oid in self.slot_data.get('objective_locations', {}).values()
        return False

    def restore_goal_events(self):
        events=self.ledger['goal_events']
        self.boss_defeated=events['boss_defeated']; self.boss_id=events.get('boss_id')
        self.max_age_reached=events['max_age_reached']
        self.completed_objectives=set(events['completed_objectives'])

    # ---------- enrollment (shared-slot mode) ----------
    @staticmethod
    def normalize_guid(value):
        """Canonical GUID normalizer: str(int(value)) for valid nonzero Int32.

        Accepts int or numeric string. Rejects bool, zero, None, empty,
        malformed, and out-of-Int32-range values. Leading zeros are
        canonicalized ('00123' -> '123'). No arbitrary labels in production.
        Returns the canonical string, or None if invalid.
        """
        if isinstance(value, bool):
            return None
        try:
            if isinstance(value, int):
                iv = value
            elif isinstance(value, str):
                s = value.strip()
                if not s:
                    return None
                # Must be a valid integer string (optional leading -).
                if not s.lstrip("-").isdigit():
                    return None
                iv = int(s)
            else:
                return None
        except (ValueError, OverflowError):
            return None
        # Int32 range: -2147483648 to 2147483647, nonzero.
        if iv == 0 or iv < -2147483648 or iv > 2147483647:
            return None
        return str(iv)

    def enroll_character(self, character_guid):
        """Durably enroll a character for per-character skill-point accounting.

        Explicit enrollment only: returns False (and enrolls nothing) under a
        recovery hold or with a missing/malformed guid. A missing or ambiguous
        history never silently becomes a fresh character — the caller must
        refuse delivery until enrollment succeeds.
        """
        if self.recovery_hold:
            print("[AP] RECOVERY HOLD: enrollment refused; explicit recovery required",
                  flush=True)
            return False
        guid_str = self.normalize_guid(character_guid)
        if guid_str is None:
            print(f"[AP] enrollment refused: invalid character_guid {character_guid!r}",
                  flush=True)
            return False
        if any(e.get("character_guid") == guid_str
               for e in self.ledger.get("enrollments", [])):
            return True
        proposed = candidate(self.ledger)
        proposed["enrollments"].append({
            "character_guid": guid_str,
            "slot": self.args.slot,
            "enrolled_at": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        })
        cg = proposed["char_grants"].setdefault(
            guid_str, {"delivered": [], "pending": []})
        # Points that arrived while nobody was enrolled are held in
        # unassigned_points; the first enrollment claims them (once).
        for ap_index in proposed.get("unassigned_points", []):
            for v in self.virtual_indices(ap_index):
                if v not in cg["delivered"] and v not in cg["pending"]:
                    cg["pending"].append(v)
        proposed["unassigned_points"] = []
        if not self.commit(proposed):
            print("[AP] ERROR: enrollment persist failed; not enrolled", flush=True)
            return False
        print(f"[AP] enrolled character {character_guid}", flush=True)
        return True

    async def send_grant(self, indices, token=None):
        # Indices are only marked delivered when the mod acks them (see
        # on_grant_ack). Grants go only to a handshake-validated mod socket —
        # never to an unknown socket.
        if self.recovery_hold:
            print(f"[AP] RECOVERY HOLD: {len(indices)} grant(s) blocked; "
                  f"explicit recovery required", flush=True)
            return
        if not self._any_mod_validated():
            print(f"[mod] not connected/validated; {len(indices)} grant(s) queued "
                  f"for delivery on reconnect", flush=True)
            return
        await self.send_mod({
            "cmd": "grant_items",
            "epoch": self.ledger["epoch"],
            "indices": indices,
        }, token=token)

    async def on_grant_ack(self, msg, token=None):
        # Acks carry the epoch they were applied under; ignore stale ones so a
        # delayed ack from a previous session can't corrupt this ledger.
        if msg.get("epoch") != self.ledger["epoch"]:
            print(f"[AP] WARNING: ignoring grant_ack for stale epoch "
                  f"{msg.get('epoch')} (current {self.ledger['epoch']})", flush=True)
            return
        indices = msg.get("indices", [])
        if not isinstance(indices, list):
            return
        guid = self._token_guid(token) if self.multiplayer_mode == 1 else None
        if guid:
            await self._on_char_grant_ack(guid, indices)
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
        print(f"[AP] delivery confirmed for grant indices {sorted(newly)} "
              f"(total delivered: {len(self.ledger['delivered'])})", flush=True)

    async def _on_char_grant_ack(self, character_guid, indices):
        # Per-character skill-point accounting (shared slot): an ack moves
        # indices from that character's pending to delivered. Other
        # characters' queues are untouched, and a reconnect never re-grants
        # (delivered persists).
        newly = []
        for g in indices:
            try:
                g = int(g)
            except (TypeError, ValueError):
                continue
            if g not in newly:
                newly.append(g)
        if not newly:
            return
        proposed = candidate(self.ledger)
        cg = proposed.get("char_grants", {}).get(character_guid)
        if cg is None:
            return
        acked = [g for g in newly if g in cg["pending"] and g not in cg["delivered"]]
        if not acked:
            return
        cg["pending"] = [g for g in cg["pending"] if g not in acked]
        cg["delivered"] = sorted(set(cg["delivered"]) | set(acked))
        try:
            save_ledger(self.ledger_path, proposed)
        except OSError as e:
            print(f"[AP] ERROR: could not persist char ack for {character_guid}: "
                  f"{e}; keeping pending", flush=True)
            return
        self.ledger = proposed
        self.restore_goal_events()
        print(f"[AP] char {character_guid}: acked {sorted(acked)} "
              f"(delivered: {len(cg['delivered'])})", flush=True)

    # ---------- location mapping & checks: game -> server ----------

    def location_id_for_mission(self, mission_id, mission_class=None):
        """Resolve a game mission to an AP location ID.

        Routing depends on the negotiated catalog version (slot_data):
        - instance (20261007_r1): numeric mission ID only, no class fallback.
          Distinct missions sharing a class must remain distinct.
        - legacy: class name first (stable IL key), then numeric game ID.

        Returns None (after a warning) when unmapped — the caller must
        ignore the check, never crash.
        """
        if not self.datapackage_ready:
            return None
        # Catalog validation with historical compatibility.
        #
        # Supported pairs:
        # - ("legacy", 0): current legacy slots (v1.7+)
        # - ("20261007_r1", 1): instance mode (v1.7+)
        # - (None, None): historical pre-catalog slots (v1.2.1 and earlier).
        #   These have objective_locations but no catalog fields.
        #   Normalized to ("legacy", 0) with active-location enforcement.
        #
        # Explicitly unsupported versions are rejected, not defaulted.
        catalog = self.slot_data.get("mission_catalog_version")
        schema = self.slot_data.get("mission_catalog_schema")
        # Historical payload: has objective_locations but no catalog fields.
        # This is a validated legacy shape, not an unknown catalog.
        is_historical = (
            catalog is None and schema is None
            and isinstance(self.slot_data.get("objective_locations"), dict)
        )
        if is_historical:
            catalog, schema = "legacy", 0
        # Supported pairs:
        # - ("legacy", 0): current canonical legacy (v1.7.2+)
        # - ("legacy", 1): historical v1.7.0/v1.7.1 legacy (emitted schema=1)
        # - ("20261007_r1", 1): instance mode
        # - (None, None) -> normalized to ("legacy", 0) above (pre-catalog)
        supported = {("legacy", 0), ("legacy", 1), ("20261007_r1", 1)}
        # Schema must be exact int (type() is int, not isinstance;
        # isinstance(True, int) is True in Python).
        if type(schema) is not int:
            print(f"[AP] REJECTED: invalid schema type {type(schema).__name__}; "
                  f"ignoring check", flush=True)
            return None
        if (catalog, schema) not in supported:
            print(f"[AP] REJECTED: unsupported catalog/schema "
                  f"({catalog!r}, {schema!r}); ignoring check", flush=True)
            return None
        if catalog == "20261007_r1":
            # Instance mode: numeric ID only, no class fallback.
            # JSON keys may be strings; accept ints too.
            name = self.game_id_to_location.get(str(mission_id))
            if name is None:
                name = self.game_id_to_location.get(mission_id)
            # No class fallback in instance mode.
        else:
            # Legacy: class name is the primary key.
            name = None
            if mission_class:
                name = self.class_to_location.get(mission_class)
            if name is None:
                name = self.game_id_to_location.get(str(mission_id))
                if name is None:
                    name = self.game_id_to_location.get(mission_id)
        if name is None:
            print(f"[AP] WARNING: mission {mission_id} (class {mission_class}) not in "
                  f"slot_data mappings; ignoring check", flush=True)
            return None
        loc_id = self.location_name_to_id.get(name)
        if loc_id is None:
            print(f"[AP] WARNING: location '{name}' (mission {mission_id}) not in "
                  f"DataPackage; ignoring check", flush=True)
            return None
        # Active-location allowlist: the resolved location must belong to
        # this slot's generated network locations. Prevents cross-catalog
        # leaks (e.g., legacy slot routing to instance ID 1013).
        #
        # Empty or malformed objective_locations means NO allowed checks.
        # The format is strictly "location:<integer>".
        active = self.slot_data.get("objective_locations")
        if not isinstance(active, dict):
            print(f"[AP] REJECTED: objective_locations missing or not a dict; "
                  f"ignoring check", flush=True)
            return None
        allowed_ids = set()
        for k, v in active.items():
            # Strict format validation: "location:<integer>"
            if not isinstance(v, str) or not v.startswith("location:"):
                print(f"[AP] REJECTED: malformed objective_locations entry "
                      f"{k!r}: {v!r}; ignoring check", flush=True)
                return None
            try:
                allowed_ids.add(int(v.split(":", 1)[1]))
            except ValueError:
                print(f"[AP] REJECTED: non-integer location ID in "
                      f"{k!r}: {v!r}; ignoring check", flush=True)
                return None
        # Empty allowlist = no allowed checks. Always enforce.
        if loc_id not in allowed_ids:
            print(f"[AP] REJECTED: location {loc_id} ('{name}') not in this slot's "
                  f"active locations; ignoring check", flush=True)
            return None
        return loc_id

    async def report_check(self, mission_id, mission_title, mission_class=None):
        if self.recovery_hold or not integer(mission_id,-2147483648,2147483647): return False
        if mission_class is not None and not valid_text(mission_class): return False
        loc_id=self.location_id_for_mission(mission_id,mission_class) if self.datapackage_ready else None
        if self.datapackage_ready and loc_id is None: return False
        record={'mission_id':mission_id,'mission_title':mission_title,'mission_class':mission_class,
                'location_id':loc_id,'deferred':loc_id is None}
        proposed=candidate(self.ledger)
        key=(mission_class,mission_id)
        if not any((v.get('mission_class'),v['mission_id'])==key for v in proposed['pending_checks']):
            proposed['pending_checks'].append(record)
        if loc_id is not None:
            name=next((n for n,v in self.location_name_to_id.items() if v==loc_id),None)
            objective=self.slot_data.get('objective_locations',{}).get(name)
            if objective:
                proposed['goal_events']['completed_objectives']=sorted(self.completed_objectives | {objective})
        if not self.commit(proposed): return False
        await self.reconcile_pending_checks()
        await self.check_goal_completion()
        return True
            # Entry remains in pending_checks; will be retried on reconnect.

    # ---------- Craftopia mod side (local bridge) ----------

    async def mod_handler(self, ws, path=None):
        # Connection ownership: in separate-slots mode (multiplayer_mode 0)
        # this bridge serves exactly one game. A second socket must never
        # overwrite the owner's socket or validation state — not even while
        # the owner's handshake is still pending. Extra sockets are rejected
        # outright. In shared-slot mode (1) several game clients may connect;
        # the first is the primary, the rest are tracked extras.
        if self.mod_ws is not None and self.multiplayer_mode != 1:
            print("[mod] REJECTED: a game connection already owns this bridge; "
                  "closing the extra socket", flush=True)
            await self._close_ws(ws)
            return
        token = None
        if self.mod_ws is None:
            self.mod_ws = ws
            self.mod_validated = False
        else:
            token = uuid.uuid4().hex
            self.extra_mod_conns[token] = {"ws": ws, "guid": None, "validated": False}
        print("[mod] socket connected, awaiting handshake", flush=True)
        try:
            async for raw in ws:
                try:
                    msg = json.loads(raw)
                except Exception:
                    print("[mod] bad message, ignoring", flush=True)
                    continue
                if token is None and not self.mod_validated:
                    # The first message must be a valid handshake; until then
                    # this socket is not eligible for grants.
                    if not await self.try_handshake(ws, msg):
                        break
                    continue
                if token is not None and not self.extra_mod_conns.get(token, {}).get("validated"):
                    if not await self.try_handshake(ws, msg, token):
                        break
                    continue
                await self.on_mod_message(msg, token)
        except Exception as e:
            print(f"[mod] disconnected: {e}", flush=True)
        finally:
            # Only the owner clears its own slot.
            if token is None:
                if self.mod_ws is ws:
                    self.mod_ws = None
                    self.mod_validated = False
                    self.primary_guid = None
            else:
                self.extra_mod_conns.pop(token, None)

    async def try_handshake(self, ws, msg, token=None):
        """Validate a mod handshake. Returns True iff the socket is now
        eligible for grants. Missing/blank slot names are rejected (socket
        closed, no abort — it may be a stray connection); a mismatching
        non-blank name is a genuine misconfiguration and aborts.
        token None = primary connection; otherwise a shared-mode extra."""
        if not isinstance(msg, dict) or msg.get("cmd") != "connect":
            print("[mod] REJECTED: first message was not a handshake", flush=True)
            await self._close_ws(ws)
            return False
        name = (msg.get("name", "") or "").strip()
        if not name:
            print("[mod] REJECTED: handshake with missing/blank slot name", flush=True)
            await self._close_ws(ws)
            return False
        game = msg.get("game", "")
        if game and game != GAME:
            print(f"[mod] WARNING: handshake game '{game}' != '{GAME}'; "
                  f"continuing but this mod may be for a different game",
                  flush=True)
        if name != self.args.slot:
            print(f"[mod] FATAL: mod slot name '{name}' != client slot "
                  f"'{self.args.slot}'. Fix one of them and restart.", flush=True)
            raise SystemExit(1)
        if msg.get('unlock_protocol') != 2 or game != GAME:
            print('[mod] incompatible protocol/game; upgrade both components',flush=True)
            await self._close_ws(ws)
            return False
        guid = msg.get("character_guid")
        # GUID wire contract: canonical numeric via normalize_guid.
        # (None means no character loaded yet, which is allowed.)
        guid_str = None
        if guid is not None:
            guid_str = self.normalize_guid(guid)
            if guid_str is None:
                print("[mod] REJECTED: malformed character_guid", flush=True)
                await self._close_ws(ws)
                return False
            # Store the canonical form for connection state.
            msg["character_guid"] = guid_str
            guid = guid_str  # Canonical: use normalized form throughout
        if token is None:
            self.connection_token=uuid.uuid4().hex
            self.mod_validated = True
            self.primary_guid = guid
        else:
            conn = self.extra_mod_conns.get(token)
            if conn is None:
                await self._close_ws(ws)
                return False
            conn["validated"] = True
            conn["guid"] = guid
        self_token = token if token is not None else self.connection_token
        print(f"[mod] handshake validated (slot '{name}', game '{game or '?'}'"
              + (f", character {guid}" if guid else "") + ")",
              flush=True)
        # Shared-slot mode: durable per-character enrollment. A missing or
        # malformed guid never silently becomes a character: without a guid
        # the connection gets unlock history but no skill-point grants.
        if self.multiplayer_mode == 1 and guid:
            if self.recovery_hold:
                print("[mod] RECOVERY HOLD: enrollment refused; explicit recovery required",
                      flush=True)
            elif self.enroll_character(guid):
                if token is None:
                    self.primary_guid = guid
                await self.flush_char_grants(guid, self_token)
        if self.ap_connected:
            await self.send_mod({"cmd": "connected"}, token=self_token)
            await self.send_session(token=self_token)
            await self.apply_starters()
        if self.unacked:
            print(f"[mod] re-sending {len(self.unacked)} unacked grant(s)", flush=True)
            await self.send_grant(list(self.unacked), token=self_token)
        # Late joiners get the full durable unlock history (permission state)
        # plus any still-pending team bundles — but never already-delivered
        # team consumables (team-once).
        await self.replay_unlocks(all_history=True, token=self_token)
        await self.flush_pending_bundles()
        return True

    async def send_session(self, token=None):
        """Send session config to the mod: epoch and enrolled GUIDs.

        The mod uses this for validation gating (Validator.CurrentEpoch,
        Validator.IsEnrolled). Enrolled GUIDs come from the durable enrollment
        ledger (not slot_data). Empty means Defer for all.
        """
        if not self.can_deliver_unlocks(): return
        try:
            # Read from the enrollment ledger (list of dicts).
            enrollments = self.ledger.get("enrollments", [])
            enrolled = []
            for e in enrollments:
                guid = e.get("character_guid") if isinstance(e, dict) else None
                if guid is not None:
                    # GUID wire contract: signed integer (preserves negatives).
                    # Stored as string in ledger; convert to int for mod.
                    enrolled.append(int(guid))
        except (TypeError, ValueError, AttributeError):
            enrolled = []
        self_token = token if token is not None else self.connection_token
        msg = {
            "cmd": "session",
            "slot": self.args.slot,
            "connection_token": self_token,
            "unlock_protocol": 2,
            "epoch": self.ledger["epoch"],
            "enrolled_guids": enrolled,
            "multiplayer_mode": self.multiplayer_mode,
        }
        await self.send_mod(msg, token=token)
        print(f"[mod] session sent: epoch={self.ledger['epoch']}, "
              f"enrolled={len(enrolled)} guid(s)", flush=True)

    @staticmethod
    async def _close_ws(ws):
        try:
            await ws.close()
        except Exception:
            pass

    async def on_mod_message(self, msg, token=None):
        # token identifies the mod connection (None = primary). Threaded
        # through from mod_handler so acks and registrations bind to the
        # right shared-slot character.
        cmd = msg.get("cmd")
        if cmd == "connect":
            # Handshake already validated in try_handshake; a repeat is harmless.
            print("[mod] duplicate handshake ignored", flush=True)
        elif cmd == "check":
            if (not self._conn_validated(token) or type(msg.get('epoch')) is not int
                    or msg['epoch'] != self.ledger['epoch'] or msg.get('slot') != self.args.slot):
                return
            accepted = await self.report_check(msg.get("mission_id", -1),
                                    msg.get("mission_title", ""),
                                    msg.get("mission_class"))
            observation_id = msg.get('observation_id')
            if accepted and valid_text(observation_id):
                await self.send_mod({'cmd': 'observation_ack', 'epoch': self.ledger['epoch'],
                    'slot': self.args.slot, 'observation_id': observation_id,
                    'connection_token': token if token is not None else self.connection_token}, token=token)
        elif cmd == "grant_ack":
            await self.on_grant_ack(msg, token)
        elif cmd == "unlock_ack":
            await self.accept_unlock_ack(msg)
        elif cmd == "bundle_ack":
            await self._on_bundle_ack(msg)
        elif cmd == "identity_update":
            # Character load or switch: update identity binding.
            # Menu-first connect sends handshake without GUID; this provides it.
            # Character switch invalidates the old binding.
            #
            # Bound to current session: requires validated connection AND
            # matching epoch/slot. Stale queued updates are rejected.
            # Character switch does NOT erase durable entitlement/history.
            if not self._conn_validated(token):
                return
            # Slot/epoch binding: must match current session.
            if (type(msg.get('epoch')) is not int
                    or msg.get('epoch') != self.ledger['epoch']
                    or msg.get('slot') != self.args.slot):
                print(f"[mod] identity_update: stale (epoch/slot mismatch); "
                      f"rejected", flush=True)
                return
            guid = msg.get("character_guid")
            guid_str = self.normalize_guid(guid)
            if guid_str is None:
                print(f"[mod] identity_update: invalid guid {guid!r}", flush=True)
                return
            # Update connection GUID binding.
            old_guid = self._token_guid(token)
            self._set_conn_guid(token, guid_str)
            if old_guid and old_guid != guid_str:
                print(f"[mod] character switch: {old_guid} -> {guid_str} "
                      f"(durable history preserved)", flush=True)
            else:
                print(f"[mod] identity updated to {guid_str} (token {token})", flush=True)
            # Re-evaluate enrollment for the new identity.
            if self.multiplayer_mode == 1:
                if self.enroll_character(guid_str):
                    await self.flush_char_grants(guid_str, token)
            # Trigger session re-send so mod gets updated enrolled list.
            await self.send_session(token=token)
        elif cmd == "register_character":
            # Shared-slot late registration for connections that handshook
            # without a character_guid.
            if self.multiplayer_mode != 1:
                return
            if (not self._conn_validated(token) or type(msg.get('epoch')) is not int
                    or msg['epoch'] != self.ledger['epoch'] or msg.get('slot') != self.args.slot):
                return
            guid = msg.get("character_guid")
            guid_str = self.normalize_guid(guid)
            if guid_str is None:
                print("[mod] register_character: invalid guid", flush=True)
                return
            if self.enroll_character(guid_str):
                self._set_conn_guid(token, guid_str)
                await self.flush_char_grants(guid_str, token if token is not None else self.connection_token)
        elif cmd in ("boss_defeat", "age_advance", "objective_complete"):
            # Future event producers must attach the negotiated identity. No
            # event is acknowledged unless its durable commit succeeds.
            if not self.can_deliver_unlocks() or msg.get('epoch') != self.ledger['epoch'] or not self._valid_conn_token(msg.get('connection_token')) or msg.get('slot') != self.args.slot: return
            event_id = msg.get("event_id")
            if event_id is not None and not valid_text(event_id):
                print("[mod] world event with malformed event_id; ignoring", flush=True)
                return
            accepted = (self.record_boss_defeat(msg.get('boss_id'), event_id=event_id) if cmd=='boss_defeat' else
                        self.record_age_advance(msg.get('age_level'), event_id=event_id) if cmd=='age_advance' else
                        self.record_objective_complete(msg.get('objective_id'), event_id=event_id))
            if accepted:
                await self.send_mod({'cmd':'goal_event_ack','event_id':event_id,
                                     'epoch':self.ledger['epoch'],'connection_token':msg.get('connection_token')})
                # Separate slots: credit the world accomplishment to every
                # registered slot sharing this world (dedup by event_id).
                if self.multiplayer_mode == 0:
                    self.mp_broadcast(cmd, self._world_event_fields(cmd, msg),
                                      event_id=event_id)
                await self.check_goal_completion()
        else:
            print(f"[mod] unknown cmd: {cmd}",flush=True)

    def _conn_validated(self, token):
        if token is None or token == self.connection_token:
            return self.mod_validated
        conn = self.extra_mod_conns.get(token)
        return bool(conn and conn.get("validated"))

    def _set_conn_guid(self, token, guid):
        if token is None or token == self.connection_token:
            self.primary_guid = guid
        else:
            conn = self.extra_mod_conns.get(token)
            if conn is not None:
                conn["guid"] = guid

    @staticmethod
    def _world_event_fields(cmd, msg):
        if cmd == "boss_defeat":
            return {"boss_id": msg.get("boss_id")}
        if cmd == "age_advance":
            return {"age_level": msg.get("age_level")}
        return {"objective_id": msg.get("objective_id")}

    async def send_mod(self, msg, token=None):
        # token None -> broadcast to every validated mod connection
        # (primary plus shared-mode extras). Otherwise send to one.
        if token is None:
            targets = [ws for _t, c in self._iter_conns() for ws in (c["ws"],)]
        else:
            ws = self._token_ws(token)
            targets = [ws] if ws is not None else []
        for ws in targets:
            try:
                await ws.send(json.dumps(msg))
            except Exception as e:
                print(f"[mod] send failed: {e}", flush=True)

    async def run(self):
        if self.args.game_id_dump:
            # Debug mode: connect, print the slot_data mapping, exit.
            # No local mod server is started.
            await self.ap_loop()
            return
        server = await websockets.serve(self.mod_handler, "127.0.0.1",
                                        self.args.port)
        print(f"[mod] local bridge listening on 127.0.0.1:{self.args.port}",
              flush=True)
        await asyncio.gather(server.wait_closed(), self.ap_loop(), self.maintenance())


def main():
    ap = argparse.ArgumentParser(
        description="CraftopiaArchipelago bridge client (native Craftopia world)")
    ap.add_argument("--server", default="localhost:38281",
                    help="Archipelago server address (default localhost:38281)")
    ap.add_argument("--slot", default="Player1",
                    help="slot name (must match the YAML)")
    ap.add_argument("--password", default=None)
    ap.add_argument("--port", type=int, default=11341,
                    help="local bridge port for the game mod")
    ap.add_argument("--session", default="default",
                    help="delivery session name; the ledger file is per-session. "
                         "Use a NEW session (or --reset-ledger) when testing a "
                         "NEW seed.")
    ap.add_argument("--reset-ledger", action="store_true",
                    help="discard this session's previous delivery ledger and start "
                         "fresh with a new epoch (do this when testing a NEW seed)")
    ap.add_argument("--game-id-dump", action="store_true",
                    help="connect to the server, print the slot_data "
                         "game_id_to_location mapping, and exit (debugging)")
    ap.add_argument("--starter-choice", choices=COMBAT, help="Explicit starting combat choice when the slot uses player_choice")
    ap.add_argument("--takeover", action="store_true",
                    help="take over the single-AP-connection authority lock for "
                         "this session from another live bridge (use with care)")
    ap.add_argument("--recover-enrollment", action="store_true",
                    help="reviewed enrollment recovery: print this session's "
                         "enrollment state and, only after explicit typed "
                         "confirmation, clear enrollment history and release "
                         "any recovery hold")
    args = ap.parse_args()
    if args.recover_enrollment:
        run_recover_enrollment(args.session)
        return
    path = ledger_path(args.session)
    if args.reset_ledger and os.path.exists(path):
        os.remove(path)
        print("[AP] ledger reset by --reset-ledger", flush=True)
    print(f"bridge client v1.2.1: slot='{args.slot}' game='{GAME}' "
          f"server={args.server}"
          + (" [game-id-dump mode]" if args.game_id_dump else ""),
          flush=True)
    if HAVE_AP_NETUTILS:
        print("[AP] using official AP 0.6.8 NetUtils wire codec", flush=True)
    else:
        print("[AP] WARNING: vendored AP NetUtils not found; using stdlib json "
              "(protocol-compatible for our packets)", flush=True)
    authority_path = claim_authority(args.session, args.takeover)
    bridge = Bridge(args)
    bridge.authority_path = authority_path
    bridge.authority_last_beat = time.monotonic()

    def _release_authority():
        # Close the owning handle; never unlink the lock path. On POSIX an
        # unlink could leave contenders locking different inodes.
        global _authority_lock_file
        if _authority_lock_file is not None:
            _authority_lock_file.close()
            _authority_lock_file = None
    import atexit
    atexit.register(_release_authority)
    try:
        asyncio.run(bridge.run())
    except KeyboardInterrupt:
        print("bye", flush=True)


def _acquire_lock(lock_file):
    """Acquire exclusive lock at fixed position 0. Returns True on success.

    Windows locking is position-based: must seek to 0 before locking.
    A pre-existing append-mode file positions at EOF; without seek,
    contenders lock different bytes.
    """
    lock_file.seek(0)
    if sys.platform == "win32":
        import msvcrt
        try:
            msvcrt.locking(lock_file.fileno(), msvcrt.LK_NBLCK, 1)
            return True
        except OSError:
            return False
    else:
        import fcntl
        try:
            fcntl.flock(lock_file.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
            return True
        except OSError:
            return False

def claim_authority(session, takeover):
    """One bridge per session may hold the AP connection.

    A second bridge for the same --session refuses to start while the lock
    holder's pid is alive, unless --takeover is given. A stale lock (dead
    pid) is taken over automatically with a log line.
    """
    path = os.path.join(LEDGER_DIR,
                         f"craftopia_bridge_authority_{sanitize_session(session)}.json")
    # Platform-safe exclusive lock: held for process lifetime.
    # On Windows, msvcrt.locking() is used (os.kill is unsafe there).
    # On POSIX, fcntl.flock() is used.
    # If the process dies, the OS releases the lock automatically.
    lock_file = None
    try:
        lock_file = open(path, "a+b")
        locked = _acquire_lock(lock_file)
        if not locked and not takeover:
            print(f"[AP] AUTHORITY: another bridge holds the AP connection for "
                  f"session '{sanitize_session(session)}'. Run only one bridge "
                  f"per session; use --takeover to replace it.", flush=True)
            lock_file.close()
            sys.exit(2)
        if not locked and takeover:
            print(f"[AP] --takeover: attempting to replace live bridge", flush=True)
            lock_file.close()
            with open(path + ".takeover", "w") as tf:
                tf.write(str(os.getpid()))
            # Wait for old bridge to detect the marker and exit.
            time.sleep(2.0)
            # Reopen and ACTUALLY try to acquire the lock.
            lock_file = open(path, "a+b")
            locked = _acquire_lock(lock_file)
            if not locked:
                print(f"[AP] AUTHORITY: takeover failed; another bridge still holds the lock.",
                      flush=True)
                lock_file.close()
                sys.exit(2)
            print(f"[AP] --takeover: lock acquired", flush=True)
        # Write our PID for diagnostics (not for liveness checks).
        lock_file.seek(0)
        lock_file.truncate()
        lock_file.write(json.dumps({"pid": os.getpid(), "started": time.time()}).encode())
        lock_file.flush()
        # Keep lock_file open for process lifetime; store globally.
        global _authority_lock_file
        _authority_lock_file = lock_file
    except Exception as e:
        print(f"[AP] AUTHORITY: failed to acquire lock: {e}", flush=True)
        if lock_file:
            try: lock_file.close()
            except: pass
        sys.exit(2)
    # Create a new file handle for the JSON data (the lock file stays locked).
    with open(path + ".info", "w", encoding="utf-8") as f:
        json.dump({"pid": os.getpid(), "started": time.time(), "ts": time.time()}, f)
    return path


def run_recover_enrollment(session):
    """Reviewed enrollment recovery tool.

    Prints the session's enrollment state (characters, per-character grant
    accounting, team consumables, hold markers). Only after the operator
    types YES does it clear enrollment history and release the hold — and it
    keeps a backup. A missing or ambiguous history is never silently turned
    into a fresh character.
    """
    session = sanitize_session(session)
    path = ledger_path(session)
    print(f"=== enrollment recovery: session '{session}' ===", flush=True)
    print(f"ledger: {path}", flush=True)
    hold = os.path.exists(path + ".hold")
    print(f"recovery hold marker: {'PRESENT' if hold else 'absent'}", flush=True)
    stored, was_corrupt = load_ledger(path)
    if was_corrupt and not hold:
        print("ledger is corrupt/unreadable; original preserved as evidence.",
              flush=True)
    elif stored:
        enrollments = stored.get("enrollments", [])
        print(f"enrolled characters: {len(enrollments)}", flush=True)
        for e in enrollments:
            cg = stored.get("char_grants", {}).get(e["character_guid"], {})
            print(f"  guid={e['character_guid']} slot={e.get('slot')} "
                  f"enrolled_at={e.get('enrolled_at')} "
                  f"delivered={len(cg.get('delivered', []))} "
                  f"pending={len(cg.get('pending', []))}", flush=True)
        print(f"team consumables delivered: {len(stored.get('team_consumables', []))}",
              flush=True)
        print(f"pending team bundles: {len(stored.get('pending_bundles', []))}",
              flush=True)
        print(f"unassigned skill points: {len(stored.get('unassigned_points', []))}",
              flush=True)
    else:
        print("no ledger yet for this session.", flush=True)
    print(flush=True)
    try:
        ans = input("Type YES to clear enrollment history and release the hold "
                    "(a backup is kept first): ")
    except EOFError:
        ans = ""
    if ans.strip() != "YES":
        print("aborted; nothing changed.", flush=True)
        return
    if os.path.exists(path):
        bak = path + ".bak-%d" % int(time.time())
        with open(path, "rb") as f, open(bak, "wb") as g:
            g.write(f.read())
        print(f"backup written: {bak}", flush=True)
    if hold:
        os.remove(path + ".hold")
        print("hold marker removed.", flush=True)
    if stored and not was_corrupt:
        stored["enrollments"] = []
        stored["char_grants"] = {}
        stored["unassigned_points"] = []
        try:
            save_ledger(path, stored)
            print("enrollment history cleared.", flush=True)
        except OSError as e:
            print(f"ERROR writing ledger: {e}", flush=True)


if __name__ == "__main__":
    main()

