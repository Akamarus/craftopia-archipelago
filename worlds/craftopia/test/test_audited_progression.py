import unittest
from ap_runtime import load_world

load_world()
from worlds.craftopia.AuditedProgression import load_catalog, recipe_cost, material_nodes, age_cost, tower_costs
from worlds.craftopia.Progression import ProgressionModel


class AuditTests(unittest.TestCase):
    def test_age_indices_and_source_costs(self):
        gold, materials = age_cost(5)
        self.assertEqual(gold, 200000)
        self.assertEqual(materials[3445], 10)
        with self.assertRaises(ValueError): age_cost(9)

    def test_definitions_and_batches(self):
        data = load_catalog()
        self.assertEqual(len(data['towers']), 10)
        self.assertEqual(sum(len(t['stages']) for t in data['towers']), 30)
        self.assertEqual(len(data['age_transitions']), 9)
        self.assertEqual(tower_costs(3807, 3), {2451:20, 3468:20, 3469:20, 3470:20})
        self.assertEqual(tower_costs(3806, 3), {699:20, 5123:5})
        self.assertEqual(recipe_cost(811, 11), {839: 2})
        self.assertEqual(recipe_cost(5305, 20), {712: 20, 3468: 20})
        self.assertEqual(recipe_cost(2457, 20), {2454: 40, 811: 20})
        with self.assertRaises(ValueError): recipe_cost(5547, 1)
        with self.assertRaises(ValueError): recipe_cost(811, -1)

    def test_recipe_gate_and_no_invented_gathering(self):
        # Owning production cannot invent the source of a filled bucket.
        nodes = {'age': [[]], 'pot': [[]]}
        nodes.update(material_nodes([811], sources={}, ages={1: 'age'}, stations={20015: 'pot'}))
        profile = dict(schema=1, reviewed=True, evidence='test', nodes=nodes, missions={'1':'material_811'})
        model = ProgressionModel(profile, {'Production: cooking'})
        self.assertFalse(model.can_complete(1, lambda _: 1))
        nodes = {'age': [[]], 'pot': [[]], 'bucket': [[]]}
        nodes.update(material_nodes([811], sources={839: [[{'node':'bucket'}]]}, ages={1:'age'}, stations={20015:'pot'}))
        profile['nodes'] = nodes
        model = ProgressionModel(profile, {'Production: cooking'})
        self.assertFalse(model.can_complete(1, lambda _: 0))
        self.assertTrue(model.can_complete(1, lambda _: 1))

    def test_purchase_alternative_does_not_require_manufacturing(self):
        nodes = {'trade': [[]]}
        nodes.update(material_nodes([712], sources={712: [[{'node':'trade'}]]}, ages={}, stations={}))
        model = ProgressionModel(dict(schema=1, reviewed=True, evidence='test', nodes=nodes,
                                      missions={'1':'material_712'}), set())
        self.assertTrue(model.can_complete(1, lambda _: 0))

    def test_cyclic_alternative_sources_cannot_bootstrap(self):
        nodes = material_nodes([698], sources={
            698: [[{'node':'material_746'}]],
            746: [[{'node':'material_698'}]]}, ages={}, stations={})
        # Explicit source references are resolved by the regional profile.
        nodes.update(material_nodes([746], sources={746: [[{'node':'material_698'}]]}, ages={}, stations={}))
        model = ProgressionModel(dict(schema=1, reviewed=True, evidence='test', nodes=nodes,
                                      missions={'1':'material_698'}), set())
        self.assertFalse(model.can_complete(1, lambda _: 99))

    def test_resistance_facts_are_not_region_access_claims(self):
        data = load_catalog()
        self.assertEqual(data['resistance']['788']['cold'], 2)
        self.assertEqual(data['resistance']['5548']['buff_cold'], 999)
        self.assertEqual(data['resistance']['5548']['duration'], 600)
        self.assertNotIn('region_access', data)
