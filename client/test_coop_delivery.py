from dataclasses import replace
from pathlib import Path
import json
import os
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from coop_delivery import TeamDelivery
from coop_items import ItemReceipt
from coop_progress import Member
from test_coop_progress import IDENTITY, A, B


TEAM = replace(IDENTITY, mode=1)
GUEST = Member(B.character, A.slot)
ROSTER = (A, GUEST)
POINTS = ItemReceipt(10, 100, 'points', 'skill_points', 3)
ORB = ItemReceipt(11, 101, 'currency', '3840', 1)
MATERIAL = ItemReceipt(12, 102, 'physical', 'Iron', 10)
BOW = ItemReceipt(13, 103, 'unlock_equipment', 'Bow', 2)
SMELTING = ItemReceipt(14, 104, 'unlock_production', 'smelting', 1)


class DeliveryFixture(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.path = Path(self.tmp.name) / 'delivery.json'

    def open(self):
        return TeamDelivery(self.path, TEAM, ROSTER)


class DeliveryTests(DeliveryFixture):
    def test_replay_once_and_conflict_never_rewrites_receipt(self):
        with self.open() as delivery:
            for receipt in (POINTS, ORB):
                self.assertTrue(delivery.receive(receipt))
                before = self.path.read_bytes()
                self.assertFalse(delivery.receive(receipt))
                self.assertEqual(self.path.read_bytes(), before)
                with self.assertRaises(ValueError): delivery.receive(replace(receipt, amount=99))
                self.assertEqual(self.path.read_bytes(), before)
            self.assertEqual(delivery.available(), [ORB])
            self.assertEqual(delivery.pending_points(A), [POINTS])
            self.assertEqual(delivery.pending_points(GUEST), [POINTS])
        with self.open() as delivery:
            self.assertFalse(delivery.receive(POINTS))
            self.assertEqual(delivery.pending_points(GUEST), [POINTS])

    def test_sorted_shared_unlocks_and_returned_lists_are_independent(self):
        with self.open() as delivery:
            for receipt in (SMELTING, MATERIAL, BOW, ORB): delivery.receive(receipt)
            self.assertEqual(delivery.unlocks(A), [BOW, SMELTING])
            self.assertEqual(delivery.unlocks(GUEST), [BOW, SMELTING])
            delivery.available().clear()
            delivery.unlocks(A).clear()
            self.assertEqual(delivery.available(), [ORB, MATERIAL])
        with self.open() as delivery:
            self.assertEqual(delivery.unlocks(GUEST), [BOW, SMELTING])

    def test_invalid_mode_members_and_receipts_refused(self):
        for identity, roster in [(IDENTITY, ROSTER), (TEAM, ()), (TEAM, (A, B)),
                                 (TEAM, (A, A)), (TEAM, (Member('0', 1),))]:
            with self.assertRaises(ValueError), TeamDelivery(self.path, identity, roster): pass
        with self.open() as delivery:
            before = self.path.read_bytes()
            with self.assertRaises(ValueError): delivery.receive(replace(ORB, index=True))
            for read in (delivery.unlocks, delivery.pending_points):
                with self.assertRaises(ValueError): read(Member('999', 1))
            self.assertEqual(self.path.read_bytes(), before)

    def test_roster_or_identity_cannot_change_on_restart(self):
        with self.open() as delivery: delivery.receive(POINTS)
        before = self.path.read_bytes()
        for identity, roster in [(replace(TEAM, world='B' * 64), ROSTER),
                                 (replace(TEAM, seed='other'), ROSTER), (TEAM, (A,))]:
            with self.assertRaises(ValueError), TeamDelivery(self.path, identity, roster): pass
            self.assertEqual(self.path.read_bytes(), before)


class TeamClaimTests(DeliveryFixture):
    def test_claim_is_persistent_and_cannot_transfer(self):
        with self.open() as delivery:
            delivery.receive(ORB)
            with self.assertRaises(ValueError): delivery.begin(ORB.index, A)
            self.assertTrue(delivery.claim(ORB.index, A))
            self.assertFalse(delivery.claim(ORB.index, A))
            self.assertEqual(delivery.available(), [])
            with self.assertRaises(ValueError): delivery.claim(ORB.index, GUEST)
        with self.open() as delivery:
            self.assertFalse(delivery.claim(ORB.index, A))
            with self.assertRaises(ValueError): delivery.claim(ORB.index, GUEST)
            envelope = delivery.begin(ORB.index, A)
            self.assertEqual(envelope['character'], '123')
            self.assertEqual(envelope['receipt']['amount'], 1)
            saved = json.loads(self.path.read_text())['grants'][0]
            self.assertEqual(saved['state'], 'dispatched')
            self.assertEqual(saved['attempt'], envelope['attempt'])
            self.assertIsNone(delivery.begin(ORB.index, A))
            envelope['receipt']['amount'] = 99
            self.assertEqual(json.loads(self.path.read_text())['receipts'][0]['amount'], 1)

    def test_dispatched_restart_is_held_and_only_original_saved_ack_resolves(self):
        with self.open() as delivery:
            delivery.receive(MATERIAL)
            delivery.claim(MATERIAL.index, GUEST)
            envelope = delivery.begin(MATERIAL.index, GUEST)
        with self.open() as delivery:
            self.assertEqual(len(delivery.holds()), 1)
            self.assertEqual(delivery.holds()[0]['attempt'], envelope['attempt'])
            self.assertEqual(delivery.holds()[0]['character'], GUEST.character)
            self.assertIsNone(delivery.begin(MATERIAL.index, GUEST))
            self.assertEqual(delivery.available(), [])
            for member, attempt, saved in [(A, envelope['attempt'], True),
                                          (GUEST, '00000000-0000-0000-0000-000000000002', True),
                                          (GUEST, envelope['attempt'], False),
                                          (GUEST, envelope['attempt'], 1)]:
                before = self.path.read_bytes()
                with self.assertRaises(ValueError): delivery.acknowledge(MATERIAL.index, member, attempt, saved)
                self.assertEqual(self.path.read_bytes(), before)
            delivery.acknowledge(MATERIAL.index, GUEST, envelope['attempt'], True)
            self.assertEqual(delivery.holds(), [])
            before = self.path.read_bytes()
            delivery.acknowledge(MATERIAL.index, GUEST, envelope['attempt'], True)
            self.assertEqual(self.path.read_bytes(), before)
        with self.open() as delivery:
            self.assertEqual(delivery.holds(), [])
            self.assertIsNone(delivery.begin(MATERIAL.index, GUEST))
            self.assertFalse(delivery.receive(MATERIAL))
            with self.assertRaises(ValueError): delivery.claim(MATERIAL.index, A)

    def test_live_disconnect_holds_only_dispatched_items_for_that_character(self):
        with self.open() as delivery:
            for receipt, member in ((ORB, A), (MATERIAL, GUEST)):
                delivery.receive(receipt)
                delivery.claim(receipt.index, member)
                delivery.begin(receipt.index, member)
            delivery.hold_dispatched(A)
            self.assertEqual([h['receipt']['index'] for h in delivery.holds()], [11])
            delivery.holds().clear()
            delivery.hold_dispatched(A)
            self.assertEqual(len(delivery.holds()), 1)

    def test_unknown_indices_and_unlocks_cannot_be_granted(self):
        with self.open() as delivery:
            delivery.receive(BOW)
            for index in (True, -1, 999, BOW.index):
                with self.assertRaises(ValueError): delivery.claim(index, A)
                with self.assertRaises(ValueError): delivery.begin(index, A)
                with self.assertRaises(ValueError): delivery.acknowledge(index, A, 'invalid', True)
            with self.assertRaises(ValueError): delivery.hold_dispatched(Member('999', 1))

    def test_claim_and_dispatch_write_failure_never_publish_transition(self):
        with self.open() as delivery:
            delivery.receive(ORB)
            before = self.path.read_bytes()
            with patch('coop_store.os.replace', side_effect=OSError('failure')):
                with self.assertRaises(OSError): delivery.claim(ORB.index, A)
            self.assertEqual(self.path.read_bytes(), before)
            self.assertEqual(delivery.available(), [ORB])
            self.assertTrue(delivery.claim(ORB.index, GUEST))
            before = self.path.read_bytes()
            with patch('coop_store.os.fsync', side_effect=OSError('failure')):
                with self.assertRaises(OSError): delivery.begin(ORB.index, GUEST)
            self.assertEqual(self.path.read_bytes(), before)
            envelope = delivery.begin(ORB.index, GUEST)
            before = self.path.read_bytes()
            with patch('coop_store.os.replace', side_effect=OSError('failure')):
                with self.assertRaises(OSError): delivery.acknowledge(ORB.index, GUEST, envelope['attempt'], True)
            self.assertEqual(self.path.read_bytes(), before)
        with self.open() as delivery:
            self.assertEqual(len(delivery.holds()), 1)

    def test_second_process_cannot_race_or_steal_claim(self):
        script = '''
import sys
from pathlib import Path
from coop_delivery import TeamDelivery
from test_coop_delivery import TEAM, ROSTER, GUEST, ORB
try:
    with TeamDelivery(Path(sys.argv[1]), TEAM, ROSTER) as delivery:
        delivery.claim(ORB.index, GUEST)
except (OSError, ValueError) as error:
    print(type(error).__name__)
else:
    sys.exit('unexpected grant')
'''
        env = dict(os.environ, PYTHONPATH=str(Path(__file__).parent))
        def compete():
            return subprocess.run([sys.executable, '-c', script, str(self.path)], env=env,
                                  capture_output=True, text=True, timeout=10)
        with self.open() as delivery:
            delivery.receive(ORB)
            delivery.claim(ORB.index, A)
            result = compete()
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(result.stdout.strip(), ('PermissionError', 'BlockingIOError', 'OSError'))
        result = compete()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), 'ValueError')


if __name__ == '__main__': unittest.main()
