"""Generation tests for the Craftopia world.

These run without the full Archipelago test suite; they verify the world
package structure, data integrity, and basic generation logic.
For full integration, run with Archipelago's test framework.
"""
import json
import os
import sys
import unittest

WORLD_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, WORLD_DIR)


class TestMissionData(unittest.TestCase):
    def test_missions_json_valid(self):
        path = os.path.join(WORLD_DIR, "data", "missions.json")
        self.assertTrue(os.path.exists(path), "missions.json must exist")
        with open(path) as f:
            data = json.load(f)
        self.assertIn("missions", data)
        self.assertIsInstance(data["missions"], list)

    def test_missions_have_required_fields(self):
        path = os.path.join(WORLD_DIR, "data", "missions.json")
        with open(path) as f:
            data = json.load(f)
        for m in data["missions"]:
            self.assertIn("name", m, f"mission missing name: {m}")
            # game_id may be null (unverified), but the key must exist
            self.assertIn("game_id", m, f"mission missing game_id: {m}")
            if m["game_id"] is not None:
                self.assertIsInstance(m["game_id"], int)

    def test_mission_names_unique(self):
        path = os.path.join(WORLD_DIR, "data", "missions.json")
        with open(path) as f:
            data = json.load(f)
        names = [m["name"] for m in data["missions"]]
        self.assertEqual(len(names), len(set(names)), "mission names must be unique")


class TestDataModule(unittest.TestCase):
    def test_build_item_table(self):
        from Data import build_item_table
        items = build_item_table()
        self.assertIn("Skill Point", items)
        self.assertIn("Victory", items)
        # IDs unique
        ids = [d.ap_id for d in items.values()]
        self.assertEqual(len(ids), len(set(ids)))

    def test_build_location_table_uses_class_key(self):
        from Data import build_location_table
        missions = [
            {"name": "Craft Mission", "game_id": None,
             "region": "Overworld", "class": "Oc.Missions.ItemCraftMission"},
            {"name": "No Class", "game_id": 42,
             "region": "Overworld", "class": None},
        ]
        locs = build_location_table(missions)
        # Class name is the stable key; null game_id is OK.
        self.assertIn("Craft Mission", locs)
        self.assertEqual(locs["Craft Mission"].mission_class,
                         "Oc.Missions.ItemCraftMission")
        # Missing class -> skipped (no stable key).
        self.assertNotIn("No Class", locs)

    def test_location_ids_unique(self):
        from Data import build_location_table
        missions = [
            {"name": f"Mission {i}", "game_id": 1000 + i, "region": "Overworld", "class": "Test"}
            for i in range(10)
        ]
        locs = build_location_table(missions)
        ids = [d.ap_id for d in locs.values()]
        self.assertEqual(len(ids), len(set(ids)))


if __name__ == "__main__":
    unittest.main(verbosity=2)
