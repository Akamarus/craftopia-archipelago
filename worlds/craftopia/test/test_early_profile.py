import unittest
from ap_runtime import load_world
World=load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
class EarlyProfile(unittest.TestCase):
 def make(self,seed=1):
  return setup_multiworld(World,seed=seed,options={'logic_mode':2,'mission_catalog_version':1,'goal_objective_count':11})
 def test_subset_pool_routing_and_dependency(self):
  mw=self.make(); w=mw.worlds[1]; locs=[l for l in mw.get_locations(1) if l.address is not None]
  self.assertEqual({l.game_id for l in locs},{30,50,51,52,53,54,55,56,57,58,60})
  self.assertEqual(len(mw.itempool),11)
  sd=w.fill_slot_data()
  self.assertEqual(set(sd['objective_locations']),{l.name for l in locs})
  self.assertEqual(set(sd['game_id_to_location'].values()),{l.name for l in locs})
  self.assertEqual(sd['equipment_protocol'],1)
  sword=next(l for l in locs if l.game_id==60); state=CollectionState(mw)
  self.assertFalse(sword.can_reach(state))
  state.collect(w.create_item('Progressive Equipment: Sword'),prevent_sweep=True)
  self.assertTrue(sword.can_reach(state))
 def test_invalid_shared_mode_and_oversized_goal_rejected(self):
  for extra in ({'multiplayer_mode':1},{'goal_objective_count':12}):
   with self.subTest(extra=extra), self.assertRaises(Exception):
    setup_multiworld(World,options=({'logic_mode':2,'mission_catalog_version':1,'goal_objective_count':11} | extra))
 def test_real_fill_reachable_completion_and_no_self_lock(self):
  from Fill import distribute_items_restrictive
  for seed in range(20):
   mw=self.make(seed); distribute_items_restrictive(mw)
   self.assertTrue(mw.can_beat_game(),str(seed))
   sword=next(l for l in mw.get_locations(1) if getattr(l,'game_id',None)==60)
   self.assertNotEqual(sword.item.name,'Progressive Equipment: Sword')
if __name__=='__main__': unittest.main()
