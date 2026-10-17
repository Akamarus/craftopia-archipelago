import unittest
from analyze_world_evidence import orb_state, classify_event, inventory


class WorldEvidenceTests(unittest.TestCase):
    def test_orb_reload_is_history_not_new_collection(self):
        self.assertEqual(orb_state({'_guid': -12, '_firstTook': True, '_taken': False}), 'historical')
        self.assertEqual(orb_state({'_guid': -12, '_firstTook': False, '_taken': True}), 'collected_this_load')
        self.assertEqual(orb_state({'_guid': -12, '_firstTook': False, '_taken': False}), 'uncollected')

    def test_unknown_is_not_uncollected(self):
        for fields in ({}, {'_guid': True, '_firstTook': False, '_taken': False},
                       {'_guid': 1, '_firstTook': 'false', '_taken': False}):
            self.assertEqual(orb_state(fields), 'unknown')

    def test_transition_and_repeat(self):
        event = {'method': 'Oc.OcSoulOrb_Spawner::SetTaken',
                 'before': {'fields': {'_guid': -12, '_firstTook': False, '_taken': False}},
                 'after': {'fields': {'_guid': -12, '_firstTook': False, '_taken': True}}}
        self.assertEqual(classify_event(event)['classification'], 'orb_first_collection_transition')
        event['before']['fields']['_firstTook'] = True
        event['after']['fields']['_firstTook'] = True
        self.assertEqual(classify_event(event)['classification'], 'orb_repeat_collection_transition')
        event['after']['fields']['_guid'] = -13
        self.assertEqual(classify_event(event)['classification'], 'unresolved')

    def test_chest_hash_is_exact_and_field_scoped(self):
        event = {'method': 'Oc.Maps.OcOWTreasureListHandler::OnOpenTreasure',
                 'arguments': [{'name': 'fieldID', 'value': 0}, {'name': 'posHash', 'value': 185247767066515153}]}
        result = classify_event(event)
        self.assertEqual(result['candidate_key'], 'random_chest:0:185247767066515153')
        self.assertEqual(result['classification'], 'random_chest_respawn_notification')
        event['arguments'][1]['value'] = 185247767066515153.0
        self.assertEqual(classify_event(event)['classification'], 'unresolved')

    def test_duplicate_ids_and_incomplete_capture(self):
        row = {'fields': {'_guid': 12, '_firstTook': False, '_taken': False}}
        result = inventory({'world_evidence': {'soul_orbs': {'status': 'collected', 'truncated': True, 'rows': [row, row]}}})
        self.assertEqual(result['soul_orbs']['duplicate_ids'], [12])
        self.assertTrue(result['soul_orbs']['truncated'])
        self.assertEqual(result['random_chests']['status'], 'not_captured')


if __name__ == '__main__':
    unittest.main()
