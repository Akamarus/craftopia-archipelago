"""A check acknowledgment transfers durable custody to the bridge."""
import unittest
from unittest.mock import patch
import test_delivery_repair as fixtures
run, cb = fixtures.run, fixtures.cb

class ObservationAck(unittest.TestCase):
    setUp = fixtures.DeliveryRepair.setUp
    tearDown = fixtures.DeliveryRepair.tearDown
    new = fixtures.DeliveryRepair.new
    connect = fixtures.DeliveryRepair.connect
    def message(self):
        return dict(cmd='check', epoch=self.b.ledger['epoch'], slot='TestPlayer',
                    mission_id=50, mission_title='Example', mission_class='ExampleMission',
                    observation_id='event-50')
    def test_check_ack_after_durable_commit(self):
        sock=self.connect(); sock.sent.clear()
        run(self.b.on_mod_message(self.message()))
        acks=[m for m in sock.sent if m.get('cmd')=='observation_ack']
        self.assertEqual(len(acks), 1)
        self.assertEqual(acks[0]['observation_id'], 'event-50')
        self.assertTrue(self.new().ledger['pending_checks'])
    def test_failed_commit_does_not_ack(self):
        sock=self.connect(); sock.sent.clear()
        with patch.object(cb, 'save_ledger', side_effect=OSError('disk full')):
            run(self.b.on_mod_message(self.message()))
        self.assertFalse(any(m.get('cmd')=='observation_ack' for m in sock.sent))
    def test_stale_check_does_not_ack(self):
        sock=self.connect(); sock.sent.clear()
        run(self.b.on_mod_message(dict(self.message(),epoch=-1)))
        self.assertFalse(any(m.get('cmd')=='observation_ack' for m in sock.sent))
    def test_deferred_check_ack_still_durable(self):
        sock=self.connect(); sock.sent.clear(); self.b.datapackage_ready=False
        run(self.b.on_mod_message(self.message()))
        self.assertTrue(any(m.get('cmd')=='observation_ack' for m in sock.sent))
        self.assertTrue(self.new().ledger['pending_checks'][0]['deferred'])

if __name__=='__main__': unittest.main()
