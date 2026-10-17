import copy
import importlib.util
from pathlib import Path
import unittest

path=Path(__file__).resolve().parents[1]/'worlds/craftopia/Milestones.py'

class CatalogTests(unittest.TestCase):
    def setUp(self):
        spec=importlib.util.spec_from_file_location('milestones',path)
        self.m=importlib.util.module_from_spec(spec); spec.loader.exec_module(self.m)
        self.data=self.m.load_catalog()
    def test_catalog(self):
        rows=self.m.validate_catalog(self.data)
        dungeon=next(r for r in rows if r['family']=='dungeon' and r['source_id']==1)
        self.assertEqual(dungeon['ap_location_id'],4100)
    def test_strict(self):
        for field,value in [('source_id',True),('source_id',1.0),('stage','0'),('family','unknown'),('ap_location_id',2000)]:
            data=copy.deepcopy(self.data);data['locations'][0][field]=value
            with self.subTest(field=field,value=value),self.assertRaises(ValueError): self.m.validate_catalog(data)
        for field in ['catalog_id','provenance','frozen_ids']:
            data=copy.deepcopy(self.data);data.pop(field)
            with self.assertRaises(ValueError): self.m.validate_catalog(data)
        data=copy.deepcopy(self.data);data['locations'].append(data['locations'][0])
        with self.assertRaises(ValueError): self.m.validate_catalog(data)
    def test_routes(self):
        slot={'milestone_schema':1,'milestone_catalog':self.m.CATALOG_ID,'milestone_locations':{'milestone:dungeon:1:0':4100}}
        self.assertEqual(self.m.resolve_milestone(slot,'dungeon',1,0),4100)
        self.assertIsNone(self.m.resolve_milestone(slot,'dungeon',True,0))
        self.assertIsNone(self.m.resolve_milestone(slot,'dungeon',2,0))
        slot['milestone_locations']['milestone:dungeon:1:0']=4101
        self.assertIsNone(self.m.resolve_milestone(slot,'dungeon',1,0))

if __name__=='__main__': unittest.main()
