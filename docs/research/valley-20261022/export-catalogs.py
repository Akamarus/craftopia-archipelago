"""Additive frozen catalogs from retained installed and runtime evidence; run from any cwd."""
from pathlib import Path
import copy,hashlib,json,sys
REPO=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(REPO/'tools'))
from export_yarden_orbs import field_at
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
write=lambda p,d:p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
prior_path=REPO/'worlds/craftopia/milestones_20261022_r3.json';prior=read(prior_path);result=copy.deepcopy(prior)
identity_path=OUT/'identity-evidence.json';tower_path=OUT/'tower-orb-evidence.json'
identity=read(identity_path);evidence=read(tower_path)
assert not identity['decode_errors']
assert {(r['data']['sourceId'],r['data']['battleDungeonType']) for r in identity['dungeons']}=={(5,0),(6,0),(205,1),(206,1)}
assert len(identity['entrances'])==2 and all(r['data']['_underDevelop']==0 for r in identity['entrances'])
result['catalog_id']='milestones_20261022_r4'
result['provenance']['valley_identity_evidence_sha256']=sha(identity_path)
result['provenance']['valley_tower_orb_evidence_sha256']=sha(tower_path)
for iid,region,label,first in ((3810,'owatatsu','Owatatsu Archipelago',3040),(3811,'brigandine','Brigandine Valley',3050)):
 tower=next(r for r in evidence['towers'] if r['definition']['item_id']==iid)
 assert len(tower['definition']['stages'])==3
 for s in tower['definition']['stages']:
  raw=s['raw_stage_index'];assert not s['costs']['truncated']
  result['locations'].append(dict(family='tower',source_id=tower['guid'],stage=raw+1,ap_location_id=first+raw,ap_location_name=f'{label}: Tower Repair Stage {raw+1}',region=region,definition_id=iid,position=tower['scene_position'],costs=s['costs']['rows'],evidence='F9 5ebf44fff52e4347844ea8c1c2226d33, 2026-10-22: loaded tower identity, position and costs; live repair transition pending. docs/research/valley-20261022.'))
for source,apid,region,label,name in ((5,4104,'owatatsu','Owatatsu Archipelago','Labyrinth of Torrent'),(6,4105,'brigandine','Brigandine Valley','Old Garden of Massacre')):
 result['locations'].append(dict(family='dungeon',source_id=source,stage=0,ap_location_id=apid,ap_location_name=f'{label}: {name}',region=region,evidence=f'Installed Story definition and active entrance; user workbook tower binding. docs/research/valley-20261022. Repeat source {source+200} excluded; live completion pending.'))
audit_path=REPO/'worlds/craftopia/progression_audit_20261016_r2.json';audit=read(audit_path)
age=next(a for a in audit['age_transitions'] if a['from_world_level']==3)
assert age['to_world_level']==4 and age['isEnabled'] and age['gold']==20000
result['locations'].append(dict(family='age',source_id=0,stage=4,ap_location_id=4003,ap_location_name='Age Evolution: World Age 5',region='owatatsu',evidence='Audited raw3 -> raw4 transition. Displayed Age5; Meadow materials and20000gold.'))
assert result['locations'][:len(prior['locations'])]==prior['locations']
assert len(result['locations'])==len(prior['locations'])+9
ids=[r['ap_location_id'] for r in result['locations']];assert len(ids)==len(set(ids)) and 4004 not in ids
milestone_path=REPO/'worlds/craftopia/milestones_20261022_r4.json';write(milestone_path,result)
envp=REPO/'docs/research/2026-10-16/environment.json';orbp=REPO/'worlds/craftopia/soul_orbs_20261015_r1.json'
env=read(envp);assets={r['_asset_path_id']:r for r in env['world_assets']};fields=[assets[r['m_PathID']] for r in assets[41584]['_dataArray']];all_orbs=read(orbp)['orbs'];seen=set()
for iid,region,field in ((3810,'owatatsu',41591),(3811,'brigandine',41585)):
 tower=next(r for r in evidence['towers'] if r['definition']['item_id']==iid)
 assert field_at(tower['scene_position'],fields)==field and assets[field]['envDmgType']==0
 rows=[{k:r[k] for k in ('guid','ap_location_id','position')} for r in all_orbs if field_at(r['position'],fields)==field]
 assert len(rows)==len({r['guid'] for r in rows})==51
 assert not seen.intersection(r['guid'] for r in rows);seen.update(r['guid'] for r in rows)
 assert rows==next(r for r in evidence['regions'] if r['tower_definition_id']==iid)['locations']
 for row in rows:
  p=row['position']
  for dx in (-.01,.01):
   for dy in (-.01,.01):
    for dz in (-.01,.01):assert field_at(dict(x=p['x']+dx,y=p['y']+dy,z=p['z']+dz),fields)==field
 orb_result=dict(schema=1,region=region,traversal_verified=False,evidence='docs/research/valley-20261022/orb-membership.md',native_field_id=field,native_field_name=assets[field]['m_Name'],source_sha256={p.relative_to(REPO).as_posix():sha(p) for p in (envp,orbp,milestone_path)},locations=rows)
 write(REPO/f'worlds/craftopia/{region}_orbs_20261022_r1.json',orb_result)
print(f'Preserved {len(prior["locations"])} prior milestones; added 9. Exported 51 Owatatsu and 51 Brigandine native orbs; frozen IDs, disjointness and perturbation checks passed.')
