"""Cumulative Millewind graph and real AP fills."""
import unittest
from ap_runtime import load_world

World = load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
from Fill import distribute_items_restrictive
from worlds.craftopia.Progression import ProgressionModel


class MillewindTests(unittest.TestCase):
    def make(self, seed=1, **extra):
        return setup_multiworld(World, seed=seed, options=dict(
            logic_mode=5, mission_catalog_version=1, ending_region=1, **extra))

    def reachable(self, world, capability, missing=None):
        profile = dict(world.regional_profile)
        profile['missions'] = {'0': capability}
        model = ProgressionModel(profile, world.item_table, allow_experimental=True)
        return model.can_complete(0, lambda name: 0 if name == missing else 99)

    def test_cumulative_selection(self):
        w = self.make().worlds[1]
        p = w.regional_profile
        self.assertEqual(p['included_regions'], ['geezah', 'millewind'])
        self.assertEqual(p['goal_location_ids'], [3012, 4101])
        self.assertEqual(len(p['missions']), 52)
        self.assertEqual(len(p['milestones']), 21)
        self.assertEqual({r['ap_location_id'] for r in p['milestones']} &
                         {3010, 3011, 3012, 4001, 4101, 4228, 4341},
                         {3010, 3011, 3012, 4001, 4101, 4228, 4341})
        self.assertNotIn([{'node': 'category_141'}], p['nodes']['claim_category'])
        self.assertEqual(p['pool'].count('Progressive Equipment: Sword'), 3)
        self.assertIn('Production: equipment_care', p['pool'])
        self.assertTrue(w.progression_model.can_complete(67, lambda _: 0))

    def test_all_nodes_and_production_gates(self):
        w = self.make().worlds[1]
        for name in w.regional_profile['nodes']:
            with self.subTest(node=name):
                self.assertTrue(self.reachable(w, name))
        for name in ('Production: smelting', 'Production: cooking',
                     'Production: farming', 'Production: equipment_care'):
            with self.subTest(permission=name):
                self.assertFalse(self.reachable(w, 'tower_3807_3', name))
        self.assertTrue(self.reachable(w, 'dungeon_millewind', 'Progressive Equipment: Sword'))
        self.assertTrue(self.reachable(w, 'dungeon_millewind', 'Progressive Equipment: Body'))
        self.assertTrue(self.reachable(w, 'generator_operation', 'Skill Tree: Tamer'))
        self.assertFalse(self.reachable(w, 'generator_operation', 'Production: smelting'))

    def test_goal_requires_both_millewind_locations(self):
        mw = self.make()
        by_id = {l.address: l for l in mw.get_locations(1)}
        self.assertFalse(mw.completion_condition[1](CollectionState(mw)))
        for repaired, cleared in ((False, False), (True, False), (False, True), (True, True)):
            by_id[3012].access_rule = lambda _, result=repaired: result
            by_id[4101].access_rule = lambda _, result=cleared: result
            self.assertEqual(mw.completion_condition[1](CollectionState(mw)), repaired and cleared)

    def test_live_opening_corrections_and_age_order(self):
        w = self.make().worlds[1]
        p = dict(w.regional_profile)
        p['nodes'] = dict(p['nodes'], age_1=[[{'node': 'age_1'}]])
        p['missions'] = {'0': 'dungeon_geezah', '1': 'age_2', '2': 'tower_3807_1'}
        model = ProgressionModel(p, w.item_table, allow_experimental=True)
        self.assertFalse(model.can_complete(0, lambda name: int(name == 'Progressive Equipment: Bow')))
        self.assertFalse(model.can_complete(1, lambda _: 99))
        self.assertFalse(model.can_complete(2, lambda _: 99))

    def test_one_hundred_real_fills(self):
        for seed in range(100):
            with self.subTest(seed=seed):
                mw = self.make(seed, enable_soul_orb_checks=seed % 2)
                distribute_items_restrictive(mw)
                self.assertTrue(mw.can_beat_game())
                state = CollectionState(mw)
                pending = {l for l in mw.get_locations(1) if l.address is not None}
                while pending:
                    sphere = {l for l in pending if l.can_reach(state)}
                    self.assertTrue(sphere, 'unreachable: ' + ', '.join(l.name for l in pending))
                    pending -= sphere
                    for loc in sphere:
                        state.collect(loc.item, prevent_sweep=True)
                self.assertTrue(mw.completion_condition[1](state))


if __name__ == '__main__': unittest.main()
