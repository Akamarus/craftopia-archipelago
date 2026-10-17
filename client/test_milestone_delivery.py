from unittest.mock import patch
from test_soul_orb_delivery import SoulOrbDelivery
from test_delivery_repair import run,cb

class MilestoneDelivery(SoulOrbDelivery):
    def start(self):
        super().start()
        self.location=4100
        self.b.slot_data.update(milestone_catalog='milestones_20261016_r1',milestone_schema=1,
            milestone_locations={'milestone:dungeon:1:0':4100},goal_location_ids=[4100])
    def message(self):
        return dict(cmd='check',check_category='milestone',family='dungeon',source_id=1,stage=0,
            observation_id='milestone:dungeon:1:0',epoch=self.b.ledger['epoch'],slot='TestPlayer',
            seed_name='currency-seed',character_guid='-123',world_fingerprint='A'*64)
    def test_persist_before_send_and_replay_until_confirmed(self):
        self.start();run(self.b.on_mod_message(self.message()))
        self.assertEqual(len(self.b.ledger.get('milestone_checks',[])),1)
        run(self.b.on_mod_message(self.message()))
        self.assertEqual(len(self.b.ledger['milestone_checks']),1)
        self.assertEqual(self.new().ledger['milestone_checks'],self.b.ledger['milestone_checks'])
        self.assertTrue(any(m.get('cmd')=='LocationChecks' and 4100 in m['locations'] for m in self.b.sent))
        self.b.checked_locations.add(4100);self.b.sent.clear()
        run(self.b.reconcile_milestone_checks());self.assertFalse(self.b.sent)
    def test_rejects_wrong_identity_unknown_and_mission_fallback(self):
        self.start()
        for k,v in [('character_guid','456'),('seed_name','other'),('source_id',True),('source_id',2),('stage',1),('family','unknown'),('world_fingerprint','bad')]:
            run(self.b.on_mod_message(dict(self.message(),**{k:v})))
        self.assertFalse(self.b.ledger.get('milestone_checks'));self.assertFalse(self.b.ledger['pending_checks'])
    def test_no_cross_catalog_map(self):
        self.start();self.b.slot_data['milestone_locations']['milestone:dungeon:1:0']=4101
        run(self.b.on_mod_message(self.message()));self.assertFalse(self.b.ledger.get('milestone_checks'))
    def test_cross_world_rejected(self):
        self.start();run(self.b.on_mod_message(self.message()))
        self.b.mod_ws.sent.clear()
        run(self.b.on_mod_message(dict(self.message(),world_fingerprint='B'*64)))
        self.assertFalse(any(m.get('cmd')=='observation_ack' for m in self.b.mod_ws.sent))
    def test_goal_only_after_confirmation(self):
        self.start();run(self.b.on_mod_message(self.message()))
        self.assertFalse(self.b.check_goal_complete())
        self.b.checked_locations.add(4100)
        self.assertTrue(self.b.check_goal_complete())
    def test_goal_requires_every_selected_location(self):
        self.start()
        self.b.slot_data['milestone_locations']['milestone:age:0:1']=4000
        self.b.slot_data['goal_location_ids']=[4100,4000]
        self.b.checked_locations.add(4100)
        self.assertFalse(self.b.check_goal_complete())
        self.b.checked_locations.add(4000)
        self.assertTrue(self.b.check_goal_complete())
        for goals in ([4100,4100],[9999],[],[True]):
            self.b.slot_data['goal_location_ids']=goals
            self.assertFalse(self.b.check_goal_complete())
    def test_regional_tower_and_dungeon_require_ap_confirmation(self):
        self.start()
        self.b.slot_data.update(logic_mode='regional',selected_region='geezah',
            goal_location_ids=[3002,4100])
        self.b.slot_data['milestone_locations']['milestone:tower:1519884039:3']=3002
        for checked, expected in ((set(),False),({3000,3001,4100},False),({3002},False),({3002,4100},True)):
            self.b.checked_locations=set(checked)
            self.assertEqual(self.b.check_goal_complete(),expected)
    def test_unmapped_history_holds_instead_of_silently_dropping(self):
        self.start();run(self.b.on_mod_message(self.message()))
        self.b.ledger['milestone_checks'][0]['location_id']=4101
        self.b.sent.clear();run(self.b.reconcile_milestone_checks())
        self.assertTrue(self.b.recovery_hold)
        self.assertFalse(self.b.sent)
    def test_reconnect_does_not_repeat_goal_status(self):
        self.start();self.b.checked_locations.add(4100)
        run(self.b.check_goal_completion())
        self.b.sent.clear()
        run(self.b.on_connected(dict(slot_data=self.b.slot_data,checked_locations=[4100],missing_locations=[])))
        self.assertFalse(any(m.get('cmd')=='StatusUpdate' for m in self.b.sent))

    def registration(self):
        return dict(cmd='milestone_enrollment',character_guid='-123',epoch=self.b.ledger['epoch'],
            slot='TestPlayer',seed_name='currency-seed',connection_token=self.b.connection_token,
            local_enrollment_verified=True)
    def test_single_player_registration_and_reconnect(self):
        self.start();self.b.ledger['enrollments']=[]
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual([e['character_guid'] for e in self.b.ledger['enrollments']],['-123'])
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual(len(self.b.ledger['enrollments']),1)
    def test_registration_rejects_unverified_or_wrong_session(self):
        self.start();self.b.ledger['enrollments']=[]
        for key,value in [('local_enrollment_verified',False),('character_guid','456'),('seed_name','other'),('epoch',0),('slot','other'),('connection_token','stale')]:
            run(self.b.on_mod_message(dict(self.registration(),**{key:value})))
            self.assertEqual(self.b.ledger['enrollments'],[])
    def test_registration_persist_failure_is_not_acknowledged(self):
        self.start();self.b.ledger['enrollments']=[];self.b.mod_ws.sent.clear()
        with patch.object(cb,'save_ledger',side_effect=OSError('full')):
            run(self.b.on_mod_message(self.registration()))
        self.assertEqual(self.b.ledger['enrollments'],[])
        self.assertFalse(any(m.get('cmd')=='session' for m in self.b.mod_ws.sent))
