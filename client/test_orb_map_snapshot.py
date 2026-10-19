import unittest
from orb_map import orb_map_payload
from soul_orb_delivery import CATALOG, frozen_orbs
class Tests(unittest.TestCase):
 def test_seed_filter_release_and_pickup(self):
  pairs=list(frozen_orbs().items())[:3]; (a,x),(b,y),(c,z)=pairs
  slot={'soul_orb_catalog':CATALOG,'soul_orb_schema':1,'soul_orb_locations':{str(a):x,str(b):y}}
  p=orb_map_payload(slot,{x,z},[{'orb_guid':b,'location_id':y},{'orb_guid':c,'location_id':z}])
  self.assertEqual(p['checked'],[x]);self.assertEqual(p['collected'],[b]);self.assertLess(len(str(p)),48000)
 def test_invalid_contract(self):
  self.assertEqual(orb_map_payload({},set(),[])['checked'],[])
  a,x=next(iter(frozen_orbs().items()))
  d={'soul_orb_catalog':CATALOG,'soul_orb_schema':1,'soul_orb_locations':{str(a):x+1}}
  self.assertEqual(orb_map_payload(d,{x+1},[])['checked'],[])
 def test_full_catalog_fits_packet(self):
  import json
  all_orbs=frozen_orbs()
  d={'soul_orb_catalog':CATALOG,'soul_orb_schema':1,'soul_orb_locations':{str(g):l for g,l in all_orbs.items()}}
  p=orb_map_payload(d,set(all_orbs.values()),[{'orb_guid':g,'location_id':l} for g,l in all_orbs.items()])
  self.assertEqual(len(p['checked']),len(all_orbs));self.assertEqual(len(p['collected']),len(all_orbs))
  self.assertLess(len(json.dumps(p).encode('utf-8')),48000)

from test_soul_orb_delivery import SoulOrbDelivery
from test_delivery_repair import run
class Integration(unittest.TestCase):
 setUp=SoulOrbDelivery.setUp
 tearDown=SoulOrbDelivery.tearDown
 new=SoulOrbDelivery.new
 connect=SoulOrbDelivery.connect
 start=SoulOrbDelivery.start
 message=SoulOrbDelivery.message
 def test_snapshot_bound_and_persisted(self):
  self.start();run(self.b.on_mod_message(self.message()))
  snap=[m for m in self.b.mod_ws.sent if m.get('cmd')=='orb_map_snapshot'][-1]
  self.assertEqual(snap['epoch'],self.b.ledger['epoch']);self.assertEqual(snap['slot'],'TestPlayer')
  self.assertEqual(snap['seed_name'],'currency-seed');self.assertEqual(snap['connection_token'],self.b.connection_token)
  self.assertEqual(snap['collected'],[self.orb])
  restored=self.new()
  payload=orb_map_payload(self.b.slot_data,set(),restored.ledger['orb_checks'])
  self.assertEqual(payload['collected'],[self.orb]);self.assertEqual(payload['checked'],[])
  payload=orb_map_payload({},set(),restored.ledger['orb_checks'])
  self.assertEqual(payload['collected'],[])
if __name__=='__main__':unittest.main()
