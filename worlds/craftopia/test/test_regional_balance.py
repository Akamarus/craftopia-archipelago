import unittest
from ap_runtime import load_world
World = load_world()
from test.general import setup_multiworld
from BaseClasses import CollectionState
from Fill import distribute_items_restrictive
from worlds.craftopia.Progression import ProgressionModel


class BalanceTests(unittest.TestCase):
    def make(self, ending=5, seed=1, **options):
        return setup_multiworld(World, seed=seed, options=dict(logic_mode=5,
            mission_catalog_version=1, ending_region=ending, **options))

    def test_each_weapon_route_at_exact_threshold(self):
        for ending, tier in enumerate((2, 3, 4, 5, 6, 7)):
            w = self.make(ending).worlds[1]
            profile = dict(w.regional_profile)
            goal = str(profile['goal_location_ids'][1])
            profile['missions'] = {'0': profile['milestone_rules'][goal]}
            model = ProgressionModel(profile, w.item_table, allow_experimental=True)
            for category in ('Sword', 'Bow', 'Lance', 'SwordTwoHand', 'Gun', 'Katana', 'MagicStaff'):
                steps = profile['equipment_tier_steps'][category]
                required = next(i+1 for i,t in enumerate(steps) if t >= tier)
                def count(name, copies=required):
                    if name.startswith('Progressive Equipment:'):
                        return copies if name == 'Progressive Equipment: '+category else 0
                    return 99
                with self.subTest(ending=ending, category=category):
                    self.assertTrue(model.can_complete(0, count))
                    self.assertFalse(model.can_complete(0, lambda n: count(n,required-1)))
                    if category == 'MagicStaff':
                        self.assertFalse(model.can_complete(0, lambda n: 0 if n=='Skill Tree: Magic' else count(n)))

    def test_life_starts_unlocked_and_later_items_are_trimmed(self):
        mw=self.make(0);w=mw.worlds[1]
        self.assertTrue(CollectionState(mw).has('Skill Tree: Life',1))
        self.assertNotIn('Skill Tree: Life',w.regional_profile['pool'])
        self.assertNotIn('Production: equipment_care',w.regional_profile['pool'])
        self.assertNotIn('Production: water_processing',w.regional_profile['pool'])
        self.assertNotIn('Progressive Equipment: VacuumGun',w.regional_profile['pool'])
        self.assertIn('Progressive Equipment: MagicStaff',w.regional_profile['pool'])

    def test_progressive_copies_have_distinct_deadlines(self):
        mw=self.make();w=mw.worlds[1]
        copies=[i for i in mw.itempool if i.name=='Progressive Equipment: Sword']
        self.assertEqual(len(copies),7)
        self.assertEqual([i.craftopia_deadline for i in copies],
            ['geezah','geezah','millewind','yarden_mountain','yarden_ruin','owatatsu','brigandine'])
        final=mw.get_location('Brigandine Valley: Tower Repair Stage 3',1)
        self.assertFalse(final.item_rule(copies[0]))
        self.assertTrue(final.item_rule(copies[-1]))
        copies[0].player = 2
        self.assertTrue(final.item_rule(copies[0]))

    def test_new_contract_keeps_runtime_equipment_mapping(self):
        w=self.make().worlds[1]
        self.assertIn('20261024',w.regional_profile['profile_id'])
        self.assertEqual(w.fill_slot_data()['equipment_catalog'],'equipment_reviewed_20261024_r1')
        self.assertEqual(w.fill_slot_data()['equipment_protocol'],3)
        self.assertEqual(w.item_table['Progressive Equipment: AssistEquip'].ap_id,38)
        self.assertEqual(w.regional_profile['equipment_tier_steps']['AssistEquip'],[1])
        self.assertEqual(w.regional_profile['pool'].count('Progressive Equipment: AssistEquip'),1)

    def test_real_fills_and_deadlines(self):
        for ending in range(6):
            for seed in range(4):
                with self.subTest(ending=ending,seed=seed):
                    mw=self.make(ending,seed,enable_soul_orb_checks=seed%2)
                    distribute_items_restrictive(mw)
                    self.assertTrue(mw.can_beat_game())
                    w=mw.worlds[1]
                    for loc in mw.get_locations(1):
                        if loc.item and getattr(loc.item,'craftopia_deadline',None):
                            self.assertTrue(loc.item_rule(loc.item))

if __name__=='__main__':unittest.main()
