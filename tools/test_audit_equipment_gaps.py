import copy
import unittest
from audit_equipment_gaps import audit


class EquipmentAuditTests(unittest.TestCase):
    def fixture(self):
        row = dict(id=6031, decision='unsupported', category='Sword', status=1,
                   wpCategory=5, equipSlot=3, workbenchId=20001, workbenchLv=0,
                   existPrefab=4, inventoryTabId=1, isOldData=False, cantUse=False)
        live = dict(row, display_name="Goblin's Club", atk=100, matk=50, def_=0)
        return dict(items=[row], game_assembly_sha256='a'*64), dict(
            items=[live], items_status='collected', game_assembly_sha256='a'*64)

    def test_no_automatic_tier_from_rank_or_loot_age(self):
        catalog, capture = self.fixture()
        capture['items'][0]['rank'] = 2
        result = audit(catalog, capture)
        row = result['entries'][0]
        self.assertEqual(row['review_status'], 'needs_individual_review')
        self.assertIsNone(row['proposed_tier'])
        self.assertIsNone(row['proposed_minimum_age'])
        self.assertEqual(row['reason'], 'loot_only')

    def test_changed_metadata_not_eligible(self):
        catalog, capture = self.fixture()
        capture['items'][0]['wpCategory'] = 6
        self.assertEqual(audit(catalog, capture)['entries'][0]['reason'], 'metadata_changed')

    def test_missing_and_unavailable_are_separate(self):
        catalog, capture = self.fixture()
        capture['items'] = []
        self.assertEqual(audit(catalog, capture)['entries'][0]['reason'], 'missing_from_capture')
        catalog, capture = self.fixture()
        catalog['items'][0]['status'] = capture['items'][0]['status'] = 0
        self.assertEqual(audit(catalog, capture)['entries'][0]['reason'], 'unavailable_metadata')

    def test_mismatched_build_and_duplicate_ids_rejected(self):
        catalog, capture = self.fixture()
        capture['game_assembly_sha256'] = 'b'*64
        with self.assertRaises(ValueError): audit(catalog, capture)
        catalog, capture = self.fixture()
        capture['items'].append(copy.deepcopy(capture['items'][0]))
        with self.assertRaises(ValueError): audit(catalog, capture)

    def test_private_capture_data_not_exported(self):
        catalog, capture = self.fixture()
        capture['character_guid'] = 123
        capture['items'][0]['private_note'] = 'secret'
        output = str(audit(catalog, capture))
        self.assertNotIn('secret', output)
        self.assertNotIn('character_guid', output)


if __name__ == '__main__': unittest.main()
