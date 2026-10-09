"""Tests for production groups and skill tree items.

Verifies the APWorld item table contains the expected progression items
and that production group resolution works correctly.
"""
import os
import sys
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
# Import Data directly (avoid AP imports).
import importlib.util
data_path = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "Data.py")
spec = importlib.util.spec_from_file_location("craftopia_data", data_path)
data = importlib.util.module_from_spec(spec)
spec.loader.exec_module(data)


class TestProgressionItems(unittest.TestCase):
    def test_skill_tree_items(self):
        table = data.build_item_table()
        expected = [
            "Skill Tree: Magic",
            "Skill Tree: Life",
            "Skill Tree: Trick",
            "Skill Tree: Tamer",
            "Skill Tree: Combat Tier 2",
            "Skill Tree: Combat Tier 3",
            "Skill Tree: Combat Tier 4",
            "Skill Tree: Combat Tier 5",
        ]
        for name in expected:
            self.assertIn(name, table, f"Missing {name}")
            self.assertEqual(table[name].classification, "progression")
            self.assertEqual(table[name].max_count, 1)

    def test_production_items(self):
        table = data.build_item_table()
        expected = [
            "Production: smelting",
            "Production: cooking",
            "Production: farming",
            "Production: livestock_fishing",
            "Production: alchemy",
            "Production: recycling",
            "Production: water_processing",
            "Production: equipment_care",
            "Production: automation",
            "Production: creature_combining",
        ]
        for name in expected:
            self.assertIn(name, table, f"Missing {name}")
            self.assertEqual(table[name].classification, "progression")
            self.assertEqual(table[name].max_count, 1)
        # G10 power should NOT be in the pool (role unconfirmed).
        self.assertNotIn("Production: power", table)

    def test_filler_items(self):
        table = data.build_item_table()
        expected = [
            "Material Bundle: Wood",
            "Material Bundle: Stone",
            "Material Bundle: Iron",
            "Money Bundle: Small",
            "Money Bundle: Medium",
        ]
        for name in expected:
            self.assertIn(name, table, f"Missing {name}")
            self.assertEqual(table[name].classification, "filler")

    def test_item_ids_unique(self):
        table = data.build_item_table()
        ids = [d.ap_id for d in table.values()]
        self.assertEqual(len(ids), len(set(ids)), "Duplicate item IDs")


if __name__ == "__main__":
    unittest.main()
