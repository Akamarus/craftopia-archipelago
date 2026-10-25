"""Yarden endpoint boundaries, material dependencies and real AP fills."""
import unittest
from ap_runtime import load_world

World = load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
from Fill import distribute_items_restrictive
from worlds.craftopia.Progression import ProgressionModel
from worlds.craftopia.Milestones import validate_slot, resolve_milestone


class YardenTests(unittest.TestCase):
    def make(self, ending=2, seed=1, **extra):
        return setup_multiworld(World, seed=seed, options=dict(
            logic_mode=5, mission_catalog_version=1, ending_region=ending, **extra))

    def reachable(self, w, target, missing=None, blocked=None, swords=99):
        p = dict(w.regional_profile)
        p['nodes'] = dict(p['nodes'])
        if blocked:
            p['nodes'][blocked] = [[{'node': blocked}]]
        p['missions'] = {'0': target}
        model = ProgressionModel(p, w.item_table, allow_experimental=True)
        return model.can_complete(0, lambda n: 0 if n == missing else
                                  swords if n == 'Progressive Equipment: Sword' else
                                  0 if swords != 99 and n.startswith('Progressive Equipment:') else 99)

    def test_boundaries_and_slot_dispatch(self):
        for ending, goals, count in ((2, [3022, 4102], 78), (3, [3032, 4103], 82)):
            w = self.make(ending, enable_soul_orb_checks=1).worlds[1]
            p, data = w.regional_profile, w.fill_slot_data()
            self.assertEqual(p['goal_location_ids'], goals)
            self.assertEqual(len(p['missions']) + len(p['milestones']), count)
            self.assertEqual(len(data['soul_orb_locations']), 137)
            self.assertEqual(len(w.multiworld.itempool), count + 137)
            self.assertTrue(p['profile_id'].endswith('_20261024_r1'))
            for loc in w.multiworld.get_locations(1):
                if getattr(loc, 'check_category', None) == 'soul_orb':
                    self.assertFalse(loc.item_rule(w.create_item('Production: smelting')))
                    self.assertTrue(loc.item_rule(w.create_item('Soul Orb Currency')))
            self.assertEqual(data['equipment_catalog'], 'equipment_reviewed_20261024_r1')
            self.assertEqual(data['milestone_catalog'], 'milestones_20261022_r3')
            validate_slot(data)
            self.assertEqual(resolve_milestone(data, 'dungeon', 3, 0), 4102)
            self.assertIsNone(resolve_milestone(data, 'dungeon', 203, 0))
            self.assertIsNone(resolve_milestone(data, 'dungeon', 204, 0))
            self.assertEqual(resolve_milestone(data, 'dungeon', 4, 0), 4103 if ending == 3 else None)
            self.assertFalse(any(r['ap_location_id'] >= 3040 and r['family'] == 'tower' for r in p['milestones']))

    def test_material_routes_and_no_self_lock(self):
        w = self.make(3).worlds[1]
        for n in w.regional_profile['nodes']:
            with self.subTest(node=n): self.assertTrue(self.reachable(w, n))
        self.assertFalse(self.reachable(w, 'material_701', blocked='iron_pickaxe'))
        self.assertTrue(self.reachable(w, 'material_763', blocked='yarden_field'))
        self.assertFalse(self.reachable(w, 'material_2046', 'Production: equipment_care'))
        self.assertTrue(self.reachable(w, 'material_2046', 'Production: farming'))
        self.assertFalse(self.reachable(w, 'tower_3809_3', 'Production: farming'))
        for permission in ('smelting', 'cooking', 'equipment_care'):
            self.assertFalse(self.reachable(w, 'tower_3809_3', 'Production: ' + permission))
        self.assertTrue(self.reachable(w, 'tower_3808_3', blocked='age_3'))
        self.assertFalse(self.reachable(w, 'tower_3809_1', blocked='age_3'))
        self.assertTrue(self.reachable(w, 'age_3', blocked='tower_3809_3'))
        self.assertFalse(self.reachable(w, 'dungeon_yarden', swords=4))
        self.assertTrue(self.reachable(w, 'dungeon_yarden', swords=5))

    def test_goal_needs_selected_pair(self):
        for ending, goals in ((2, (3022, 4102)), (3, (3032, 4103))):
            mw = self.make(ending)
            locations = {l.address: l for l in mw.get_locations(1)}
            for tower, dungeon in ((False, False), (True, False), (False, True), (True, True)):
                locations[goals[0]].access_rule = lambda _, v=tower: v
                locations[goals[1]].access_rule = lambda _, v=dungeon: v
                self.assertEqual(mw.completion_condition[1](CollectionState(mw)), tower and dungeon)

    def test_one_hundred_fills_per_ending(self):
        for ending in (2, 3):
            for seed in range(100):
                with self.subTest(ending=ending, seed=seed):
                    mw = self.make(ending, seed, enable_soul_orb_checks=seed % 2)
                    distribute_items_restrictive(mw)
                    state = CollectionState(mw)
                    pending = {l for l in mw.get_locations(1) if l.address is not None}
                    while pending:
                        sphere = {l for l in pending if l.can_reach(state)}
                        self.assertTrue(sphere, ', '.join(l.name for l in pending))
                        pending -= sphere
                        for loc in sphere: state.collect(loc.item, prevent_sweep=True)
                    self.assertTrue(mw.completion_condition[1](state))
