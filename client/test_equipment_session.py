import unittest
import test_delivery_repair as fixtures
run, cb = fixtures.run, fixtures.cb

class EquipmentSession(unittest.TestCase):
    setUp = fixtures.DeliveryRepair.setUp
    tearDown = fixtures.DeliveryRepair.tearDown
    new = fixtures.DeliveryRepair.new
    connect = fixtures.DeliveryRepair.connect

    def request(self):
        self.b.slot_data.update(equipment_catalog='equipment_20261010_r1', equipment_protocol=1)

    def test_legacy_unchanged(self):
        self.connect()
        self.assertTrue(self.b.equipment_contract_ready())

    def test_old_mod_cannot_activate(self):
        self.connect(); self.request()
        self.assertFalse(self.b.equipment_contract_ready())
        self.assertFalse(self.b.can_deliver_unlocks())

    def test_matching_capability_and_forwarding(self):
        self.connect(); self.request()
        self.b.equipment_capabilities = {self.b.connection_token: 1}
        self.assertTrue(self.b.equipment_contract_ready())
        run(self.b.send_session())
        sessions = [m for m in self.b.mod_ws.sent if m.get('cmd') == 'session']
        self.assertEqual(sessions[-1]['equipment_catalog'], 'equipment_20261010_r1')
        self.assertEqual(sessions[-1]['equipment_protocol'], 1)

    def test_invalid_contracts_hold(self):
        self.connect(); self.b.equipment_capabilities = {self.b.connection_token: 1}
        for catalog, protocol in [('bad',1), ('equipment_20261010_r1',True), (None,1), ([],1), ({},1), ('equipment_20261010_r1',2)]:
            self.b.slot_data.update(equipment_catalog=catalog, equipment_protocol=protocol)
            self.assertFalse(self.b.equipment_contract_ready())

    def test_reviewed_loot_requires_new_capability(self):
        self.connect()
        self.b.slot_data.update(equipment_catalog='equipment_loot_20261016_r1', equipment_protocol=2)
        self.b.equipment_capabilities = {self.b.connection_token: 1}
        self.assertFalse(self.b.equipment_contract_ready())
        self.b.equipment_capabilities[self.b.connection_token] = 2
        self.assertTrue(self.b.equipment_contract_ready())

    def test_max_capability_received_in_handshake(self):
        self.b.ap_connected = True
        self.b.mod_ws = fixtures.Socket()
        self.b.slot_data.update(equipment_catalog='equipment_loot_20261016_r1', equipment_protocol=2)
        run(self.b.try_handshake(self.b.mod_ws, dict(cmd='connect', name='TestPlayer',
            game='Craftopia', unlock_protocol=2, equipment_protocol=1, equipment_protocol_max=2)))
        self.assertTrue(self.b.equipment_contract_ready())
        run(self.b.send_session())
        sessions = [m for m in self.b.mod_ws.sent if m.get('cmd') == 'session']
        self.assertEqual(sessions[-1]['equipment_catalog'], 'equipment_loot_20261016_r1')
        self.request()
        self.assertTrue(self.b.equipment_contract_ready())

    def test_reviewed_r2_contract_and_forwarding(self):
        self.connect()
        self.b.slot_data.update(equipment_catalog='equipment_loot_20261016_r2', equipment_protocol=2)
        self.b.equipment_capabilities = {self.b.connection_token: 1}
        self.assertFalse(self.b.equipment_contract_ready())
        self.b.equipment_capabilities[self.b.connection_token] = 2
        self.assertTrue(self.b.equipment_contract_ready())
        run(self.b.send_session())
        sessions = [m for m in self.b.mod_ws.sent if m.get('cmd') == 'session']
        self.assertEqual(sessions[-1]['equipment_catalog'], 'equipment_loot_20261016_r2')
        self.assertEqual(sessions[-1]['equipment_protocol'], 2)
        self.b.slot_data['equipment_protocol'] = 1
        self.assertFalse(self.b.equipment_contract_ready())

    def test_multiplayer_not_advertised_as_supported(self):
        self.connect(); self.request()
        self.b.equipment_capabilities = {self.b.connection_token: 1}
        self.b.multiplayer_mode = 1
        self.assertFalse(self.b.equipment_contract_ready())

if __name__ == '__main__': unittest.main()
