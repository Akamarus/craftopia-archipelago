from dataclasses import FrozenInstanceError, asdict, replace
import unittest
from coop_items import ItemReceipt, validate_receipt, decode_receipt


class ReceiptTests(unittest.TestCase):
    def test_supported_receipts_roundtrip_without_normalizing_content(self):
        for kind, target, amount in [('points', 'skill_points', 3), ('physical', 'Iron', 10),
                ('currency', '3840', 1), ('unlock_equipment', 'Bow', 2),
                ('unlock_production', 'smelting', 1), ('unlock_skill_tree', 'Skill Tree: Magic', 1)]:
            receipt = ItemReceipt(0, 100, kind, target, amount)
            validate_receipt(receipt)
            self.assertEqual(decode_receipt(asdict(receipt)), receipt)
        with self.assertRaises(FrozenInstanceError): receipt.amount = 99

    def test_invalid_receipts_refused_before_persistence(self):
        receipt = ItemReceipt(0, 100, 'points', 'skill_points', 2)
        for changes in ({'index': True}, {'index': -1}, {'index': 2**63}, {'item_id': False},
                        {'item_id': 0}, {'amount': True}, {'amount': 0}, {'kind': 'mystery'},
                        {'target': ' skill_points'}, {'target': ''}, {'target': 'x' * 257},
                        {'target': 'other'}, {'kind': 'unlock_production', 'amount': 2},
                        {'kind': 'unlock_skill_tree', 'amount': 2}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                validate_receipt(replace(receipt, **changes))
        row = asdict(receipt)
        row['extra'] = True
        with self.assertRaises(ValueError): decode_receipt(row)
        del row['extra']
        del row['amount']
        with self.assertRaises(ValueError): decode_receipt(row)


if __name__ == '__main__': unittest.main()
