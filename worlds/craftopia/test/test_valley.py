"""Cumulative Brigandine endpoint and its actual crafting requirements."""
import unittest
from ap_runtime import load_world

World = load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
from Fill import distribute_items_restrictive
from worlds.craftopia.Milestones import validate_slot, resolve_milestone
from worlds.craftopia.Progression import ProgressionModel


class ValleyTests(unittest.TestCase):
    def make(self, seed=1, **extra):
        return setup_multiworld(World, seed=seed, options=dict(
            logic_mode=5, mission_catalog_version=1, ending_region=5, **extra))

    def test_cumulative_goal_and_identities(self):
        w = self.make(enable_soul_orb_checks=1).worlds[1]
        p, data = w.regional_profile, w.fill_slot_data()
        self.assertEqual(p['goal_location_ids'], [3052, 4105])
        self.assertEqual(p['included_regions'][-2:], ['owatatsu', 'brigandine'])
        validate_slot(data)
        self.assertEqual(resolve_milestone(data, 'dungeon', 5, 0), 4104)
        self.assertEqual(resolve_milestone(data, 'dungeon', 6, 0), 4105)
        for source in (205, 206):
            self.assertIsNone(resolve_milestone(data, 'dungeon', source, 0))
        for address in (3022, 3032, 3042, 3052, 4003):
            self.assertIn(address, data['milestone_locations'].values())
        self.assertFalse(any(r.get('definition_id', 0) >= 3812 for r in p['milestones']))
        self.assertEqual(len(data['soul_orb_locations']), 239)
        self.assertEqual(len(w.multiworld.itempool), 330)
        for loc in w.multiworld.get_locations(1):
            if getattr(loc, 'check_category', None) == 'soul_orb':
                self.assertFalse(loc.item_rule(w.create_item('Progressive Equipment: Sword')))

    def test_dependencies(self):
        w = self.make().worlds[1]
        def reachable(target, blocked=None, missing=None):
            p = dict(w.regional_profile)
            p['nodes'] = dict(p['nodes'])
            if blocked: p['nodes'][blocked] = [[{'node': blocked}]]
            p['missions'] = {'0': target}
            return ProgressionModel(p, w.item_table, allow_experimental=True).can_complete(
                0, lambda name: 0 if name == missing else 99)
        for n in w.regional_profile['nodes']:
            with self.subTest(node=n): self.assertTrue(reachable(n))
        for target, blocked in [('material_702', 'silver_pickaxe'),
                ('tower_3810_3', 'machine_factory'), ('tower_3811_3', 'furniture_workbench'),
                ('tower_3810_1', 'age_4'), ('tower_3811_1', 'regional_herbs')]:
            self.assertFalse(reachable(target, blocked=blocked))
        self.assertTrue(reachable('age_4', blocked='tower_3810_3'))
        self.assertTrue(reachable('age_4', blocked='tower_3811_3'))
        for permission in ('smelting', 'cooking', 'equipment_care', 'farming'):
            self.assertFalse(reachable('tower_3811_3', missing='Production: ' + permission))

    def test_owatatsu_boundary(self):
        mw = setup_multiworld(World, seed=1, options=dict(logic_mode=5,
            mission_catalog_version=1, ending_region=4, enable_soul_orb_checks=1))
        w = mw.worlds[1]
        self.assertEqual(w.regional_profile['goal_location_ids'], [3042, 4104])
        self.assertEqual(len(w.fill_slot_data()['soul_orb_locations']), 188)
        self.assertEqual(len(mw.itempool), 275)
        self.assertFalse(any(r['region'] == 'brigandine' for r in w.regional_profile['milestones']))

    def test_ten_owatatsu_fills(self):
        for seed in range(10):
            mw = setup_multiworld(World, seed=seed, options=dict(logic_mode=5,
                mission_catalog_version=1, ending_region=4, enable_soul_orb_checks=seed % 2))
            distribute_items_restrictive(mw)
            state = CollectionState(mw)
            pending = set(mw.get_locations(1))
            while pending:
                sphere = {l for l in pending if l.can_reach(state)}
                self.assertTrue(sphere)
                pending -= sphere
                for loc in sphere: state.collect(loc.item, prevent_sweep=True)
            self.assertTrue(mw.completion_condition[1](state))

    def test_goal_requires_both_final_checks(self):
        mw = self.make()
        locations = {l.address: l for l in mw.get_locations(1)}
        for tower, dungeon in ((False, False), (True, False), (False, True), (True, True)):
            locations[3052].access_rule = lambda _, v=tower: v
            locations[4105].access_rule = lambda _, v=dungeon: v
            self.assertEqual(mw.completion_condition[1](CollectionState(mw)), tower and dungeon)

    def test_one_hundred_fills(self):
        for seed in range(100):
            with self.subTest(seed=seed):
                mw = self.make(seed, enable_soul_orb_checks=seed % 2)
                distribute_items_restrictive(mw)
                state = CollectionState(mw)
                pending = {l for l in mw.get_locations(1) if l.address is not None}
                while pending:
                    sphere = {l for l in pending if l.can_reach(state)}
                    self.assertTrue(sphere, ', '.join(l.name for l in pending))
                    pending -= sphere
                    for loc in sphere:
                        state.collect(loc.item, prevent_sweep=True)
                self.assertTrue(mw.completion_condition[1](state))
