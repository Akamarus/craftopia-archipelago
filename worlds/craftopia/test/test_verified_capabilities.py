import unittest
from ap_runtime import load_world

load_world()
from worlds.craftopia.AuditedProgression import material_nodes, protection_nodes, repair_nodes, evolution_node
from worlds.craftopia.Progression import ProgressionModel


class VerifiedCapabilitiesTests(unittest.TestCase):
    def reachable(self, nodes, target, owned=()):
        model = ProgressionModel(dict(schema=1, reviewed=True, evidence='test',
            nodes=nodes, missions={'1': target}), {'Production: cooking', 'Production: alchemy'})
        return model.can_complete(1, lambda name: int(name in owned))

    def test_torch_cold_does_not_supply_heat_or_stack(self):
        nodes = {'field': [[]]}
        nodes.update(protection_nodes({'cold2': ('cold', 2), 'cold3': ('cold', 3),
            'heat': ('fire', 1)}, candidates=[788], sources={746: [[]], 2454: [[]]},
            ages={0: 'field'}, stations={20002: 'field'}))
        self.assertTrue(self.reachable(nodes, 'cold2'))
        self.assertFalse(self.reachable(nodes, 'cold3'))
        self.assertFalse(self.reachable(nodes, 'heat'))

    def test_amulet_requires_age_station_and_all_materials(self):
        sources = {764: [[]], 683: [[]], 695: [[]], 713: [[]]}
        def build(ages, materials):
            return dict(field=[[]], **protection_nodes({'heat': ('fire', 1)},
                candidates=[2307], sources=materials, ages=ages, stations={20017:'field'}))
        self.assertFalse(self.reachable(build({}, sources), 'heat'))
        self.assertFalse(self.reachable(build({3:'field'}, {k:v for k,v in sources.items() if k!=713}), 'heat'))
        self.assertTrue(self.reachable(build({3:'field'}, sources), 'heat'))

    def test_potion_requires_repeatable_recipe_and_production(self):
        nodes = {'field': [[]]}
        nodes.update(protection_nodes({'heat': ('fire', 3)}, candidates=[5546],
            sources={740:[[]],3445:[[]],839:[[]]}, ages={1:'field',4:'field'},
            stations={20015:'field',20012:'field'}))
        for owned in [(), ('Production: cooking',), ('Production: alchemy',)]:
            self.assertFalse(self.reachable(nodes, 'heat', owned))
        self.assertTrue(self.reachable(nodes, 'heat', ('Production: cooking','Production: alchemy')))

    def test_unreviewed_candidates_and_hellfire_rejected(self):
        for candidate in (2511, 5547, 6161):
            with self.assertRaises(ValueError):
                protection_nodes({'heat':('fire',1)}, candidates=[candidate],sources={},ages={},stations={})
        with self.assertRaises(ValueError):
            protection_nodes({'hell':('hellfire',50)}, candidates=[788],sources={},ages={},stations={})

    def test_repair_sequence_and_age_currency_cannot_bootstrap(self):
        nodes = {'base':[[]], 'money':[[{'node':'money'}]]}
        nodes.update(repair_nodes(3806, sources={}, ages={0:'base'}, stations={20002:'base',20011:'base'}))
        self.assertFalse(self.reachable(nodes,'tower_3806_3'))
        with self.assertRaises(ValueError):
            evolution_node(1, tower_node='tower_3806_3', currency_node=None)
        node = evolution_node(1,tower_node='tower_3806_3',currency_node='money')
        self.assertIn({'node':'money'},node[0])


if __name__ == '__main__': unittest.main()
