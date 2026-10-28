import unittest
import test_delivery_repair as repair
run=repair.run
from delivery_state import valid_unlock
class StarterTests(unittest.TestCase):
 setUp=repair.DeliveryRepair.setUp
 tearDown=repair.DeliveryRepair.tearDown
 new=repair.DeliveryRepair.new
 def test_staff_kit_magic_persisted(self):
  self.b.slot_data.update(starting_combat=0,starter_kit_protocol=1)
  self.args.starter_choice='MagicStaff';self.b.ap_connected=True
  run(self.b.apply_starters()); first=list(self.b.ledger['unlocks'])
  self.assertIn({'cmd':'unlock_skill_tree','tree_id':'Magic','receipt_id':-8},first)
  run(self.b.apply_starters());self.assertEqual(first,self.b.ledger['unlocks'])
 def test_magic_receipt_restricted(self):
  self.assertTrue(valid_unlock({'cmd':'unlock_skill_tree','tree_id':'Magic','receipt_id':-8}))
  self.assertFalse(valid_unlock({'cmd':'unlock_skill_tree','tree_id':'Life','receipt_id':-8}))
 def test_old_slot_no_kit_magic(self):
  self.b.slot_data.update(starting_combat=0);self.args.starter_choice='MagicStaff';self.b.ap_connected=True
  run(self.b.apply_starters()); self.assertFalse(any(x['cmd']=='unlock_skill_tree' for x in self.b.ledger['unlocks']))
