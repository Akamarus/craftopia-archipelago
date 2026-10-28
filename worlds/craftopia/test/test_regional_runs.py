"""New regional contract, separate from the frozen dungeon-only pilot."""
import unittest
from ap_runtime import load_world

World = load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
from Fill import distribute_items_restrictive


class RegionalRunTests(unittest.TestCase):
    def test_pilot_multiworld_keeps_opening_smelting_local(self):
        from Options import OptionError
        options = dict(logic_mode=4, mission_catalog_version=1)
        mw = setup_multiworld([World, World], options=options)
        for world in mw.worlds.values():
            self.assertIn('Production: smelting', world.options.local_items.value)
        with self.assertRaisesRegex(OptionError, 'must remain local'):
            setup_multiworld([World, World], options=dict(
                **options, non_local_items={'Production: smelting'}))

    def test_surplus_skill_points_match_network_reconstruction(self):
        mw = self.make(); w = mw.worlds[1]
        surplus = next(i for i in mw.itempool if i.name == 'Skill Point' and not i.advancement)
        server, tracker = CollectionState(mw), CollectionState(mw)
        for state in (server, tracker):
            state.collect(w.create_item('Skill Tree: Magic'), prevent_sweep=True)
        server.collect(surplus, prevent_sweep=True)
        tracker.collect(w.create_item('Skill Point'), prevent_sweep=True)
        self.assertEqual(server.count('Skill Point', 1), tracker.count('Skill Point', 1))
        mission = mw.get_location('Mission 99: Use the skill points to learn a skill', 1)
        self.assertTrue(mission.can_reach(server))
        server.remove(surplus)
        self.assertEqual(server.count('Skill Point', 1), 0)
        self.assertFalse(mission.can_reach(server))

    def test_opening_essentials_stay_local_but_equipment_can_cross_games(self):
        from Options import OptionError
        mw = self.make(); w = mw.worlds[1]
        self.assertEqual(w.options.local_items.value,
                         {'Production: cooking', 'Production: smelting'})
        self.assertNotIn('Progressive Equipment: Sword', w.options.local_items.value)
        with self.assertRaisesRegex(OptionError, 'must remain local'):
            self.make(non_local_items={'Production: cooking'})

    def test_equipment_contract_preserves_prior_profiles(self):
        mw = self.make(); w = mw.worlds[1]
        for revision, contract in [('r2', 'r1'), ('r3', 'r1'), ('r4', 'r2'), ('r5', 'r2')]:
            w.regional_profile['profile_id'] = 'regional_geezah_20261016_' + revision
            self.assertEqual(w.fill_slot_data()['equipment_catalog'], 'equipment_loot_20261016_' + contract)

    def make(self, seed=1, **extra):
        return setup_multiworld(World, seed=seed, options=dict(
            logic_mode=5, mission_catalog_version=1, **extra))

    def test_contract_and_region_boundary(self):
        mw = self.make(); w = mw.worlds[1]; data = w.fill_slot_data()
        self.assertEqual(data['logic_mode'], 'regional')
        self.assertEqual(data['selected_region'], 'geezah')
        self.assertEqual(data['goal_location_ids'], [3002, 4100])
        self.assertEqual(data['goal_policy'], 'regional_tower_and_dungeon')
        self.assertEqual(data['equipment_catalog'], 'equipment_reviewed_20261024_r1')
        self.assertEqual(data['equipment_protocol'], 3)
        self.assertEqual(len(mw.itempool), 68)
        self.assertEqual({r['region'] for r in w.regional_profile['milestones']}, {'geezah'})
        self.assertIn('512', data['game_id_to_location'])
        self.assertIn('513', data['game_id_to_location'])
        self.assertNotIn('soul_orb_locations', data)

    def test_geezah_orbs_and_added_missions(self):
        mw = self.make(enable_soul_orb_checks=1); w = mw.worlds[1]
        data = w.fill_slot_data()
        self.assertEqual(len(mw.itempool), 119)
        self.assertEqual(len(data['soul_orb_locations']), 51)
        self.assertNotIn('714560355', data['soul_orb_locations'])
        from worlds.craftopia.SoulOrbs import resolve_location
        self.assertIsNone(resolve_location(data, 714560355))
        for guid, location in data['soul_orb_locations'].items():
            self.assertEqual(resolve_location(data, int(guid)), location)
        self.assertEqual(sum(i.name == 'Soul Orb Currency' for i in mw.itempool), 51)
        for mid in (34,61,73,80,81,82,87,88,93,99,519,529,530,544):
            self.assertIn(str(mid), data['game_id_to_location'])
        for loc in mw.get_locations(1):
            if getattr(loc, 'check_category', None) == 'soul_orb':
                self.assertFalse(loc.item_rule(w.create_item('Skill Tree: Magic')))
                self.assertTrue(loc.item_rule(w.create_item('Soul Orb Currency')))
        model = w.progression_model
        self.assertFalse(model.can_complete(82, lambda _: 0))
        self.assertFalse(model.can_complete(544, lambda _: 0))
        self.assertFalse(model.can_complete(530, lambda name: int(name != 'Production: farming')))

    def test_recipe_dependencies(self):
        mw = self.make(); w = mw.worlds[1]; state = CollectionState(mw)
        checks = {l.game_id: l for l in mw.get_locations(1) if getattr(l, 'check_category', None) == 'mission'}
        # Furnace construction needs no Smelting unlock; operating it does.
        self.assertTrue(checks[72].can_reach(state))
        self.assertFalse(checks[70].can_reach(state))
        self.assertFalse(checks[78].can_reach(state))
        state.collect(w.create_item('Production: smelting'), prevent_sweep=True)
        self.assertTrue(checks[70].can_reach(state))
        self.assertTrue(checks[78].can_reach(state))
        self.assertTrue(checks[67].can_reach(state))
        state.collect(w.create_item('Production: cooking'), prevent_sweep=True)
        self.assertTrue(checks[67].can_reach(state))

    def test_bucket_route_and_audited_station_construction(self):
        mw = self.make(); w = mw.worlds[1]; state = CollectionState(mw)
        nodes = w.progression_model.nodes
        self.assertEqual(w.regional_profile['profile_id'], 'regional_geezah_20261024_r1')
        self.assertEqual(w.progression_model.missions['105'], 'water_bucket')
        self.assertNotIn('well', nodes)
        self.assertEqual(nodes['blacksmith'], [[{'node': 'material_272'}]])
        self.assertEqual(nodes['glider'], [[{'node': 'material_141'}]])
        self.assertTrue(w.progression_model.can_complete(105, lambda _: 0))
        self.assertFalse(w.progression_model.can_complete(106, lambda _: 0))
        self.assertTrue(w.progression_model.can_complete(106, lambda name: int(name == 'Production: farming')))
        self.assertEqual(w.regional_profile['pool'].count('Progressive Equipment: Bow'), 2)

    def test_goal_requires_both_locations(self):
        mw = self.make(); goal = mw.completion_condition[1]
        tower = mw.get_location('Geezah Plateau: Tower Repair Stage 3', 1)
        dungeon = mw.get_location('Geezah Plateau: Ruin of Beginning', 1)
        state = CollectionState(mw)
        for repaired, cleared in ((False, False), (True, False), (False, True), (True, True)):
            tower.access_rule = lambda _, value=repaired: value
            dungeon.access_rule = lambda _, value=cleared: value
            self.assertEqual(goal(state), repaired and cleared)

    def test_tower_materials_do_not_require_arbitrary_armor(self):
        mw = self.make(); state = CollectionState(mw)
        for stage in (1, 2, 3):
            self.assertTrue(mw.get_location(f'Geezah Plateau: Tower Repair Stage {stage}', 1).can_reach(state))
        w = mw.worlds[1]
        model = w.progression_model
        # Claim tutorial is tied to a completable native category, not its AP reward.
        self.assertEqual(model.missions['513'], 'claim_category')
        self.assertTrue(model.can_complete(513, lambda _: 0))

    def test_unknown_region_and_incompatible_options_rejected(self):
        from worlds.craftopia.RegionalRuns import build_run_profile
        with self.assertRaisesRegex(ValueError, 'not ready'):
            build_run_profile('volcano', self.make().worlds[1].item_table)
        for extra in (dict(multiplayer_mode=1),
                      dict(enable_age_checks=0), dict(enable_boss_checks=0)):
            with self.subTest(extra=extra), self.assertRaises(ValueError): self.make(**extra)

    def test_opening_placements_and_exclusions(self):
        mw = self.make(); w = mw.worlds[1]; state = CollectionState(mw)
        for name in ('Production: smelting', 'Production: cooking'):
            allowed = w.regional_profile['early_item_locations'][name]
            destinations = [l for l in mw.get_locations(1) if l.address in allowed]
            self.assertTrue(destinations)
            self.assertTrue(all(l.can_reach(state) for l in destinations))
            excluded = {l.name for l in destinations}
            with self.assertRaisesRegex(ValueError, 'opening.*destination'):
                self.make(exclude_locations=excluded)
            item = w.create_item(name)
            for loc in mw.get_locations(1):
                if loc.address is not None:
                    self.assertEqual(loc.item_rule(item), loc.address in allowed)
        early = [l for l in mw.get_locations(1) if l.address in allowed]
        with self.assertRaisesRegex(ValueError, 'distinct opening'):
            self.make(exclude_locations={l.name for l in early[1:]})

    def test_feasible_exclusion_fills(self):
        from worlds.generic.Rules import exclusion_rules
        name = 'Mission 30: Equip a stone workbench and place it'
        mw = self.make(exclude_locations={name})
        exclusion_rules(mw, 1, {name})
        distribute_items_restrictive(mw)
        self.assertTrue(mw.can_beat_game())
        item = mw.get_location(name, 1).item
        self.assertFalse(item.advancement or item.useful)

    def test_one_hundred_real_fills(self):
        for seed in range(100):
            with self.subTest(seed=seed):
                mw = self.make(seed, enable_soul_orb_checks=1); w = mw.worlds[1]
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
                for item, allowed in w.regional_profile['early_item_locations'].items():
                    self.assertTrue(all(l.address in allowed for l in mw.get_locations(1) if l.item and l.item.name == item))


if __name__ == '__main__': unittest.main()
