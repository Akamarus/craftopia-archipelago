import unittest
import test_observation_ack as fixtures
run = fixtures.run

class ActivityHistory(unittest.TestCase):
    setUp = fixtures.ObservationAck.setUp
    tearDown = fixtures.ObservationAck.tearDown
    new = fixtures.ObservationAck.new
    connect = fixtures.ObservationAck.connect
    message = fixtures.ObservationAck.message

    def test_progressive_result_and_seed_total(self):
        self.b.total_check_count = 564
        self.b.checked_locations = {1, 2, 3, 4, 5}
        self.b.received_activity = {2: 'Progressive Equipment: Bow'}
        self.b.ledger['unlocks'] = [{'receipt_id': 2, 'cmd': 'unlock_equipment', 'category': 'Bow', 'tier': 2, 'progressive': True}]
        payload = self.b.activity_payload()
        self.assertEqual(payload['received'], ['Bow \u2192 Tier 2'])
        self.assertEqual((payload['checked_count'], payload['total_check_count']), (5, 564))

    def test_currency_history_distinguishes_pending_from_delivered(self):
        self.b.received_activity = {1: 'Soul Orb Currency'}
        self.b.ledger['currency_receipts'] = [{'receipt_id': 1, 'amount': 1, 'character_guid': '123', 'acknowledged': False}]
        self.assertEqual(self.b.activity_payload()['received'], ['Soul Orb x1 - delivery pending'])
        self.b.ledger['currency_receipts'][0]['acknowledged'] = True
        self.assertEqual(self.b.activity_payload()['received'], ['Soul Orb x1 - delivered'])

    def test_pending_is_not_sent_and_snapshot_is_bound(self):
        sock = self.connect()
        run(self.b.on_mod_message(self.message()))
        run(self.b.send_activity())
        snapshot = [m for m in sock.sent if m.get('cmd') == 'activity_snapshot'][-1]
        self.assertEqual(snapshot['sent'], [])
        self.assertEqual(snapshot['connection_token'], self.b.connection_token)
        self.assertEqual(snapshot['epoch'], self.b.ledger['epoch'])
        run(self.b.on_room_update({'checked_locations': [1]}))
        snapshot = [m for m in sock.sent if m.get('cmd') == 'activity_snapshot'][-1]
        self.assertEqual(len(snapshot['sent']), 1)

    def test_received_replay_is_bounded_and_idempotent(self):
        self.b.item_id_to_name = {1: 'Skill Point'}
        msg = {'index': 0, 'items': [[1, 1, 1, 0]] * 100}
        self.b.record_received_activity(msg)
        self.b.record_received_activity(msg)
        self.assertEqual(len(self.b.received_activity), 100)
        self.assertEqual(min(self.b.received_activity), 0)
        self.assertEqual(self.b.activity_payload()['received'], ['Skill Point'] * 100)

    def test_large_history_packet_is_bounded_and_discloses_truncation(self):
        import json
        self.b.item_id_to_name = {1: '\u2603' * 100}
        self.b.record_received_activity({'index': 0, 'items': [[1, 1, 1, 0]] * 1200})
        payload = self.b.activity_payload()
        self.assertLess(len(json.dumps(payload).encode('utf-8')), 49000)
        self.assertTrue(payload['history_limited'])

    def test_reconnect_restores_from_server_checks_and_received_items(self):
        self.b.checked_locations = {1}
        self.b.item_id_to_name = {1: 'Skill Point'}
        self.b.record_received_activity({'index': 0, 'items': [[1, 1, 1, 0]]})
        sock = self.connect()
        snapshots = [m for m in sock.sent if m.get('cmd') == 'activity_snapshot']
        self.assertTrue(snapshots)
        self.assertEqual(len(snapshots[-1]['sent']), 1)
        self.assertEqual(snapshots[-1]['received'], ['Skill Point'])

    def test_actual_received_path_and_cosmetic_failure_preserve_delivery(self):
        sock = self.connect()
        run(self.b.on_received_items({'index': 0, 'items': [{'item': 3}]}))
        snapshots = [m for m in sock.sent if m.get('cmd') == 'activity_snapshot']
        self.assertEqual(snapshots[-1]['received'], ['Production: Refiner'])
        original = self.b.send_mod
        async def fail_snapshot(msg, token=None):
            if msg.get('cmd') == 'activity_snapshot':
                raise OSError('cosmetic send failed')
            await original(msg, token=token)
        self.b.send_mod = fail_snapshot
        run(self.b.on_received_items({'index': 1, 'items': [{'item': 3}]}))
        self.assertTrue(any(u['receipt_id'] == 1 for u in self.b.ledger['unlocks']))

    def test_deferred_datapackage_publishes_named_history(self):
        sock = self.connect()
        self.b.datapackage_ready = False
        run(self.b.on_received_items({'index': 0, 'items': [{'item': 3}]}))
        self.assertFalse(self.b.received_activity)
        run(self.b.on_datapackage({'data': {'games': {'Craftopia': {
            'location_name_to_id': {'Example': 1},
            'item_name_to_id': {'Skill Point': 1, 'Production: Refiner': 3}}}}}))
        snapshots = [m for m in sock.sent if m.get('cmd') == 'activity_snapshot']
        self.assertEqual(snapshots[-1]['received'], ['Production: Refiner'])

if __name__ == '__main__':
    unittest.main()
