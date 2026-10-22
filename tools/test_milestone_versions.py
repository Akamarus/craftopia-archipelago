import copy
import importlib.util
from pathlib import Path
import unittest

path=Path(__file__).resolve().parents[1]/'worlds/craftopia/Milestones.py'
spec=importlib.util.spec_from_file_location('milestones_versions',path)
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
OLD='milestones_20261016_r1'
NEW='milestones_20261017_r2'

class CatalogVersionTests(unittest.TestCase):
    def test_yarden_is_additive_and_isolated(self):
        version = 'milestones_20261022_r3'
        old = m.validate_catalog(m.load_catalog(NEW))
        rows = m.validate_catalog(m.load_catalog(version))
        self.assertEqual(rows[:len(old)], old)
        self.assertEqual(len(rows) - len(old), 9)
        for source, location in ((3, 4102), (4, 4103)):
            slot = {'milestone_schema': 1, 'milestone_catalog': version,
                    'milestone_locations': {f'milestone:dungeon:{source}:0': location},
                    'goal_location_ids': [location]}
            m.validate_slot(slot)
            self.assertEqual(m.resolve_milestone(slot, 'dungeon', source, 0), location)
            self.assertIsNone(m.resolve_milestone(slot, 'dungeon', source + 200, 0))
            slot['milestone_catalog'] = NEW
            with self.assertRaises(ValueError): m.validate_slot(slot)
        towers = {r['definition_id']: r for r in rows if r['family'] == 'tower'}
        self.assertEqual(towers[3808]['source_id'], -793448649)
        self.assertEqual(towers[3809]['source_id'], 1309797564)

    def slot(self,catalog,source=28,location=4228):
        return {'milestone_schema':1,'milestone_catalog':catalog,
                'milestone_locations':{f'milestone:category:{source}:0':location},'goal_location_ids':[location]}
    def test_additive_catalog(self):
        old=m.validate_catalog(m.load_catalog(OLD));new=m.validate_catalog(m.load_catalog(NEW))
        self.assertEqual(m.load_catalog()['catalog_id'],NEW)
        self.assertEqual(new[:len(old)],old)
        rows={r['source_id']:r for r in new if r['family']=='category'}
        self.assertEqual(rows[28]['members'],[529,530])
        self.assertEqual(rows[141]['members'],[512,513])
        self.assertEqual(rows[28]['ap_location_id'],4228)
        self.assertEqual(rows[141]['ap_location_id'],4341)
    def test_dispatch_isolation(self):
        for version in (OLD,NEW):
            slot=self.slot(version,2,4202)
            m.validate_slot(slot)
            self.assertEqual(m.resolve_milestone(slot,'category',2,0),4202)
        for source,location in ((28,4228),(141,4341)):
            m.validate_slot(self.slot(NEW,source,location))
            self.assertEqual(m.resolve_milestone(self.slot(NEW,source,location),'category',source,0),location)
            with self.assertRaises(ValueError):m.validate_slot(self.slot(OLD,source,location))
            self.assertIsNone(m.resolve_milestone(self.slot(OLD,source,location),'category',source,0))
    def test_dungeon_dispatch(self):
        slot={'milestone_schema':1,'milestone_catalog':NEW,
              'milestone_locations':{'milestone:dungeon:2:0':4101},'goal_location_ids':[4101]}
        m.validate_slot(slot)
        self.assertEqual(m.resolve_milestone(slot,'dungeon',2,0),4101)
        self.assertIsNone(m.resolve_milestone(slot,'dungeon',202,0))
        slot['milestone_catalog']=OLD
        with self.assertRaises(ValueError):m.validate_slot(slot)
        self.assertIsNone(m.resolve_milestone(slot,'dungeon',2,0))
    def test_unknown_versions(self):
        for version in ('unknown','../milestones_20261016_r1',None,[],{}):
            with self.subTest(version=version):
                with self.assertRaises(ValueError):m.load_catalog(version)
                with self.assertRaises(ValueError):m.validate_slot(self.slot(version))
                self.assertIsNone(m.resolve_milestone(self.slot(version),'category',28,0))

if __name__=='__main__':unittest.main()
