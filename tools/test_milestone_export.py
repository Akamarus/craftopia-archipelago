import unittest
from export_milestone_catalog import category_evidence


class ExportTests(unittest.TestCase):
    def capture(self):
        return dict(schema_version=5, game_assembly_sha256='a'*64,
                    missions_status='collected', missions=[dict(id=50,category=2,observed=True)],
                    world_evidence=dict(mission_categories=dict(status='collected',truncated=False,
                        rows=[dict(fields=dict(id=2,title='Trees')),dict(fields=dict(id=3,title='Empty'))])))

    def test_complete_members_only(self):
        rows=category_evidence(self.capture(),{50})
        self.assertEqual(rows[0]['members'],[50])
        self.assertTrue(rows[0]['membership_complete'])
        self.assertFalse(rows[1]['membership_complete'])

    def test_unknown_or_duplicate_member_rejected(self):
        d=self.capture()
        self.assertFalse(category_evidence(d,set())[0]['membership_complete'])
        d['missions'].append(d['missions'][0])
        with self.assertRaises(ValueError): category_evidence(d,{50})

    def test_incomplete_capture_rejected(self):
        for field,value in [('truncated',True),('status','error')]:
            d=self.capture();d['world_evidence']['mission_categories'][field]=value
            with self.assertRaises(ValueError): category_evidence(d,{50})

if __name__=='__main__': unittest.main()
