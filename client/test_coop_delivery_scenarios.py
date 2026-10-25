import copy
import json
from pathlib import Path
from unittest.mock import patch
import unittest
from coop_delivery import TeamDelivery
from test_coop_delivery import DeliveryFixture, TEAM, ROSTER, A, GUEST, POINTS, ORB, MATERIAL, BOW


class DeliveryScenarioTests(DeliveryFixture):
    def test_mixed_replay_and_restart_keep_team_and_personal_ownership(self):
        with self.open() as delivery:
            for receipt in (BOW, ORB, POINTS, MATERIAL): delivery.receive(receipt)
            delivery.claim(ORB.index, GUEST)
            orb = delivery.begin(ORB.index, GUEST)
            points = delivery.begin(POINTS.index, A)
            delivery.acknowledge(POINTS.index, A, points['attempt'], True)
            delivery.hold_dispatched(GUEST)
        with self.open() as delivery:
            for receipt in (POINTS, MATERIAL, ORB, BOW): self.assertFalse(delivery.receive(receipt))
            self.assertEqual(delivery.unlocks(A), [BOW])
            self.assertEqual(delivery.unlocks(GUEST), [BOW])
            self.assertEqual(delivery.available(), [MATERIAL])
            self.assertEqual(delivery.pending_points(A), [])
            self.assertEqual(delivery.pending_points(GUEST), [POINTS])
            self.assertEqual([h['receipt']['index'] for h in delivery.holds()], [ORB.index])
            self.assertIsNone(delivery.begin(ORB.index, GUEST))
            delivery.acknowledge(ORB.index, GUEST, orb['attempt'], True)
        # A different world's journal never inherits these records.
        from dataclasses import replace
        with TeamDelivery(Path(self.tmp.name) / 'other.json', replace(TEAM, world='B' * 64), ROSTER) as other:
            self.assertEqual(other.pending_points(A), [])
            self.assertEqual(other.available(), [])
            self.assertEqual(other.unlocks(A), [])

    def test_receive_failure_creates_no_partial_entitlements(self):
        with self.open() as delivery:
            before = self.path.read_bytes()
            with patch('coop_store.os.replace', side_effect=OSError('injected')):
                with self.assertRaises(OSError): delivery.receive(POINTS)
            self.assertEqual(self.path.read_bytes(), before)
            self.assertEqual(delivery.pending_points(A), [])
            self.assertEqual(delivery.pending_points(GUEST), [])
            self.assertTrue(delivery.receive(POINTS))
            self.assertEqual(delivery.pending_points(A), [POINTS])
            self.assertEqual(delivery.pending_points(GUEST), [POINTS])

    def test_failed_recovery_write_does_not_open_journal_or_drop_lock_forever(self):
        with self.open() as delivery:
            delivery.receive(ORB)
            delivery.claim(ORB.index, A)
            delivery.begin(ORB.index, A)
        before = self.path.read_bytes()
        with patch('coop_store.os.replace', side_effect=OSError('injected')):
            with self.assertRaises(OSError), self.open(): pass
        self.assertEqual(self.path.read_bytes(), before)
        with self.open() as delivery:
            self.assertEqual(len(delivery.holds()), 1)
            self.assertIsNone(delivery.begin(ORB.index, A))

    def test_point_begin_hold_and_ack_failures_preserve_committed_state(self):
        with self.open() as delivery:
            delivery.receive(POINTS)
            before = self.path.read_bytes()
            with patch('coop_store.os.fsync', side_effect=OSError('injected')):
                with self.assertRaises(OSError): delivery.begin(POINTS.index, A)
            self.assertEqual(self.path.read_bytes(), before)
            self.assertEqual(delivery.pending_points(A), [POINTS])
            envelope = delivery.begin(POINTS.index, A)
            before = self.path.read_bytes()
            with patch('coop_store.os.replace', side_effect=OSError('injected')):
                with self.assertRaises(OSError): delivery.hold_dispatched(A)
                with self.assertRaises(OSError): delivery.acknowledge(POINTS.index, A, envelope['attempt'], True)
            self.assertEqual(self.path.read_bytes(), before)
            self.assertEqual(delivery.holds(), [])
            self.assertIsNone(delivery.begin(POINTS.index, A))
        with self.open() as delivery:
            self.assertEqual(len(delivery.holds()), 1)

    def test_corrupt_receipts_or_grants_are_never_reset(self):
        with self.open() as delivery:
            delivery.receive(POINTS)
            delivery.receive(ORB)
            delivery.claim(ORB.index, A)
            delivery.begin(ORB.index, A)
        original = json.loads(self.path.read_text())
        corruptions = []
        def altered(change):
            state = copy.deepcopy(original)
            change(state)
            corruptions.append(json.dumps(state))
        altered(lambda s: s['receipts'].append(s['receipts'][0]))
        altered(lambda s: s['grants'].pop(0))
        altered(lambda s: s['grants'].append(s['grants'][0]))
        altered(lambda s: s['grants'][0].update(state='acknowledged', attempt=None))
        altered(lambda s: s['grants'][-1].update(character='999'))
        altered(lambda s: s['grants'][-1].update(state='unknown'))
        altered(lambda s: s['grants'][-1].update(attempt='invalid'))
        altered(lambda s: s['receipts'][0].update(index=True))
        corruptions.extend(['{', '{}', '{"format_version":1,"format_version":1}'])
        for content in corruptions:
            self.path.write_text(content)
            before = self.path.read_bytes()
            with self.subTest(content=content), self.assertRaises(ValueError), self.open(): pass
            self.assertEqual(self.path.read_bytes(), before)


if __name__ == '__main__': unittest.main()
