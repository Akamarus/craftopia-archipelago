from pathlib import Path
import json,hashlib,sys
root=Path.cwd();repo=root/'public-repo/craftopia-archipelago';out=repo/'docs/research/valley-20261022';sys.path.insert(0,str(repo/'tools'))
from export_yarden_orbs import field_at
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
envp=repo/'docs/research/2026-10-16/environment.json';orbp=repo/'worlds/craftopia/soul_orbs_20261015_r1.json'
env=json.loads(envp.read_text(encoding='utf-8-sig'));assets={r['_asset_path_id']:r for r in env['world_assets']};fields=[assets[r['m_PathID']] for r in assets[41584]['_dataArray']];orbs=json.loads(orbp.read_text(encoding='utf-8'))['orbs']
p=next((root/'evidence/yarden-20261022').glob('*.json'));d=json.loads(p.read_text(encoding='utf-8-sig'));towers=[r for r in d['world_evidence']['towers']['rows'] if r.get('definition',{}).get('item_id') in (3810,3811)]
result=dict(sources={str(x.relative_to(root)):sha(x) for x in (envp,orbp,p)},towers=towers,regions=[])
for tower in towers:
 field=field_at(tower['scene_position'],fields);a=assets[field];rows=[{k:r[k] for k in ('guid','ap_location_id','position')} for r in orbs if field_at(r['position'],fields)==field]
 for row in rows:
  p0=row['position']
  for dx in (-.01,.01):
   for dy in (-.01,.01):
    for dz in (-.01,.01):assert field_at(dict(x=p0['x']+dx,y=p0['y']+dy,z=p0['z']+dz),fields)==field
 result['regions'].append(dict(tower_definition_id=tower['definition']['item_id'],native_field_id=field,native_field_name=a['m_Name'],envDmgType=a['envDmgType'],traversal_verified=False,locations=rows))
 print(tower['definition']['item_id'],tower['guid'],tower['scene_position'],field,a['m_Name'],'orbs',len(rows),'envDmg',a['envDmgType'])
(out/'tower-orb-evidence.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
