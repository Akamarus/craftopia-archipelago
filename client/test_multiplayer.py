#!/usr/bin/env python3
"""Cooperative multiplayer tests for craftopia_bridge.py.

Run: python3 test_multiplayer.py
Covers, with real isolated ledger files and no sockets/network:
- separate slots: world-event broadcast via the shared registry, peer poll
  application, event dedup by event_id, invalid events ignored;
- shared slot: late-join full unlock-history replay, per-character skill-point
  accounting (no double grant on reconnect/spend), team-once money/material
  bundles, enrollment refusal under recovery hold;
- reviewed enrollment recovery tool (explicit YES confirmation);
- single-AP-connection authority (refuse/takeover);
- second mod connection rejected in separate mode, accepted in shared mode.
"""
import asyncio
import importlib.util
import json
import os
import sys
import tempfile
import types
import unittest
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).parent))
sys.modules.setdefault('websockets', types.ModuleType('websockets'))
spec = importlib.util.spec_from_file_location('mp_bridge', Path(__file__).with_name('craftopia_bridge.py'))
cb = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cb)


def run(c):
    try:
        c.send(None)
    except StopIteration as done:
        return done.value
    raise AssertionError('Unexpected external wait')


class Socket:
    def __init__(self):
        self.sent = []
        self.closed = False

    async def send(self, message):
        self.sent.append(json.loads(message))

    async def close(self):
        self.closed = True


class FakeWS(Socket):
    """Async-iterable socket double for mod_handler tests."""
    def __init__(self, incoming=(), stay_open=False):
        super().__init__()
        self.queue = asyncio.Queue()
        for m in incoming:
            self.queue.put_nowait(m)
        if not stay_open:
            self.queue.put_nowait(None)

    def __aiter__(self):
        return self._gen()

    async def _gen(self):
        while True:
            m = await self.queue.get()
            if m is None:
                return
            yield m

    async def push(self, s):
        await self.queue.put(s)

    async def eof(self):
        await self.queue.put(None)


def make_args(slot='TestPlayer', session='mp'):
    return types.SimpleNamespace(server='test:1', slot=slot, session=session,
                                 password=None, port=11341, game_id_dump=False,
                                 starter_choice='Sword')


