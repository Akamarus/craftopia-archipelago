import unittest
from item_notices import ItemNotices

class Notices(unittest.TestCase):
    def setUp(self):
        self.n = ItemNotices()
        self.n.bind(1, {1: 'Me', 2: 'Alex'}, {1: 'Craftopia', 2: 'Other'})
        self.n.packages({'Craftopia': {'item_name_to_id': {'Bow': 10}, 'location_name_to_id': {'Tree': 20}},
                         'Other': {'item_name_to_id': {'Pipes': 11}, 'location_name_to_id': {'Castle': 21}}})

    def test_directions_and_cross_game_names(self):
        sent = self.n.event([11, 20, 1, 0], 2)
        self.assertEqual((sent['title'], sent['location'], sent['kind']), ('Sent Pipes to Alex', 'Tree', 'sent'))
        own = self.n.event([10, 20, 1, 0], 1)
        self.assertEqual(own['title'], 'Found Bow')
        received = self.n.event([10, 21, 2, 0], 1)
        self.assertEqual((received['title'], received['location']), ('Received Bow from Alex', 'Castle'))
        self.assertIsNone(self.n.event([10, 21, 2, 0], 1))

    def test_replay_without_losing_live_events(self):
        initial = self.n.event([10, 20, 1, 0], 1, silent=True)
        self.assertTrue(initial['silent'])
        self.assertIsNone(self.n.event([10, 20, 1, 0], 1))
        events = [self.n.event([10, i, 2, 0], 1) for i in range(200)]
        self.assertEqual(sum(e is not None for e in events), 200)
        self.assertEqual(events[-1]['location'], 'Location #199')
        self.n.bind(1, {1:'Me',2:'Alex'}, {1:'Craftopia',2:'Other'})
        self.assertIsNone(self.n.event([10,199,2,0],1))

import test_observation_ack as fixtures

class BridgeNotices(unittest.TestCase):
    setUp = fixtures.ObservationAck.setUp
    tearDown = fixtures.ObservationAck.tearDown
    new = fixtures.ObservationAck.new
    connect = fixtures.ObservationAck.connect

    def configure(self):
        self.b.item_notices.bind(1,{1:'Me',2:'Alex'},{1:'Craftopia',2:'Other'})
        self.b.item_notices.packages({'Craftopia': {'item_name_to_id': {'Skill Point': 1},
            'location_name_to_id': {'Tree': 20}}, 'Other': {'item_name_to_id': {'Pipes': 11},
            'location_name_to_id': {'Castle': 21}}})
        return self.connect()

    def test_initial_replay_quiet_then_live_zero_and_duplicate_packets(self):
        sock = self.configure()
        fixtures.run(self.b.on_received_items({'index':0,'items':[]}))
        fixtures.run(self.b.on_received_items({'index':0,'items':[[1,20,1,0]]}))
        fixtures.run(self.b.on_ap_message({'cmd':'PrintJSON','type':'ItemSend',
            'item':[1,20,1,0],'receiving':1,'data':[]}))
        notices = [m for m in sock.sent if m.get('cmd')=='item_notice']
        self.assertEqual(len(notices),1)
        self.assertEqual(notices[0]['title'],'Found Skill Point')
        self.assertEqual(notices[0]['connection_token'], self.b.connection_token)

    def test_old_items_quiet_and_sent_card_cross_game(self):
        sock = self.configure()
        fixtures.run(self.b.on_received_items({'index':0,'items':[[1,20,1,0]]}))
        self.assertFalse(any(m.get('cmd')=='item_notice' for m in sock.sent))
        fixtures.run(self.b.on_ap_message({'cmd':'PrintJSON','type':'ItemSend',
            'item':[11,20,1,0],'receiving':2,'data':[]}))
        notices = [m for m in sock.sent if m.get('cmd')=='item_notice']
        self.assertEqual(notices[0]['title'],'Sent Pipes to Alex')
        self.assertEqual(notices[0]['location'],'Tree')

if __name__ == '__main__': unittest.main()
