"""Execute production world methods with minimal AP objects; not a full fill."""
import ast
from pathlib import Path
import types
import unittest
from typing import Dict, List

path=Path(__file__).resolve().parents[1]/'__init__.py'
tree=ast.parse(path.read_text(encoding='utf-8-sig'))
cls=next(n for n in tree.body if isinstance(n,ast.ClassDef) and n.name=='CraftopiaWorld')
cls.bases=[]
cls.body=[n for n in cls.body if isinstance(n,ast.FunctionDef) and n.name in ('set_rules','fill_slot_data')]
ns={'Dict':Dict,'List':List}
exec(compile(ast.Module(body=[cls],type_ignores=[]),str(path),'exec'),ns)

class GoalContract(unittest.TestCase):
    def world(self):
        w=ns['CraftopiaWorld'](); w.player=1
        opts=dict(goal_boss=0,goal_age=0,goal_objectives=1,goal_objective_count=30,goal_boss_choice=0,
                  goal_age_target=5,starting_combat=0,mission_catalog_version=0,skill_points_per_check=1,multiplayer_mode=0,
                  **{'enable_'+n+'_checks':1 for n in ('mission','age','soul_orb','chest','challenge','anubis','shop','boss')})
        w.options=types.SimpleNamespace(**{k:types.SimpleNamespace(value=v) for k,v in opts.items()})
        locations=[types.SimpleNamespace(name='Mission'+str(i),address=i+1) for i in range(53)]
        w.multiworld=types.SimpleNamespace(get_region=lambda *a:types.SimpleNamespace(locations=locations),completion_condition={})
        w.class_to_location={};w.game_id_to_location={};w.location_name_to_id={l.name:l.address for l in locations}
        w.set_rules()
        return w
    def test_victory_is_not_objectives(self):
        w=self.world()
        self.assertFalse(w.multiworld.completion_condition[1](types.SimpleNamespace(has=lambda name,*args:name=='Victory')))
    def test_exact_objective_threshold(self):
        w=self.world(); goal=w.multiworld.completion_condition[1]
        for count,expected in [(0,False),(29,False),(30,True),(31,True)]:
            self.assertEqual(goal(types.SimpleNamespace(has=lambda name,player,required=1: name=='Completed Objective' and count>=required)),expected)
    def test_catalog_contract(self):
        w=self.world(); data=w.fill_slot_data()
        self.assertEqual(data['objective_locations'],{n:'location:'+str(i) for n,i in w.location_name_to_id.items()})
    def test_unsupported_boss_rejected(self):
        w=self.world();w.options.goal_boss.value=1
        with self.assertRaisesRegex(Exception,'not yet supported'):w.set_rules()

if __name__=='__main__':unittest.main()
