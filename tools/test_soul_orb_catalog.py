import copy
import importlib.util
from pathlib import Path
import unittest
import json
import tempfile
from export_soul_orb_catalog import export

MODULE = Path(__file__).resolve().parents[1] / 'worlds/craftopia/SoulOrbs.py'
spec = importlib.util.spec_from_file_location('soul_orbs_under_test', MODULE)
orbs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(orbs)


class SoulOrbCatalogTests(unittest.TestCase):
    def test_frozen_catalog(self):
        data = orbs.load_catalog()
        rows = orbs.validate_catalog(data)
        self.assertEqual(len(rows), 439)
        self.assertEqual(len({r['ap_location_id'] for r in rows}), 439)
        self.assertTrue(all(r['ap_location_id'] >= 2000 for r in rows))
        captured = next(r for r in rows if r['guid'] == -1595765594)
        self.assertEqual(captured['position'], {'x': -2338.406, 'y': 127.9241, 'z': -1090.284})
        self.assertFalse(data['runtime_ready'])

    def test_order_does_not_reassign_ids(self):
        data = orbs.load_catalog()
        original = {r['guid']: r['ap_location_id'] for r in orbs.validate_catalog(data)}
        data['orbs'].reverse()
        self.assertEqual(original, {r['guid']: r['ap_location_id'] for r in orbs.validate_catalog(data)})

    def test_reject_duplicate_and_malformed_rows(self):
        for field, value in [('guid', True), ('guid', 2**31), ('ap_location_id', True),
                             ('ap_location_id', 1013), ('position', {'x': float('nan'), 'y': 0, 'z': 0})]:
            data = orbs.load_catalog(); data['orbs'][0][field] = value
            with self.assertRaises(ValueError): orbs.validate_catalog(data)
        data = orbs.load_catalog(); data['orbs'][1] = copy.deepcopy(data['orbs'][0])
        with self.assertRaises(ValueError): orbs.validate_catalog(data)

    def test_routing_requires_exact_catalog_and_allowlist(self):
        rows = orbs.validate_catalog(orbs.load_catalog())
        row = next(r for r in rows if r['guid'] == -1595765594)
        slot = {'soul_orb_catalog': orbs.CATALOG_ID, 'soul_orb_schema': 1,
                'soul_orb_locations': {str(row['guid']): row['ap_location_id']}}
        self.assertEqual(orbs.resolve_location(slot, row['guid']), row['ap_location_id'])
        for guid in (True, '00123', 123, 2**31):
            self.assertIsNone(orbs.resolve_location(slot, guid))
        for key, value in [('soul_orb_catalog', 'unknown'), ('soul_orb_schema', True),
                           ('soul_orb_locations', {}), ('soul_orb_locations', {str(row['guid']): 1013})]:
            bad = copy.deepcopy(slot); bad[key] = value
            self.assertIsNone(orbs.resolve_location(bad, row['guid']))

    def test_export_refuses_incomplete_or_invalid_capture(self):
        good = {'schema_version': 5, 'timestamp': '2026-10-15T19:59:54Z',
                'game_assembly_sha256': 'a'*64, 'collector_sha256': 'b'*64, 'collector_version': '1.4.0',
                'world_evidence': {'soul_orbs': {'status': 'collected', 'truncated': False,
                    'observed_count': 1, 'rows': [{'fields': {'_guid': -12}, 'scene_position': {'x': 1, 'y': 2, 'z': 3}}]}}}
        with tempfile.TemporaryDirectory() as directory:
            source, target = Path(directory)/'source.json', Path(directory)/'catalog.json'
            for mutation in ('position', 'count', 'hash'):
                bad = copy.deepcopy(good)
                if mutation == 'position': bad['world_evidence']['soul_orbs']['rows'][0]['scene_position']['x'] = True
                if mutation == 'count': bad['world_evidence']['soul_orbs']['observed_count'] = 2
                if mutation == 'hash': bad['game_assembly_sha256'] = ''
                source.write_text(json.dumps(bad))
                with self.assertRaises(ValueError): export(source, target)
                self.assertFalse(target.exists())
            source.write_text(json.dumps(good)); export(source, target)
            original = target.read_bytes()
            with self.assertRaises(FileExistsError): export(source, target)
            self.assertEqual(target.read_bytes(), original)


if __name__ == '__main__': unittest.main()