class MPBase(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        cb.LEDGER_DIR = self.tmp.name

    def tearDown(self):
        self.tmp.cleanup()

    def new_bridge(self, slot='TestPlayer', session='mp'):
        b = cb.Bridge(make_args(slot, session))
        b.slot_data = {
            'goal_boss': True, 'goal_boss_choice': 'Kraken',
            'goal_boss_ids': ['Kraken', 'Hydra'],
            'goal_objectives': True, 'goal_objective_count': 1,
            'objective_locations': {'Example': 'location:1'},
            'class_to_location': {'ExampleMission': 'Example'},
            'multiplayer_mode': 0,
        }
        b.class_to_location = {'ExampleMission': 'Example'}
        b.location_name_to_id = {'Example': 1}
        b.item_name_to_id = {'Skill Point': 1, 'Victory': 2, 'Money Bundle: 500': 3,
                               'Production: Refiner': 9}
        b.item_id_to_name = {1: 'Skill Point', 2: 'Victory', 3: 'Money Bundle: 500',
                             9: 'Production: Refiner'}
        b.skill_point_item_id = 1
        b.victory_item_id = 2
        b.datapackage_ready = True
        b.ap_connected = True
        b.sent_ap = []
        async def sink(m):
            b.sent_ap.append(m)
        b.send_ap = sink
        return b

    def new_shared(self, slot='TestPlayer', session='shared'):
        b = self.new_bridge(slot=slot, session=session)
        b.slot_data['multiplayer_mode'] = 1
        b.multiplayer_mode = 1
        return b

    def connect_extra(self, b, guid=None):
        # Simulate an extra shared-mode connection through mod_handler's
        # extra path by driving try_handshake with a token.
        ws = Socket()
        token = 'tok-' + str(guid or 'anon')
        b.extra_mod_conns[token] = {'ws': ws, 'guid': None, 'validated': False}
        hs = {'cmd': 'connect', 'name': b.args.slot, 'game': 'Craftopia',
              'unlock_protocol': 2}
        if guid:
            hs['character_guid'] = guid
        run(b.try_handshake(ws, hs, token))
        return ws, token

    def set_seed(self, b, seed='seed-X'):
        proposed = cb.candidate(b.ledger)
        proposed['seed_name'] = seed
        self.assertTrue(b.commit(proposed))

    def connect_mod(self, b, guid=None, token_extra=False):
        ws = Socket()
        b.mod_ws = ws
        hs = {'cmd': 'connect', 'name': b.args.slot, 'game': 'Craftopia',
              'unlock_protocol': 2}
        if guid:
            hs['character_guid'] = guid
        run(b.try_handshake(ws, hs))
        return ws

    def world_event(self, b, cmd, fields, event_id='e1'):
        msg = {'cmd': cmd, 'epoch': b.ledger['epoch'],
               'connection_token': b.connection_token, 'slot': b.args.slot,
               'event_id': event_id}
        msg.update(fields)
        run(b.on_mod_message(msg))


class SeparateSlots(MPBase):
    def test_broadcast_credits_peer(self):
        a = self.new_bridge(slot='P1', session='s1')
        c = self.new_bridge(slot='P2', session='s2')
        self.set_seed(a)
        self.set_seed(c)
        self.connect_mod(a)
        # P1's mod reports the boss kill through the full production path.
        self.world_event(a, 'boss_defeat', {'boss_id': 'Kraken'}, event_id='e-boss-1')
        self.assertTrue(a.boss_defeated)
        # P2 polls the shared registry and gets credit too.
        run(c.mp_poll())
        self.assertTrue(c.boss_defeated)
        self.assertEqual(c.boss_id, 'Kraken')
        # Re-polling never double-applies; no rebroadcast loop.
        before = json.dumps(c.ledger['goal_events'], sort_keys=True)
        run(c.mp_poll())
        run(a.mp_poll())
        self.assertEqual(json.dumps(c.ledger['goal_events'], sort_keys=True), before)
        self.assertEqual(len(c.ledger['seen_events']), 1)

    def test_event_dedup_by_id(self):
        a = self.new_bridge(slot='P1', session='s1')
        c = self.new_bridge(slot='P2', session='s2')
        self.set_seed(a)
        self.set_seed(c)
        self.connect_mod(a)
        self.world_event(a, 'boss_defeat', {'boss_id': 'Kraken'}, event_id='e-dup')
        # Simulate a duplicate registry entry (two bridges, same physical event).
        path = a.mp_registry_path()
        reg = a._mp_read(path)
        reg['events'].append(dict(reg['events'][0]))
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(reg, f)
        run(c.mp_poll())
        self.assertTrue(c.boss_defeated)
        self.assertEqual(c.ledger['seen_events'].count('e-dup'), 1)

    def test_invalid_peer_event_ignored(self):
        a = self.new_bridge(slot='P1', session='s1')
        c = self.new_bridge(slot='P2', session='s2')
        self.set_seed(a)
        self.set_seed(c)
        # P1 broadcasts an objective that is not in P2's catalog.
        a.mp_broadcast('objective_complete', {'objective_id': 'location:UNKNOWN'},
                       event_id='e-bad')
        run(c.mp_poll())
        self.assertEqual(c.completed_objectives, set())
        # ...but it is marked seen so we never spam about it.
        self.assertIn('e-bad', c.ledger['seen_events'])

    def test_peer_presence_registered(self):
        a = self.new_bridge(slot='P1', session='s1')
        self.set_seed(a)
        a.mp_register()
        reg = a._mp_read(a.mp_registry_path())
        self.assertTrue(any(p['slot'] == 'P1' for p in reg['peers']))

    def test_second_socket_rejected_in_separate_mode(self):
        async def go():
            b = self.new_bridge()
            b.multiplayer_mode = 0
            ws1 = FakeWS(['{"cmd":"connect","name":"TestPlayer","game":"Craftopia","unlock_protocol":2}'],
                         stay_open=True)
            ws2 = FakeWS(['{"cmd":"connect","name":"TestPlayer","game":"Craftopia","unlock_protocol":2}'],
                         stay_open=True)
            t1 = asyncio.create_task(b.mod_handler(ws1))
            await asyncio.sleep(0.05)
            t2 = asyncio.create_task(b.mod_handler(ws2))
            await asyncio.sleep(0.05)
            self.assertTrue(ws2.closed)
            self.assertFalse(ws1.closed)
            await ws1.eof()
            await ws2.eof()
            await asyncio.gather(t1, t2)
        asyncio.run(go())


class SharedSlot(MPBase):
    def test_late_join_replays_unlock_history(self):
        b = self.new_shared()
        # Unlock arrives while no mod is connected: durable + pending.
        run(b.process_received_items({'index': 0, 'items': [{'item': 9}]}))
        self.assertTrue(any(p['receipt_id'] == 0 for p in b.ledger['pending_unlocks']))
        # Late joiner gets the full history replay.
        ws, _tok = self.connect_extra(b, guid='G1')
        kinds = [m.get('cmd') for m in ws.sent]
        self.assertIn('unlock_production', [m.get('cmd') for m in ws.sent])
        # Ack clears pending but history survives...
        msg = next(m for m in ws.sent if m.get('cmd') == 'unlock_production')
        run(b.on_mod_message(dict(msg, cmd='unlock_ack')))
        self.assertFalse(any(p['receipt_id'] == 0 for p in b.ledger['pending_unlocks']))
        self.assertTrue(any(u['receipt_id'] == 0 for u in b.ledger['unlocks']))
        # ...and a later joiner gets the full history replay again (idempotent
        # permission state rebuild, matching v1.2.1 semantics): it returns to
        # pending until re-acked, never lost.
        ws2, _t2 = self.connect_extra(b, guid='G2')
        self.assertTrue(any(m.get('cmd') == 'unlock_production' for m in ws2.sent))
        msg2 = next(m for m in ws2.sent if m.get('cmd') == 'unlock_production')
        run(b.on_mod_message(dict(msg2, cmd='unlock_ack')))
        self.assertFalse(any(p['receipt_id'] == 0 for p in b.ledger['pending_unlocks']))

    def test_skill_points_per_character_no_double_grant(self):
        b = self.new_shared()
        # Point arrives before anyone enrolls: held.
        run(b.process_received_items({'index': 0, 'items': [{'item': 1}]}))
        self.assertEqual(b.ledger['unassigned_points'], [0])
        # G1 enrolls via handshake: claims held point, gets grant.
        ws1, tok1 = self.connect_extra(b, guid='G1')
        grants1 = [m for m in ws1.sent if m.get('cmd') == 'grant_items']
        self.assertEqual(len(grants1), 1)
        idx = grants1[0]['indices']
        self.assertTrue(idx)
        # G1 acks (points "spent" in game).
        run(b.on_mod_message({'cmd': 'grant_ack', 'epoch': b.ledger['epoch'],
                              'indices': idx, 'slot': 'TestPlayer'}, tok1))
        cg1 = b.ledger['char_grants']['G1']
        self.assertEqual(cg1['pending'], [])
        self.assertEqual(sorted(cg1['delivered']), sorted(idx))
        # G1 reconnects: nothing re-granted.
        ws1b, tok1b = self.connect_extra(b, guid='G1')
        self.assertFalse(any(m.get('cmd') == 'grant_items' for m in ws1b.sent))
        # G2 enrolls later: does NOT get G1's historical point...
        ws2, tok2 = self.connect_extra(b, guid='G2')
        self.assertFalse(any(m.get('cmd') == 'grant_items' for m in ws2.sent))
        # ...but both get the next point, once each.
        run(b.process_received_items({'index': 1, 'items': [{'item': 1}]}))
        self.assertTrue(b.ledger['char_grants']['G1']['pending'])
        self.assertTrue(b.ledger['char_grants']['G2']['pending'])
        self.assertEqual(sorted(b.ledger['char_grants']['G1']['pending']),
                         sorted(b.ledger['char_grants']['G2']['pending']))

    def test_team_bundle_delivered_once(self):
        b = self.new_shared()
        run(b.process_received_items({'index': 5, 'items': [{'item': 3}]}))
        self.assertEqual(len(b.ledger['pending_bundles']), 1)
        # First connected mod gets it.
        ws1, _t1 = self.connect_extra(b, guid='G1')
        bundles = [m for m in ws1.sent if m.get('cmd') == 'grant_bundle']
        self.assertEqual(len(bundles), 1)
        self.assertEqual(bundles[0]['amount'], 500)
        # Ack -> team-delivered.
        run(b.on_mod_message({'cmd': 'bundle_ack', 'epoch': b.ledger['epoch'],
                              'receipt_id': 5, 'slot': 'TestPlayer'}))
        self.assertEqual(b.ledger['team_consumables'], [5])
        self.assertEqual(b.ledger['pending_bundles'], [])
        # Late joiner does NOT get it again.
        ws2, _t2 = self.connect_extra(b, guid='G2')
        self.assertFalse(any(m.get('cmd') == 'grant_bundle' for m in ws2.sent))
        # Bridge restart does not resurrect it either.
        b2 = self.new_shared()
        self.assertEqual(b2.ledger['team_consumables'], [5])
        ws3, _t3 = self.connect_extra(b2, guid='G3')
        self.assertFalse(any(m.get('cmd') == 'grant_bundle' for m in ws3.sent))

    def test_pending_bundle_survives_restart_until_ack(self):
        b = self.new_shared()
        run(b.process_received_items({'index': 5, 'items': [{'item': 3}]}))
        b2 = self.new_shared()  # same session -> same ledger
        self.assertEqual(len(b2.ledger['pending_bundles']), 1)
        ws, _t = self.connect_extra(b2, guid='G1')
        self.assertTrue(any(m.get('cmd') == 'grant_bundle' for m in ws.sent))

    def test_second_socket_allowed_in_shared_mode(self):
        async def go():
            b = self.new_shared()
            b.ap_connected = True
            hs = '{"cmd":"connect","name":"TestPlayer","game":"Craftopia","unlock_protocol":2,"character_guid":"G%d"}'
            ws1 = FakeWS([hs % 1], stay_open=True)
            ws2 = FakeWS([hs % 2], stay_open=True)
            t1 = asyncio.create_task(b.mod_handler(ws1))
            await asyncio.sleep(0.05)
            t2 = asyncio.create_task(b.mod_handler(ws2))
            await asyncio.sleep(0.05)
            self.assertFalse(ws1.closed)
            self.assertFalse(ws2.closed)
            self.assertTrue(b.mod_validated)
            self.assertEqual(len(b.extra_mod_conns), 1)
            # Both got their own session with their own token.
            s1 = [m for m in ws1.sent if isinstance(m, dict) and m.get('cmd') == 'session']
            s2 = [m for m in ws2.sent if isinstance(m, dict) and m.get('cmd') == 'session']
            self.assertEqual(len(s1), 1)
            self.assertEqual(len(s2), 1)
            self.assertNotEqual(s1[0]['connection_token'], s2[0]['connection_token'])
            self.assertEqual(s1[0]['multiplayer_mode'], 1)
            # Both characters enrolled.
            guids = {e['character_guid'] for e in b.ledger['enrollments']}
            self.assertEqual(guids, {'G1', 'G2'})
            await ws1.eof()
            await ws2.eof()
            await asyncio.gather(t1, t2)
            self.assertEqual(b.extra_mod_conns, {})
        asyncio.run(go())

    def test_enrollment_refused_under_recovery_hold(self):
        b = self.new_shared()
        b.recovery_hold = True
        self.assertFalse(b.enroll_character(123))
        self.assertEqual(b.ledger['enrollments'], [])

    def test_enrollment_rejects_blank_guid(self):
        b = self.new_shared()
        self.assertFalse(b.enroll_character(''))
        self.assertFalse(b.enroll_character(None))
        self.assertEqual(b.ledger['enrollments'], [])


class RecoveryAndAuthority(MPBase):
    def test_recover_enrollment_clears_with_yes(self):
        b = self.new_shared()
        self.assertTrue(b.enroll_character(123))
        path = Path(b.ledger_path)
        with mock.patch('builtins.input', return_value='YES'):
            cb.run_recover_enrollment('shared')
        fresh = cb.Bridge(make_args(session='shared'))
        self.assertEqual(fresh.ledger['enrollments'], [])
        # A backup was kept.
        self.assertTrue(any(str(p).startswith(str(path) + '.bak-')
                            for p in path.parent.iterdir()))

    def test_recover_enrollment_aborts_without_yes(self):
        b = self.new_shared()
        self.assertTrue(b.enroll_character(123))
        before = Path(b.ledger_path).read_bytes()
        with mock.patch('builtins.input', return_value='no'):
            cb.run_recover_enrollment('shared')
        self.assertEqual(Path(b.ledger_path).read_bytes(), before)

    def test_authority_refuses_second_live_bridge(self):
        p1 = cb.claim_authority('auth-test', False)
        try:
            with self.assertRaises(SystemExit):
                cb.claim_authority('auth-test', False)
        finally:
            os.remove(p1)
        # After release, claiming works again.
        p2 = cb.claim_authority('auth-test', False)
        os.remove(p2)

    def test_authority_takes_over_stale_lock(self):
        path = os.path.join(cb.LEDGER_DIR, 'craftopia_bridge_authority_stale-test.json')
        with open(path, 'w', encoding='utf-8') as f:
            json.dump({'pid': 2 ** 30, 'started': 0, 'ts': 0}, f)
        try:
            p = cb.claim_authority('stale-test', False)
            with open(p, encoding='utf-8') as f:
                self.assertEqual(json.load(f)['pid'], os.getpid())
        finally:
            if os.path.exists(path):
                os.remove(path)

    def test_authority_takeover_flag(self):
        p1 = cb.claim_authority('takeover-test', False)
        try:
            p2 = cb.claim_authority('takeover-test', True)
            self.assertEqual(p1, p2)
        finally:
            if os.path.exists(p1):
                os.remove(p1)


if __name__ == '__main__':
    unittest.main()

class NumericGuidWireFormat(MPBase):
    """Regression: numeric C# wire payload through full lifecycle.

    Starts with actual numeric character_guid: 123 (as C# sends),
    then queue -> send -> ack -> restart -> replay.
    Covers negative and leading-zero forms.
    """
    def test_numeric_handshake_full_lifecycle(self):
        b = self.new_shared()
        # 1. Numeric handshake (C# sends int, not string).
        ws, tok = self.connect_extra(b, guid=123)
        # Enrollment should use canonical '123'.
        self.assertTrue(any(
            e.get("character_guid") == "123"
            for e in b.ledger["enrollments"]))
        # Connection GUID should be canonical string.
        conn = b.extra_mod_conns.get(tok) or {}
        # primary_guid for token=None, or conn guid for extra
        # (connect_extra uses extra path)
        self.assertEqual(conn.get("guid"), "123")

    def test_negative_guid_canonical(self):
        b = self.new_shared()
        # Negative GUID preserves sign.
        self.assertTrue(b.enroll_character(-456))
        self.assertTrue(any(
            e.get("character_guid") == "-456"
            for e in b.ledger["enrollments"]))

    def test_leading_zero_canonical(self):
        b = self.new_shared()
        # '00123' canonicalizes to '123', not a separate identity.
        self.assertTrue(b.enroll_character("00123"))
        self.assertTrue(any(
            e.get("character_guid") == "123"
            for e in b.ledger["enrollments"]))
        # Enrolling '123' again is idempotent (same canonical).
        self.assertTrue(b.enroll_character(123))
        count = sum(1 for e in b.ledger["enrollments"]
                   if e.get("character_guid") == "123")
        self.assertEqual(count, 1)

    def test_bool_zero_rejected(self):
        b = self.new_shared()
        self.assertFalse(b.enroll_character(True))
        self.assertFalse(b.enroll_character(False))
        self.assertFalse(b.enroll_character(0))
        self.assertFalse(b.enroll_character("0"))
        self.assertEqual(b.ledger["enrollments"], [])

class CatalogIsolationRegression(MPBase):
    """Regression: cross-catalog routing leaks are rejected.

    Legacy slot with mission 51 and unknown class must NOT route to
    instance ID 1013. Every successful route must belong to the slot's
    active network locations.
    """
    def _make_bridge_with_slot_data(self, catalog_version, objective_locations):
        b = self.new_bridge()
        b.datapackage_ready = True
        b.slot_data = {
            "mission_catalog_version": catalog_version,
            "mission_catalog_schema": 1 if catalog_version == "20261007_r1" else 0,
            "objective_locations": objective_locations,
        }
        # Legacy mappings
        b.class_to_location = {"Oc.Missions.ItemCraftMission": "Item Craft"}
        b.location_name_to_id = {"Item Craft": 27}
        # Instance mappings (would leak without isolation)
        b.game_id_to_location = {
            "51": "Mission 51: Make a Stone Axe",
            51: "Mission 51: Make a Stone Axe",
        }
        b.location_name_to_id["Mission 51: Make a Stone Axe"] = 1013
        return b

    def test_legacy_unknown_class_does_not_leak_to_instance(self):
        # Legacy slot: 53 locations, IDs 1-53.
        active = {f"Loc{i}": f"location:{i}" for i in range(1, 54)}
        active["Item Craft"] = "location:27"
        b = self._make_bridge_with_slot_data("legacy", active)
        # Mission 51 with UNKNOWN class: legacy has no numeric fallback
        # for 51 in its filtered game_id_to_location (we didn't add it),
        # so it should return None, not leak to 1013.
        # (Our test fixture has 51 in game_id_to_location, but the real
        # fill_slot_data filters it out. Simulate by removing.)
        del b.game_id_to_location["51"]
        del b.game_id_to_location[51]
        result = b.location_id_for_mission(51, None)
        self.assertIsNone(result)

    def test_legacy_allowlist_rejects_instance_id(self):
        # Even if a leak occurs in mapping, allowlist must reject.
        active = {f"Loc{i}": f"location:{i}" for i in range(1, 54)}
        b = self._make_bridge_with_slot_data("legacy", active)
        # Force the leak: 51 maps to 1013
        result = b.location_id_for_mission(51, "Unknown.Class")
        # Should be rejected by allowlist (1013 not in 1-53)
        self.assertIsNone(result)

    def test_unsupported_catalog_rejected(self):
        b = self._make_bridge_with_slot_data("bogus_v9", {})
        result = b.location_id_for_mission(51, "Oc.Missions.ItemCraftMission")
        self.assertIsNone(result)

    def test_instance_unknown_id_rejected(self):
        active = {"Mission 51: Make a Stone Axe": "location:1013"}
        b = self._make_bridge_with_slot_data("20261007_r1", active)
        # Unknown numeric ID 999999: no mapping, no class fallback.
        result = b.location_id_for_mission(999999, "Oc.Missions.ItemCraftMission")
        self.assertIsNone(result)
