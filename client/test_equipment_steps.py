from test_progressive_equipment import ProgressiveEquipment


class EquipmentSteps(ProgressiveEquipment):
    def test_sparse_tiers_and_retry(self):
        self.start()
        self.b.slot_data['equipment_tier_steps'] = {'Gun': [3, 4, 5, 6, 7, 10]}
        self.receive(0, 'Gun'); self.receive(1, 'Gun'); self.receive(1, 'Gun')
        records = [r for r in self.b.ledger['unlocks'] if r['receipt_id'] >= 0]
        self.assertEqual([r['tier'] for r in records], [3, 4])

    def test_malformed_steps_fail_closed(self):
        self.start()
        for steps in ([True], [3, 2], [], [11], [1, 1]):
            self.b.slot_data['equipment_tier_steps'] = {'Gun': steps}
            self.assertIsNone(self.b.progressive_equipment('Gun', 0))
