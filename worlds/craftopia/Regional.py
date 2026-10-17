"""Explicit Geezah playtest selection. Geographic/resource assumptions are experimental."""
import json
from pathlib import Path
from .Expedition import build_profile
from .Milestones import load_catalog,validate_catalog
from .mission_id_mapper import get_id_map


def load_profile():
    return json.loads(Path(__file__).with_name('geezah_pilot_20261016_r1.json').read_text(encoding='utf-8'))


def build_regional_profile(data,item_table):
    canonical=load_profile()
    if not isinstance(data,dict) or data!=canonical:
        raise ValueError('Unreviewed regional selection or prerequisites')
    base=build_profile(item_table)
    selected=set(data['missions']);catalog=get_id_map()
    if any(type(mid) is not int or mid not in catalog for mid in selected):raise ValueError('Unknown mission')
    rows={r['ap_location_id']:r for r in validate_catalog(load_catalog())}
    milestones=[]
    for loc in data['milestones']:
        row=rows.get(loc)
        if row is None or row['region']!='geezah':raise ValueError('Outside regional milestone')
        if row['family']=='category' and not set(row['members'])<=selected:
            raise ValueError('Category has an outside member')
        milestones.append(row)
    nodes=base['nodes'];missions={str(mid):base['missions'][str(mid)] for mid in selected}
    def item(name,count=1):return dict(item=name,count=count)
    def node(name):return dict(node=name)
    # Access estimates are conservative playtest routes, not measured boss thresholds.
    nodes['tower_1']=[[]] # Sand gathering is an explicit local-resource playtest assumption.
    nodes['tower_2']=[[node('tower_1'),item('Progressive Equipment: Body'),node('combat_1')]]
    nodes['tower_3']=[[node('tower_2')]] # Copper ore and placeable torches, not smelted equipment.
    nodes['age_1']=[[node('tower_3')]]
    nodes['dungeon_geezah']=[
        [node('age_1'),item('Production: smelting'),item('Progressive Equipment: Sword',2)],
        [node('age_1'),item('Progressive Equipment: Bow')],
        [node('age_1'),item('Production: smelting'),item('Skill Tree: Tamer'),item('Skill Point',3)]]
    for mid in (72,78,70,15,105,106,107):
        n='regional_mission_'+str(mid)
        nodes[n]=[[node(missions[str(mid)]),node('age_1')]];missions[str(mid)]=n
    rules={}
    for row in milestones:
        family=row['family'];loc=row['ap_location_id']
        if family=='category':
            name='category_'+str(row['source_id'])
            nodes[name]=[[node(missions[str(mid)]) for mid in row['members']]]
        elif family=='tower':name='tower_'+str(row['stage'])
        elif family=='age':name='age_'+str(row['stage'])
        else:name='dungeon_geezah'
        rules[str(loc)]=name
    base.update(profile_id=data['profile_id'],evidence='docs/GEEZAH_PILOT.md',missions=missions,
                milestone_rules=rules,milestones=milestones,goal_location_ids=data['goal_location_ids'],
                smelting_locations=[catalog[mid]['ap_location_id'] for mid in data['smelting_missions']],
                pool=data['pool'])
    return base
