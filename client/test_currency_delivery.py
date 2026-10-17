import unittest
from unittest.mock import patch
import test_delivery_repair as f


class CurrencyDelivery(unittest.TestCase):
    setUp = f.DeliveryRepair.setUp
    tearDown = f.DeliveryRepair.tearDown
    new = f.DeliveryRepair.new
    def connect(self):
        self.b.ap_connected = True
        self.b.mod_ws = f.Socket()
        f.run(self.b.try_handshake(self.b.mod_ws, {'cmd':'connect','name':'TestPlayer',
            'game':'Craftopia','unlock_protocol':2,'currency_protocol':1,'soul_orb_protocol':1}))
        return self.b.mod_ws

    def start(self):
        self.b.slot_data['soul_orb_currency_protocol'] = 1
        self.b.ledger['seed_name'] = 'currency-seed'
        self.connect()
        self.b.primary_guid = '-123'
        self.b.ledger['enrollments'] = [{'character_guid': '-123', 'slot': 'TestPlayer', 'enrolled_at': 'test'}]
        self.b.item_id_to_name[44] = 'Soul Orb Currency'

    def receive(self, index=0):
        f.run(self.b.process_received_items({'index': index, 'items': [{'item': 44}]}))

    def packet(self):
        return next(m for m in reversed(self.b.mod_ws.sent) if m.get('cmd') == 'grant_currency')

    def test_persist_send_ack_restart(self):
        self.start(); self.receive()
        packet = self.packet()
        self.assertEqual(packet['character_guid'], '-123')
        self.assertEqual(packet['seed_name'], 'currency-seed')
        self.assertFalse(self.b.ledger['currency_receipts'][0]['acknowledged'])
        f.run(self.b.on_mod_message(dict(packet, cmd='currency_ack', saved=True)))
        self.assertTrue(self.new().ledger['currency_receipts'][0]['acknowledged'])
        self.b.mod_ws.sent.clear(); self.receive()
        self.assertFalse(any(m.get('cmd') == 'grant_currency' for m in self.b.mod_ws.sent))

    def test_failed_write_never_sends(self):
        self.start(); self.b.mod_ws.sent.clear()
        with patch.object(f.cb, 'save_ledger', side_effect=OSError('full')):
            self.receive()
        self.assertFalse(any(m.get('cmd') == 'grant_currency' for m in self.b.mod_ws.sent))
        self.assertNotIn(0, self.b.seen_non_grant)
        self.receive(); self.packet()

    def test_wrong_identity_or_payload_never_acks(self):
        self.start(); self.receive(); packet=self.packet()
        for key,value in [('seed_name','other'),('slot','other'),('epoch',True),('character_guid','123'),('connection_token','old'),('amount',2),('receipt_id',True),('item_id',698),('currency_protocol',True)]:
            f.run(self.b.on_mod_message(dict(packet,cmd='currency_ack',saved=True,**{key:value})))
            self.assertFalse(self.b.ledger['currency_receipts'][0]['acknowledged'], key)
        for saved in (None,False,1,'true'):
            f.run(self.b.on_mod_message(dict(packet,cmd='currency_ack',saved=saved)))
            self.assertFalse(self.b.ledger['currency_receipts'][0]['acknowledged'])

    def test_character_switch_never_reassigns_pending(self):
        self.start(); self.receive(); self.b.mod_ws.sent.clear()
        self.b.primary_guid='456'
        f.run(self.b.flush_currency())
        self.assertFalse(any(m.get('cmd')=='grant_currency' for m in self.b.mod_ws.sent))
        self.assertEqual(self.b.ledger['currency_receipts'][0]['character_guid'],'-123')

    def test_unnegotiated_stays_retryable(self):
        self.start(); self.b.slot_data.pop('soul_orb_currency_protocol')
        self.receive()
        self.assertNotIn(0,self.b.seen_non_grant)
        self.assertFalse(self.b.ledger.get('currency_receipts'))

    def test_pending_replays_and_ack_write_failure_keeps_it(self):
        self.start(); self.receive(); packet=self.packet()
        with patch.object(f.cb,'save_ledger',side_effect=OSError('full')):
            f.run(self.b.on_mod_message(dict(packet,cmd='currency_ack',saved=True)))
        self.assertFalse(self.b.ledger['currency_receipts'][0]['acknowledged'])
        self.b.mod_ws.sent.clear(); f.run(self.b.flush_currency()); self.packet()

    def test_hold_blocks_ack_and_send(self):
        self.start(); self.receive(); packet=self.packet()
        self.b.recovery_hold=True; self.b.mod_ws.sent.clear()
        f.run(self.b.on_mod_message(dict(packet,cmd='currency_ack',saved=True)))
        f.run(self.b.flush_currency())
        self.assertFalse(self.b.ledger['currency_receipts'][0]['acknowledged'])
        self.assertFalse(self.b.mod_ws.sent)

    def test_missing_character_field_rejected(self):
        self.start(); self.receive()
        del self.b.ledger['currency_receipts'][0]['character_guid']
        with self.assertRaises(ValueError): f.cb.validate(self.b.ledger)

    def test_old_mod_blocked_in_both_connection_orders(self):
        for ap_first in (True, False):
            self.b = self.new()
            if ap_first: self.b.slot_data['soul_orb_currency_protocol'] = 1
            f.DeliveryRepair.connect(self)
            self.b.slot_data['soul_orb_currency_protocol'] = 1
            self.b.ledger['seed_name'] = 'currency-seed'
            self.b.mod_ws.sent.clear()
            f.run(self.b.send_session())
            f.run(self.b.send_grant([9]))
            self.assertFalse(self.b.mod_ws.sent)

    def test_boolean_capability_is_not_protocol_one(self):
        self.start()
        self.b.currency_capabilities[self.b.connection_token] = (True, 1)
        self.assertFalse(self.b.currency_ready())
