import json
import unittest
from collections import Counter
from ap_runtime import load_world

World = load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
from Fill import distribute_items_restrictive


class Expedition(unittest.TestCase):
    def make(self, seed=1, **extra):
        return setup_multiworld(World, seed=seed, options=dict(
            logic_mode=3, mission_catalog_version=1, goal_objective_count=60, **extra))

    def test_full_pool_and_wire_contract(self):
        mw = self.make(); world = mw.worlds[1]
        locations = [l for l in mw.get_locations(1) if l.address is not None]
        self.assertEqual(len(locations), 125)
        self.assertEqual(len(mw.itempool), 125)
        sd = json.loads(json.dumps(world.fill_slot_data()))
        self.assertEqual(sd['logic_mode'], 'expedition')
        self.assertEqual(sd['tamer_scope'], 'vanilla')
        counts = Counter(i.name for i in mw.itempool)
        self.assertEqual(len(sd['equipment_tier_steps']), 11)
        for category, steps in sd['equipment_tier_steps'].items():
            self.assertEqual(counts['Progressive Equipment: ' + category], len(steps))
        self.assertGreater(counts['Skill Point'], 0)
        self.assertEqual(len(sd['objective_locations']), 125)
        self.assertEqual(len(set(sd['game_id_to_location'].values())), 125)

    def test_known_permission_dependencies(self):
        mw = self.make(); w = mw.worlds[1]
        by_id = {l.game_id: l for l in mw.get_locations(1) if l.address is not None}
        state = CollectionState(mw)
        for mid in (73, 82, 529, 544, 570):
            self.assertFalse(by_id[mid].can_reach(state), str(mid))
        state.collect(w.create_item('Production: smelting'), prevent_sweep=True)
        self.assertTrue(by_id[73].can_reach(state))
        self.assertFalse(by_id[82].can_reach(state))
        for _ in range(2): state.collect(w.create_item('Progressive Equipment: Shield'), prevent_sweep=True)
        self.assertTrue(by_id[82].can_reach(state))

    def test_real_fill_twenty_seeds(self):
        for seed in range(20):
            mw = self.make(seed); distribute_items_restrictive(mw)
            self.assertTrue(mw.can_beat_game(), str(seed))
            state = mw.get_all_state(False)
            self.assertTrue(all(l.can_reach(state) for l in mw.get_locations(1)))

    def test_late_hunt_has_alternative_combat_routes(self):
        mw = self.make(); w = mw.worlds[1]
        hunt = next(l for l in mw.get_locations(1) if getattr(l, 'game_id', None) == 621)
        state = CollectionState(mw)
        self.assertFalse(hunt.can_reach(state))
        for _ in range(4): state.collect(w.create_item('Progressive Equipment: Sword'), prevent_sweep=True)
        self.assertTrue(hunt.can_reach(state))

    def test_experimental_profile_cannot_enter_verified_model(self):
        from worlds.craftopia.Expedition import build_profile
        from worlds.craftopia.Progression import ProgressionModel
        w = self.make().worlds[1]
        with self.assertRaises(ValueError): ProgressionModel(build_profile(w.item_table), w.item_table)

    def test_shared_rejected(self):
        with self.assertRaises(ValueError): self.make(multiplayer_mode=1)

if __name__ == '__main__': unittest.main()
