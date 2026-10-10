import unittest
from unittest.mock import patch
import test_delivery_repair as fixtures
run, cb = fixtures.run, fixtures.cb

class ProgressiveEquipment(unittest.TestCase):
    setUp = fixtures.DeliveryRepair.setUp
    tearDown = fixtures.DeliveryRepair.tearDown
    new = fixtures.DeliveryRepair.new
    connect = fixtures.DeliveryRepair.connect

    def receive(self, index=0, category='Sword'):
        self.b.item_id_to_name[40] = 'Progressive Equipment: ' + category
        run(self.b.process_received_items({'index': index, 'items': [{'item': 40}]}))

    def start(self):
        self.b.slot_data['starting_combat'] = 0
        self.connect()

    def test_distinct_receipts_advance_and_replay_does_not(self):
        self.start(); self.receive(); self.receive(1)
        records = [r for r in self.b.ledger['unlocks'] if r['receipt_id'] >= 0]
        self.assertEqual([r['tier'] for r in records], [2, 3])
        self.b = self.new(); self.receive(); self.receive(1)
        self.assertEqual([r for r in self.b.ledger['unlocks'] if r['receipt_id'] >= 0], records)

    def test_other_category_starts_at_one(self):
        self.start(); self.receive(category='Bow')
        self.assertEqual(self.b.ledger['unlocks'][-1]['tier'], 1)
        self.assertEqual(self.b.ledger['unlocks'][-1]['category'], 'Bow')

    def test_write_failure_retries_same_tier(self):
        self.start()
        with patch.object(cb, 'save_ledger', side_effect=OSError('disk full')):
            self.receive()
        self.assertNotIn(0, self.b.seen_non_grant)
        self.receive()
        self.assertEqual(self.b.ledger['unlocks'][-1]['tier'], 2)

    def test_waits_for_starter_choice_before_assigning_tiers(self):
        self.receive()
        self.assertFalse(self.b.ledger['unlocks'])
        self.assertNotIn(0, self.b.seen_non_grant)
        self.start(); self.receive()
        self.assertEqual(self.b.ledger['unlocks'][-1]['tier'], 2)

    def test_reused_receipt_cannot_change_category(self):
        self.start(); self.receive()
        self.b = self.new(); self.receive(category='Bow')
        self.assertTrue(self.b.recovery_hold)

    def test_tier_cap_keeps_every_receipt_without_exceeding_ten(self):
        self.start()
        for index in range(12): self.receive(index)
        records = [r for r in self.b.ledger['unlocks'] if r['receipt_id'] >= 0]
        self.assertEqual(len(records), 12)
        self.assertEqual(max(r['tier'] for r in records), 10)

    def test_batch_retry_after_one_failed_commit_counts_each_receipt_once(self):
        self.start(); self.b.item_id_to_name[40] = 'Progressive Equipment: Sword'
        save = cb.save_ledger
        calls = 0
        def fail_first(*args, **kwargs):
            nonlocal calls
            calls += 1
            if calls == 1: raise OSError('disk full')
            return save(*args, **kwargs)
        msg = {'index': 0, 'items': [{'item': 40}, {'item': 40}]}
        with patch.object(cb, 'save_ledger', side_effect=fail_first):
            run(self.b.process_received_items(msg))
        run(self.b.process_received_items(msg))
        records = [r for r in self.b.ledger['unlocks'] if r['receipt_id'] >= 0]
        self.assertEqual(sorted(r['receipt_id'] for r in records), [0, 1])
        self.assertEqual(sorted(r['tier'] for r in records), [2, 3])

if __name__ == '__main__': unittest.main()
