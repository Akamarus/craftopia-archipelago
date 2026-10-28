"""Real AP 0.6.8 generation and goal checks; never copies into an AP install."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from ap_runtime import load_world
World=load_world()
from test.general import setup_multiworld as setup_solo_multiworld
from BaseClasses import CollectionState
mw=setup_solo_multiworld(World,seed=1,options={'logic_mode':0,'mission_catalog_version':0})
w=mw.worlds[1]
network=[l for l in mw.get_locations(1) if l.address is not None]
events=[l for l in mw.get_locations(1) if l.address is None]
assert len(network)==53 and len(events)==53
# v1.4.0+: pool contains skill trees, production, filler, not just Skill Points.
pool_names = [i.name for i in mw.itempool]
assert len(mw.itempool)==53, "pool size wrong"
skill_trees = [n for n in pool_names if n.startswith("Skill Tree:")]
production = [n for n in pool_names if n.startswith("Production:")]
assert len(skill_trees)==8, "skill tree count wrong"
assert len(production)==10, "production count wrong"
others = [n for n in pool_names if not n.startswith("Skill Tree:") and not n.startswith("Production:")]
assert all(n=="Skill Point" or "Bundle" in n for n in others), "unexpected filler"
assert all(l.parent_region is not None for l in mw.get_locations(1))
goal=mw.completion_condition[1]
state=CollectionState(mw)
assert not goal(state)
state.collect(w.create_item('Victory'),prevent_sweep=True)
assert not goal(state)
for i in range(29):state.collect(w.create_item('Completed Objective'),prevent_sweep=True)
assert not goal(state)
state.collect(w.create_item('Completed Objective'),prevent_sweep=True)
assert goal(state)
assert len(w.fill_slot_data()['objective_locations'])==53
state=CollectionState(mw)
state.sweep_for_advancements()
assert goal(state), 'candidate mission events must be reachable in current flat logic'
print('PASS AP 0.6.8: 53 network locations, 53 local events, 53 pool items; Victory cannot win; 29/30 threshold; reachable event sweep')
print('Limit: flat candidate-class logic does not prove runtime mission availability.')
