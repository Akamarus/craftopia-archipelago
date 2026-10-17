import unittest
import test_observation_ack as fixtures
run = fixtures.run
class CheckNotices(unittest.TestCase):
 setUp = fixtures.ObservationAck.setUp
 tearDown = fixtures.ObservationAck.tearDown
 new = fixtures.ObservationAck.new
 connect = fixtures.ObservationAck.connect
 message = fixtures.ObservationAck.message
 def test_confirmation_only_after_server_and_once(self):
  sock=self.connect(); sock.sent.clear()
  run(self.b.on_mod_message(self.message()))
  self.assertFalse(any(m.get('cmd')=='check_confirmed' for m in sock.sent))
  run(self.b.on_room_update({'checked_locations':[1]}))
  notices=[m for m in sock.sent if m.get('cmd')=='check_confirmed']
  self.assertEqual(len(notices),1)
  self.assertEqual(notices[0]['epoch'],self.b.ledger['epoch'])
  self.assertEqual(notices[0]['connection_token'],self.b.connection_token)
  run(self.b.on_room_update({'checked_locations':[1]}))
  self.assertEqual(len([m for m in sock.sent if m.get('cmd')=='check_confirmed']),1)
 def test_replayed_completed_check_does_not_announce(self):
  sock=self.connect(); self.b.checked_locations={1}; sock.sent.clear()
  run(self.b.on_mod_message(self.message()))
  self.assertFalse(any(m.get('cmd')=='check_confirmed' for m in sock.sent))
  self.assertIn('location:1', self.b.ledger['goal_events']['completed_objectives'])
 def test_recipient_disconnect_cannot_interrupt_reconciliation(self):
  self.connect(); run(self.b.on_mod_message(self.message()))
  from test_delivery_repair import Socket
  self.b.extra_mod_conns={'a':{'ws':Socket(),'validated':True},'b':{'ws':Socket(),'validated':True}}
  async def send(msg,token=None):
   if msg.get('cmd')=='check_confirmed' and token=='a': self.b.extra_mod_conns.pop('b',None)
  self.b.send_mod=send
  run(self.b.on_room_update({'checked_locations':[1]}))
  self.assertFalse(self.b.ledger['pending_checks'])
 def test_cosmetic_send_failure_does_not_interrupt_goal(self):
  self.connect(); run(self.b.on_mod_message(self.message()))
  self.b.goal_announced=False
  async def send(msg,token=None): raise OSError('notification unavailable')
  self.b.send_mod=send
  run(self.b.on_room_update({'checked_locations':[1]}))
  self.assertTrue(self.b.goal_announced)
  self.assertFalse(self.b.ledger['pending_checks'])
if __name__=='__main__': unittest.main()
