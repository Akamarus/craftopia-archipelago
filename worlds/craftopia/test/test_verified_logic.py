"""Real AP integration with explicit synthetic evidence, never game evidence."""
import unittest
from unittest.mock import patch
from ap_runtime import load_world
World = load_world()
from test.general import setup_multiworld as setup_solo_multiworld
from BaseClasses import CollectionState
from worlds.craftopia.mission_id_mapper import get_id_map


class VerifiedLogicTests(unittest.TestCase):
    def test_unavailable_profile_rejects_instead_of_flat_generation(self):
        with self.assertRaisesRegex(ValueError, 'reviewed.*profile'):
            setup_solo_multiworld(World, options={'logic_mode': 1, 'mission_catalog_version': 1})

    def test_real_locations_require_items_and_cycles_stay_closed(self):
        profile = {'schema': 1, 'reviewed': True, 'evidence': 'SYNTHETIC TEST ONLY',
                   'nodes': {'free': [[]], 'smelt': [[{'item': 'Production: smelting', 'count': 1}]]},
                   'missions': {str(mid): 'free' for mid in get_id_map()}}
        profile['missions']['51'] = 'smelt'
        with patch('worlds.craftopia.load_reviewed_profile', return_value=profile):
            mw = setup_solo_multiworld(World, options={'logic_mode': 1, 'mission_catalog_version': 1})
        world = mw.worlds[1]
        check = next(loc for loc in mw.get_locations(1) if getattr(loc, 'game_id', None) == 51)
        state = CollectionState(mw)
        self.assertFalse(check.can_reach(state))
        state.collect(world.create_item('Production: smelting'), prevent_sweep=True)
        self.assertTrue(check.can_reach(state))
        self.assertEqual(world.fill_slot_data()['logic_mode'], 'verified')

    def test_legacy_default_is_unchanged(self):
        mw = setup_solo_multiworld(World, seed=1)
        self.assertEqual(len(mw.itempool), 53)
        self.assertEqual(mw.worlds[1].fill_slot_data()['logic_mode'], 'candidate')

if __name__ == '__main__': unittest.main()
