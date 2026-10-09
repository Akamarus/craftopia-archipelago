"""Production-path tests for goal events.

These tests exercise the actual Bridge class methods (check_goal_complete,
record_boss_defeat, record_age_advance, record_objective_complete) with
real temporary ledger files. No copied predicates.
"""
import asyncio
import json
import os
import sys
import tempfile
import unittest

# Import the bridge module.
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# The bridge requires websockets; we mock the module import.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import types
sys.modules["websockets"] = types.ModuleType("websockets")
sys.modules["websockets"].serve = lambda *a, **k: None

import importlib.util
bridge_path = os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "craftopia_bridge.py")
spec = importlib.util.spec_from_file_location("craftopia_bridge", bridge_path)
cb = importlib.util.module_from_spec(spec)
sys.modules["craftopia_bridge"] = cb
spec.loader.exec_module(cb)


def make_bridge(tmpdir, slot_data_overrides=None):
    """Create a Bridge with real temp ledger and mocked args."""
    args = types.SimpleNamespace(
        server="localhost:1234",
        slot="TestSlot",
        session="test",
        reset_ledger=False,
        game_id_dump=False,
    )
    cb.LEDGER_DIR = tmpdir
    args.password = None
    b = cb.Bridge(args)
    b.slot_data = {
        "goal_boss": False, "goal_boss_choice": "boss_123",
        "goal_age": False, "goal_age_target": 5,
        "goal_objectives": False, "goal_objective_count": 30,
        "objective_locations": {str(i):v for i,v in enumerate([
            "mission:Collect:1", "mission:Defeat:2", "mission:Test:1"])}
    }
    if slot_data_overrides: b.slot_data.update(slot_data_overrides)
    return b


class TestProductionGoalPredicate(unittest.TestCase):
    """Test the actual Bridge.check_goal_complete method."""

    def test_no_goals_returns_false(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {
            "goal_boss": False, "goal_age": False, "goal_objectives": False})
        # Production predicate must return False, not True via all([]).
        self.assertFalse(b.check_goal_complete())

    def test_boss_goal_requires_event(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {"goal_boss": True})
        self.assertFalse(b.check_goal_complete())
        b.record_boss_defeat("boss_123")
        self.assertTrue(b.check_goal_complete())

    def test_boss_id_validation(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {"goal_boss": True, "goal_boss_choice": "boss_123"})
        # Wrong boss ID should be rejected.
        result = b.record_boss_defeat("wrong_boss")
        self.assertFalse(result)
        self.assertFalse(b.boss_defeated)
        # Correct boss ID should be accepted.
        result = b.record_boss_defeat("boss_123")
        self.assertTrue(result)
        self.assertTrue(b.boss_defeated)
        self.assertTrue(b.check_goal_complete())

    def test_empty_boss_id_rejected(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {"goal_boss": True})
        self.assertFalse(b.record_boss_defeat(""))
        self.assertFalse(b.record_boss_defeat(None))
        self.assertFalse(b.boss_defeated)

    def test_age_goal(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {"goal_age": True, "goal_age_target": 5})
        self.assertFalse(b.check_goal_complete())
        b.record_age_advance(3)
        self.assertFalse(b.check_goal_complete())
        b.record_age_advance(5)
        self.assertTrue(b.check_goal_complete())

    def test_objective_goal_distinct_counting(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {
            "goal_objectives": True, "goal_objective_count": 2})
        self.assertFalse(b.check_goal_complete())
        b.record_objective_complete("mission:Collect:1")
        self.assertFalse(b.check_goal_complete())
        # Duplicate should not double-count.
        b.record_objective_complete("mission:Collect:1")
        self.assertFalse(b.check_goal_complete())
        b.record_objective_complete("mission:Defeat:2")
        self.assertTrue(b.check_goal_complete())

    def test_empty_objective_id_rejected(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {
            "goal_objectives": True, "goal_objective_count": 1})
        self.assertFalse(b.record_objective_complete(""))
        self.assertFalse(b.record_objective_complete(None))
        self.assertFalse(b.check_goal_complete())

    def test_combined_goals_all_required(self):
        tmp = tempfile.mkdtemp()
        b = make_bridge(tmp, {
            "goal_boss": True,
            "goal_age": True, "goal_age_target": 5,
            "goal_objectives": True, "goal_objective_count": 1,
        })
        # Only boss done.
        b.record_boss_defeat("boss_123")
        self.assertFalse(b.check_goal_complete())
        # Boss + age done.
        b.record_age_advance(5)
        self.assertFalse(b.check_goal_complete())
        # All done.
        b.record_objective_complete("mission:Test:1")
        self.assertTrue(b.check_goal_complete())


if __name__ == "__main__":
    unittest.main()


