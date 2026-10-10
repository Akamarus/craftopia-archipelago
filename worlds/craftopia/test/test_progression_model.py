import importlib.util
from pathlib import Path
import unittest

PATH = Path(__file__).resolve().parents[1] / 'Progression.py'

class ProgressionModelTests(unittest.TestCase):
    def setUp(self):
        self.assertTrue(PATH.exists(), 'Evidence-bound progression model is required')
        spec = importlib.util.spec_from_file_location('progression_model', PATH)
        self.module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.module)

    def profile(self):
        return {'schema': 1, 'reviewed': True, 'evidence': 'fixture-only',
                'nodes': {'ore': [[]], 'age1': [[{'node': 'ore'}]],
                          'ingot': [[{'node': 'ore'}, {'node': 'age1'}, {'item': 'Production: smelting', 'count': 1}]],
                          'sword': [[{'node': 'ingot'}, {'item': 'Progressive Equipment: Sword', 'count': 2}]]},
                'missions': {'50': 'ore', '72': 'ingot', '99': 'sword'}}

    def model(self, profile=None):
        return self.module.ProgressionModel(profile or self.profile(), {'Production: smelting', 'Progressive Equipment: Sword'})

    def test_permissions_and_resource_dependencies_are_both_required(self):
        model = self.model()
        self.assertFalse(model.can_complete(99, {'Progressive Equipment: Sword': 2}.get))
        self.assertFalse(model.can_complete(99, {'Production: smelting': 1, 'Progressive Equipment: Sword': 1}.get))
        self.assertTrue(model.can_complete(99, {'Production: smelting': 1, 'Progressive Equipment: Sword': 2}.get))

    def test_circular_recipes_do_not_become_free(self):
        p = self.profile(); p['nodes']['ore'] = [[{'node': 'ingot'}]]
        self.assertFalse(self.model(p).can_complete(72, {'Production: smelting': 1}.get))

    def test_alternative_verified_source_breaks_cycle(self):
        p = self.profile(); p['nodes']['ore'] = [[{'node': 'ingot'}], []]
        self.assertTrue(self.model(p).can_complete(72, {'Production: smelting': 1}.get))

    def test_unknown_references_unreviewed_profiles_and_missing_checks_rejected(self):
        for edit in ('unknown_node', 'unknown_item', 'unreviewed', 'bool_count', 'empty_sources'):
            p = self.profile()
            if edit == 'unknown_node': p['nodes']['ore'] = [[{'node': 'invented'}]]
            if edit == 'unknown_item': p['nodes']['ore'] = [[{'item': 'invented', 'count': 1}]]
            if edit == 'unreviewed': p['reviewed'] = False
            if edit == 'bool_count': p['nodes']['ore'] = [[{'item': 'Production: smelting', 'count': True}]]
            if edit == 'empty_sources': p['nodes']['ore'] = []
            with self.subTest(edit=edit), self.assertRaises(ValueError): self.model(p)
        with self.assertRaises(ValueError): self.model().require_coverage([50, 999])
        with self.assertRaises(ValueError): self.model().can_complete(999, {}.get)

    def test_candidate_equipment_tiers_use_age_not_rarity(self):
        row = {'id': 45, 'status': 1, 'isOldData': False, 'cantUse': False,
               'wpCategory': 5, 'equipSlot': 0, 'workbenchId': 20011, 'workbenchLv': 1,
               'rank': 99, 'rarity': 99}
        result = self.module.equipment_candidate(row)
        self.assertEqual(result, {'item_id': 45, 'category': 'Sword', 'tier': 2, 'minimum_age': 1})
        self.assertIsNone(self.module.equipment_candidate(dict(row, workbenchId=0)))
        self.assertIsNone(self.module.equipment_candidate(dict(row, workbenchId=20001)))
        self.assertIsNone(self.module.equipment_candidate(dict(row, status=0)))
        self.assertIsNone(self.module.equipment_candidate(dict(row, wpCategory=1)))

if __name__ == '__main__': unittest.main()
