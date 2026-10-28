import copy
import unittest
from ap_runtime import load_world
World=load_world()
from test.general import setup_multiworld
from Fill import distribute_items_restrictive


class RegionalTests(unittest.TestCase):
    def make(self,seed=1,**extra):
        return setup_multiworld(World,seed=seed,options=dict(logic_mode=4,mission_catalog_version=1,**extra))

    def test_selection_and_contract(self):
        mw=self.make();w=mw.worlds[1];sd=w.fill_slot_data()
        locations=[l for l in mw.get_locations(1) if l.address is not None]
        self.assertEqual(len(locations),len(mw.itempool))
        self.assertEqual(sd['goal_location_ids'],[4100])
        self.assertEqual(sd['logic_mode'],'geezah_pilot')
        self.assertFalse(any('Millewind' in l.name for l in locations))
        self.assertEqual(set(sd['milestone_locations'].values()),
                         {l.address for l in locations if l.check_category=='milestone'})
        self.assertGreaterEqual(len(locations),40)

    def test_profile_rejects_unreviewed_selection(self):
        from worlds.craftopia.Regional import load_profile,build_regional_profile
        w=self.make().worlds[1]
        for field,value in [('region','volcana'),('missions',[999999]),('milestones',[3010])]:
            data=copy.deepcopy(load_profile());data[field]=value
            with self.assertRaises(ValueError):build_regional_profile(data,w.item_table)
        data=copy.deepcopy(load_profile());data['missions'].remove(50)
        with self.assertRaises(ValueError):build_regional_profile(data,w.item_table)

    def test_ten_real_fills(self):
        for seed in range(10):
            mw=self.make(seed);distribute_items_restrictive(mw)
            self.assertTrue(mw.can_beat_game(),str(seed))
            smelting=next(l for l in mw.get_locations(1) if l.item and l.item.name=='Production: smelting')
            self.assertIn(smelting.address,mw.worlds[1].regional_profile['smelting_locations'])

    def test_unsupported_orbs_and_multiplayer_rejected(self):
        with self.assertRaises(ValueError):self.make(enable_soul_orb_checks=1)
        with self.assertRaises(ValueError):self.make(multiplayer_mode=1)
    def test_exclusions_cannot_remove_all_smelting_destinations(self):
        w=self.make().worlds[1]
        names={name for name,loc in w.location_name_to_id.items() if loc in w.regional_profile['smelting_locations']}
        with self.assertRaises(ValueError):self.make(exclude_locations=names)
    def test_smelting_restriction_is_composed_with_location_rule(self):
        mw=self.make();w=mw.worlds[1];item=w.create_item('Production: smelting')
        boss=mw.get_location('Geezah Plateau: Ruin of Beginning',1)
        self.assertFalse(boss.item_rule(item))
        self.assertTrue(boss.item_rule(w.create_item('Skill Point')))
        early=next(l for l in mw.get_locations(1) if l.address in w.regional_profile['smelting_locations'])
        self.assertTrue(early.item_rule(item))
        item.player=2
        self.assertTrue(early.item_rule(item))
        self.assertTrue(boss.item_rule(item))

if __name__=='__main__':unittest.main()
