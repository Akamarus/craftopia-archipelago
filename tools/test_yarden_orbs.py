import json
import unittest
from export_yarden_orbs import export, OUTPUT, inside, field_at


class YardenOrbTests(unittest.TestCase):
    def test_reproducible_membership(self):
        self.assertEqual(export(), json.loads(OUTPUT.read_text(encoding='utf-8')))

    def test_strict_cylinder_and_negative_height(self):
        c = dict(x=0, y=0, z=0)
        self.assertTrue(inside(dict(x=0, y=1, z=0), c, 2, 2))
        for p in (dict(x=2, y=1, z=0), dict(x=0, y=0, z=0), dict(x=0, y=2, z=0)):
            self.assertFalse(inside(p, c, 2, 2))
        self.assertTrue(inside(dict(x=0, y=-1, z=0), c, -2, 2))

    def test_priority_ties_and_disabled_fields(self):
        def f(i, weight):
            return dict(_asset_path_id=i, _weight=weight, _center=dict(x=0,y=5,z=0),
                        _zoneList=[dict(CenterOffset=dict(x=0,y=0,z=0),Height=2,Radius=2)])
        p = dict(x=0,y=1,z=0)
        self.assertEqual(field_at(p, [f(1,1),f(2,2)]),2)
        self.assertEqual(field_at(p, [f(1,2),f(2,2)]),1)
        self.assertIsNone(field_at(p,[f(1,-1)]))
