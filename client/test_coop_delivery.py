from dataclasses import replace
from pathlib import Path
import tempfile
import unittest
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


class DeliveryTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.path = Path(self.tmp.name) / 'delivery.json'

    def open(self):
        return TeamDelivery(self.path, TEAM, ROSTER)

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


if __name__ == '__main__': unittest.main()
