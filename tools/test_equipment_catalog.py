import copy
import unittest
from export_equipment_catalog import build_catalog


def capture():
    return dict(schema_version=4, items_status='collected', timestamp='test',
                game_assembly_sha256='a' * 64, collector_sha256='b' * 64,
                items=[dict(id=44, status=1, isOldData=False, cantUse=False,
                            wpCategory=5, equipSlot=3, workbenchId=20002,
                            workbenchLv=0, existPrefab=4, inventoryTabId=1)])


class EquipmentExportTests(unittest.TestCase):
    def export(self, data):
        return build_catalog(data, 'c' * 64)

    def test_age_and_category_not_rarity(self):
        data = capture()
        data['items'][0].update(workbenchLv=2, rarity=99, rank=99)
        row = self.export(data)['items'][0]
        self.assertEqual((row['category'], row['tier'], row['minimum_age']), ('Sword', 3, 2))
        self.assertEqual(row['decision'], 'progressive')

    def test_loot_is_explicitly_unsupported(self):
        data = capture()
        data['items'][0].update(workbenchId=20001, workbenchLv=99)
        row = self.export(data)['items'][0]
        self.assertEqual(row['decision'], 'unsupported')
        self.assertNotIn('tier', row)

    def test_tools_keep_vanilla(self):
        data = capture()
        data['items'][0]['wpCategory'] = 1
        self.assertEqual(self.export(data)['items'][0]['decision'], 'vanilla')

    def test_combat_support_is_not_mistaken_for_a_tool(self):
        data = capture()
        data['items'][0].update(wpCategory=38, equipSlot=1)
        self.assertEqual(self.export(data)['items'][0]['decision'], 'unsupported')

    def test_duplicate_and_malformed_rejected(self):
        data = capture()
        data['items'].append(copy.deepcopy(data['items'][0]))
        with self.assertRaises(ValueError): self.export(data)
        for key, value in [('id', True), ('workbenchLv', '0'), ('isOldData', 0), ('existPrefab', None),
                           ('wpCategory', 999), ('equipSlot', 99), ('existPrefab', 99), ('inventoryTabId', -1)]:
            data = capture()
            data['items'][0][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.export(data)

    def test_invalid_identity_and_partial_capture_rejected(self):
        data = capture()
        data['items_status'] = 'failed'
        with self.assertRaises(ValueError): self.export(data)
        with self.assertRaises(ValueError): build_catalog(capture(), 'bad')

    def test_unavailable_combat_and_wrong_prefab_not_supported(self):
        for change in [dict(status=0), dict(isOldData=True), dict(cantUse=True),
                       dict(existPrefab=0), dict(inventoryTabId=0)]:
            data = capture()
            data['items'][0].update(change)
            self.assertEqual(self.export(data)['items'][0]['decision'], 'unsupported')

    def test_sanitized_and_deterministic(self):
        data = capture()
        data['character_guid'] = 123
        data['items'][0]['display_name'] = 'localized text'
        result = self.export(data)
        self.assertNotIn('character_guid', result)
        self.assertNotIn('display_name', result['items'][0])
        self.assertEqual(result, self.export(copy.deepcopy(data)))


if __name__ == '__main__': unittest.main()
