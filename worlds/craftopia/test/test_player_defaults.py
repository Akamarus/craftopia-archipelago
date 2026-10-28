"""The player example must work without internal compatibility switches."""
import unittest
from pathlib import Path
import yaml
from ap_runtime import load_world
World = load_world()
from test.general import setup_multiworld

class PlayerDefaultsTests(unittest.TestCase):
    def test_player_choices_alone_select_regional_contract(self):
        mw = setup_multiworld(World, options=dict(ending_region=5, starting_combat=0,
            skill_points_per_check=1, enable_soul_orb_checks=1))
        w = mw.worlds[1]
        data = w.fill_slot_data()
        self.assertEqual(data['logic_mode'], 'regional')
        self.assertEqual(data['ending_region'], 'brigandine')
        self.assertEqual(data['goal_policy'], 'regional_tower_and_dungeon')
        self.assertEqual(len(mw.itempool), 330)
        self.assertEqual(w.options.mission_catalog_version.value, 1)
        self.assertEqual(w.options.local_items.value, {'Production: cooking', 'Production: smelting'})

    def test_example_exposes_only_player_choices(self):
        root = Path(__file__).resolve().parents[3]
        example = yaml.safe_load((root / 'worlds/craftopia/Craftopia.yaml').read_text(encoding='utf-8'))
        self.assertEqual(set(example['Craftopia']), {'ending_region', 'starting_combat',
            'skill_points_per_check', 'enable_soul_orb_checks'})
        self.assertEqual(example, yaml.safe_load((root / 'client/Craftopia.yaml').read_text(encoding='utf-8')))
