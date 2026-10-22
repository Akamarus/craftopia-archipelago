"""Frozen, opt-in progression milestone identities; no AP imports."""
import json
from pathlib import Path

LEGACY_CATALOG_ID='milestones_20261016_r1'
CATALOG_ID='milestones_20261017_r2'
YARDEN_CATALOG_ID='milestones_20261022_r3'
SUPPORTED_CATALOGS=(LEGACY_CATALOG_ID,CATALOG_ID,YARDEN_CATALOG_ID)
RANGES={'tower':(3000,3999),'age':(4000,4099),'dungeon':(4100,4199),'category':(4200,4999)}

def integer(value,low,high):
    return type(value) is int and low<=value<=high

def key(family,source_id,stage):
    return f'milestone:{family}:{source_id}:{stage}'

def load_catalog(catalog_id=CATALOG_ID):
    if catalog_id not in SUPPORTED_CATALOGS: raise ValueError('Unsupported milestone catalog')
    return json.loads(Path(__file__).with_name(catalog_id+'.json').read_text(encoding='utf-8'))

def validate_catalog(data):
    if (not isinstance(data,dict) or data.get('catalog_id') not in SUPPORTED_CATALOGS or
        type(data.get('schema')) is not int or data['schema']!=1 or data.get('frozen_ids') is not True or
        not isinstance(data.get('provenance'),dict) or not data['provenance'].get('assembly_sha256')):
        raise ValueError('Invalid milestone catalog header')
    rows=data.get('locations')
    if not isinstance(rows,list) or not rows: raise ValueError('Missing milestone rows')
    ids,names,keys=set(),set(),set()
    for row in rows:
        if not isinstance(row,dict): raise ValueError('Invalid milestone row')
        family=row.get('family'); source=row.get('source_id'); stage=row.get('stage'); loc=row.get('ap_location_id'); name=row.get('ap_location_name')
        if family not in RANGES or not integer(source,-2147483648,2147483647) or not integer(stage,0,99):
            raise ValueError('Invalid milestone identity')
        if (family in ('dungeon','category') and (source<0 or stage!=0) or
            family=='age' and (source!=0 or stage==0) or family=='tower' and stage==0):
            raise ValueError('Invalid milestone stage')
        if not integer(loc,*RANGES[family]) or not isinstance(name,str) or not name.strip() or name!=name.strip():
            raise ValueError('Invalid milestone ID/name')
        if family=='category':
            members=row.get('members')
            if (not isinstance(members,list) or not members or any(not integer(m,0,2147483647) for m in members)
                or len(members)!=len(set(members))): raise ValueError('Invalid category membership')
        k=key(family,source,stage)
        if loc in ids or name in names or k in keys: raise ValueError('Duplicate milestone')
        if not row.get('evidence') or not row.get('region'): raise ValueError('Missing milestone evidence/region')
        ids.add(loc);names.add(name);keys.add(k)
    return rows

def validate_slot(data):
    if (not isinstance(data,dict) or data.get('milestone_catalog') not in SUPPORTED_CATALOGS or
        type(data.get('milestone_schema')) is not int or data['milestone_schema']!=1):
        raise ValueError('Unsupported milestone session')
    allowed=data.get('milestone_locations')
    if not isinstance(allowed,dict) or not allowed: raise ValueError('Empty milestone session')
    frozen={key(r['family'],r['source_id'],r['stage']):r['ap_location_id'] for r in validate_catalog(load_catalog(data['milestone_catalog']))}
    if any(type(v) is not int or frozen.get(k)!=v for k,v in allowed.items()): raise ValueError('Unknown milestone mapping')
    goals=data.get('goal_location_ids')
    if (not isinstance(goals,list) or not goals or any(type(g) is not int or g not in allowed.values() for g in goals)
        or len(goals)!=len(set(goals))): raise ValueError('Invalid milestone goal')
    return allowed

def resolve_milestone(slot_data,family,source_id,stage):
    if family not in RANGES or not integer(source_id,-2147483648,2147483647) or not integer(stage,0,99): return None
    if not isinstance(slot_data,dict) or slot_data.get('milestone_catalog') not in SUPPORTED_CATALOGS or type(slot_data.get('milestone_schema')) is not int or slot_data['milestone_schema']!=1: return None
    allowed=slot_data.get('milestone_locations')
    if not isinstance(allowed,dict): return None
    loc=allowed.get(key(family,source_id,stage))
    if not integer(loc,*RANGES[family]): return None
    return loc if any(r['family']==family and r['source_id']==source_id and r['stage']==stage and r['ap_location_id']==loc for r in validate_catalog(load_catalog(slot_data['milestone_catalog']))) else None
