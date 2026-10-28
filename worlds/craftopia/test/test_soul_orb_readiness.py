import unittest
from ap_runtime import load_world

World = load_world()
from test.general import setup_multiworld


class SoulOrbReadiness(unittest.TestCase):
    def test_reserved_ids_do_not_activate_checks(self):
        mw = setup_multiworld(World, seed=1, options={'logic_mode': 0, 'mission_catalog_version': 0})
        self.assertIn('Soul Orb -1595765594', World.location_name_to_id)
        self.assertEqual(len([l for l in mw.get_locations(1) if l.address is not None]), 53)
        self.assertEqual(mw.worlds[1].fill_slot_data()['enable_soul_orb_checks'], 0)

    def test_experimental_scope_is_explicit(self):
        with self.assertRaisesRegex(ValueError, 'experimental expedition'):
            setup_multiworld(World, seed=1, options={'enable_soul_orb_checks': 1, 'logic_mode': 0, 'mission_catalog_version': 0})

    def test_currency_supply_and_separate_objectives(self):
        from collections import Counter
        from Fill import distribute_items_restrictive
        import json
        for seed in range(3):
            mw=setup_multiworld(World,seed=seed,options={'enable_soul_orb_checks':1,
                'logic_mode':3,'mission_catalog_version':1,'goal_objective_count':60})
            self.assertEqual(len(mw.itempool),564)
            self.assertEqual(Counter(i.name for i in mw.itempool)['Soul Orb Currency'],439)
            self.assertEqual(Counter(i.name for i in mw.itempool)['Skill Point'],19)
            sd=json.loads(json.dumps(mw.worlds[1].fill_slot_data()))
            self.assertEqual(len(sd['soul_orb_locations']),439)
            self.assertEqual(len(sd['objective_locations']),125)
            self.assertEqual(sd['soul_orb_currency_protocol'],1)
            distribute_items_restrictive(mw)
            self.assertTrue(mw.can_beat_game()) # Modeled logic only; traversal remains unverified.

    def test_orbs_cannot_inflate_mission_goal(self):
        with self.assertRaisesRegex(Exception,'objective count'):
            setup_multiworld(World,seed=1,options={'enable_soul_orb_checks':1,
                'logic_mode':3,'mission_catalog_version':1,'goal_objective_count':126})

    def test_real_slot_data_bridge_roundtrip(self):
        import json, sys
        from pathlib import Path
        sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'client'))
        import test_currency_delivery as fixtures
        import test_delivery_repair as bridge
        mw=setup_multiworld(World,seed=77,options={'enable_soul_orb_checks':1,
            'logic_mode':3,'mission_catalog_version':1,'goal_objective_count':60})
        fixture=fixtures.CurrencyDelivery()
        fixture.setUp()
        try:
            fixture.b.slot_data=json.loads(json.dumps(mw.worlds[1].fill_slot_data()))
            fixture.b.ledger['seed_name']='roundtrip'
            fixture.b.ap_connected=True; fixture.b.mod_ws=bridge.Socket()
            bridge.run(fixture.b.try_handshake(fixture.b.mod_ws,dict(cmd='connect',name='TestPlayer',
                game='Craftopia',unlock_protocol=2,equipment_protocol=1,currency_protocol=1,soul_orb_protocol=1)))
            fixture.b.primary_guid='-123'
            fixture.b.ledger['enrollments']=[dict(character_guid='-123',slot='TestPlayer',enrolled_at='test')]
            sd=fixture.b.slot_data
            orb,location=next(iter(sd['soul_orb_locations'].items()))
            packet=dict(cmd='check',check_category='soul_orb',orb_guid=int(orb),observation_id='orb:'+orb,
                epoch=fixture.b.ledger['epoch'],slot='TestPlayer',seed_name='roundtrip',character_guid='-123')
            bridge.run(fixture.b.on_mod_message(packet))
            self.assertTrue(any(m.get('cmd')=='LocationChecks' and location in m['locations'] for m in fixture.b.sent))
            currency_id=World.item_name_to_id['Soul Orb Currency']
            fixture.b.item_id_to_name[currency_id]='Soul Orb Currency'
            bridge.run(fixture.b.process_received_items(dict(index=0,items=[dict(item=currency_id)])))
            grant=next(m for m in fixture.b.mod_ws.sent if m.get('cmd')=='grant_currency')
            self.assertEqual(grant['item_id'],3840)
            bridge.run(fixture.b.on_mod_message(dict(grant,cmd='currency_ack',saved=True)))
            self.assertTrue(fixture.new().ledger['currency_receipts'][0]['acknowledged'])
        finally: fixture.tearDown()


if __name__ == '__main__': unittest.main()
