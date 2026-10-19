"""Explicit approved enrollment for supported seeds without milestones."""
from unittest.mock import patch
from test_soul_orb_delivery import SoulOrbDelivery
from test_delivery_repair import run, cb


class CharacterEnrollment(SoulOrbDelivery):
    def registration(self):
        return dict(cmd='character_enrollment',character_guid='-123',epoch=self.b.ledger['epoch'],
                    slot='TestPlayer',seed_name='currency-seed',connection_token=self.b.connection_token,
                    local_enrollment_verified=True)
    def prepare(self):
        self.start()
        self.b.ledger['enrollments']=[]
        self.b.mod_ws.sent.clear()
    def test_nonregional_currency_registration_persists_and_reconnects(self):
        self.prepare()
        self.assertNotIn('milestone_catalog',self.b.slot_data)
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual([e['character_guid'] for e in self.new().ledger['enrollments']],['-123'])
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual(len(self.b.ledger['enrollments']),1)
        self.assertTrue(any(m.get('cmd')=='session' for m in self.b.mod_ws.sent))
    def test_generic_registration_accepts_valid_milestone_contract(self):
        self.prepare()
        self.b.slot_data.update(milestone_catalog='milestones_20261016_r1',milestone_schema=1,
            milestone_locations={'milestone:dungeon:1:0':4100},goal_location_ids=[4100])
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual([e['character_guid'] for e in self.b.ledger['enrollments']],['-123'])
    def test_missing_negotiated_currency_capability_rejected(self):
        self.prepare(); self.b.currency_capabilities={}
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual(self.b.ledger['enrollments'],[])
    def test_requires_exact_local_approval_and_binding(self):
        self.prepare()
        for key,value in [('local_enrollment_verified',False),('local_enrollment_verified',1),('character_guid','456'),
                          ('seed_name','other'),('epoch',True),('epoch',0),('slot','other'),('connection_token','stale')]:
            run(self.b.on_mod_message(dict(self.registration(),**{key:value})))
            self.assertEqual(self.b.ledger['enrollments'],[],key)
    def test_missing_currency_contract_and_malformed_milestones_rejected(self):
        for replacement in ({}, {'soul_orb_currency_protocol':True}, {'soul_orb_currency_protocol':2},
                            {'soul_orb_currency_protocol':1,'milestone_catalog':'bad'}):
            self.prepare()
            self.b.slot_data=replacement
            run(self.b.on_mod_message(self.registration()))
            self.assertEqual(self.b.ledger['enrollments'],[])
    def test_recovery_hold_or_conflicting_character_cannot_enroll(self):
        self.prepare(); self.b.recovery_hold=True
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual(self.b.ledger['enrollments'],[])
        self.b.recovery_hold=False
        self.b.ledger['enrollments']=[dict(character_guid='456',slot='TestPlayer',enrolled_at='test')]
        run(self.b.on_mod_message(self.registration()))
        self.assertEqual([e['character_guid'] for e in self.b.ledger['enrollments']],['456'])
    def test_failed_persist_cannot_acknowledge(self):
        self.prepare()
        with patch.object(cb,'save_ledger',side_effect=OSError('full')):
            run(self.b.on_mod_message(self.registration()))
        self.assertEqual(self.b.ledger['enrollments'],[])
        self.assertFalse(any(m.get('cmd')=='session' for m in self.b.mod_ws.sent))
    def test_legacy_milestone_command_still_requires_catalog(self):
        self.prepare()
        run(self.b.on_mod_message(dict(self.registration(),cmd='milestone_enrollment')))
        self.assertEqual(self.b.ledger['enrollments'],[])
