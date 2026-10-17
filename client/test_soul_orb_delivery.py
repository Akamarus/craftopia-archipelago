import unittest
import json
from pathlib import Path
from unittest.mock import patch
from test_currency_delivery import CurrencyDelivery
from test_delivery_repair import run, cb


class SoulOrbDelivery(unittest.TestCase):
    setUp=CurrencyDelivery.setUp
    tearDown=CurrencyDelivery.tearDown
    new=CurrencyDelivery.new
    connect=CurrencyDelivery.connect

    def start(self):
        CurrencyDelivery.start(self)
        row=json.loads((Path(__file__).parents[1]/'worlds/craftopia/soul_orbs_20261015_r1.json').read_text())['orbs'][0]
        self.orb=row['guid']; self.location=row['ap_location_id']
        self.b.slot_data.update(soul_orb_catalog='soul_orbs_20261015_r1',soul_orb_schema=1,
            soul_orb_locations={str(self.orb):self.location})

    def message(self):
        return dict(cmd='check',check_category='soul_orb',orb_guid=self.orb,
            observation_id='orb:'+str(self.orb),epoch=self.b.ledger['epoch'],slot='TestPlayer',
            seed_name='currency-seed',character_guid='-123')

    def test_persist_before_send_and_replay_until_confirmed(self):
        self.start(); run(self.b.on_mod_message(self.message()))
        self.assertEqual(len(self.b.ledger['orb_checks']),1)
        self.assertTrue(any(m.get('cmd')=='LocationChecks' and self.location in m['locations'] for m in self.b.sent))
        run(self.b.on_mod_message(self.message())); self.assertEqual(len(self.b.ledger['orb_checks']),1)
        self.assertEqual(self.new().ledger['orb_checks'],self.b.ledger['orb_checks'])
        self.b.checked_locations.add(self.location); self.b.sent.clear()
        run(self.b.reconcile_orb_checks()); self.assertFalse(self.b.sent)

    def test_rejects_wrong_identity_unknown_and_mission_fallback(self):
        self.start()
        for key,value in [('character_guid','456'),('seed_name','other'),('orb_guid',True),('orb_guid',2147483647)]:
            run(self.b.on_mod_message(dict(self.message(),**{key:value})))
        self.assertFalse(self.b.ledger.get('orb_checks'))
        self.assertFalse(self.b.ledger['pending_checks'])

    def test_failed_persist_neither_sends_nor_acknowledges(self):
        self.start(); self.b.sent.clear(); self.b.mod_ws.sent.clear()
        with patch.object(cb,'save_ledger',side_effect=OSError('full')):
            run(self.b.on_mod_message(self.message()))
        self.assertFalse(self.b.sent)
        self.assertFalse(any(m.get('cmd')=='observation_ack' for m in self.b.mod_ws.sent))

    def test_no_cross_catalog_map(self):
        self.start(); self.b.slot_data['soul_orb_locations'][str(self.orb)]=27
        run(self.b.on_mod_message(self.message()))
        self.assertFalse(self.b.ledger.get('orb_checks'))
