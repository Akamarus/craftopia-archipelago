"""Read-only current item identities joined to existing complete spawner export."""
from pathlib import Path
import json,sys
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[4]
sys.path[:0]=[r'C:/Users/Jack/.codex/.chatgpt-projects/g-p-6a888cf2cfb48191bcae6c0956701629/.local-tools/python',r'C:/Users/Jack/AppData/Local/Temp/craftopia-research-typetree']
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
base=Path('G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data')
g=TypeTreeGenerator('2022.3.62f3');g.load_local_dll_folder(str(base/'Managed'))
e=UnityPy.load(str(base/'sharedassets0.assets'));e.typetree_generator=g
items=[]
for obj in e.objects:
    if obj.assets_file.name!='sharedassets0.assets' or obj.type.name!='MonoBehaviour':continue
    head=obj.parse_monobehaviour_head()
    if head.m_Name not in ('So--ItemData_00723','So--ItemData_00735','So--ItemData_01204','So--ItemData_01234'):continue
    data=obj.read_typetree(check_read=True)
    items.append(dict(asset=obj.assets_file.name,path_id=obj.path_id,data=data))
assert {i['data']['id'] for i in items}=={723,735,1204,1234}
identities={i['path_id']:i['data']['id'] for i in items}
raw=json.loads((ROOT/'artifacts/remaining-merchants/raw.json').read_text(encoding='utf-8'))
matches=[]
for row in raw['rows']:
    if row['cls']!='OcSpawner':continue
    ref=row['data'].get('_ItemData',{})
    fid=ref.get('m_FileID',0)
    asset=raw['files'].get(row['asset'],{}).get(str(fid)) if fid else row['asset']
    if asset and asset.replace('\\','/').split('/')[-1]=='sharedassets0.assets' and ref.get('m_PathID') in identities:
        matches.append(dict(item_id=identities[ref['m_PathID']],**row))
total=len(matches)
matches=[r for r in matches if any(x in r.get('hierarchy','') for x in ('/4th_biome/','/5th_biome/'))]
(OUT/'herbs.json').write_text(json.dumps(dict(items=items,total_matches=total,spawners=matches,
    limitation='Item IDs decoded now; source placements reuse prior bounded spawner export, not exhaustive current live world.'),ensure_ascii=False,indent=2),encoding='utf-8')
print([(i['data']['id'],i['path_id']) for i in items]);print('total',total,'regional retained',len(matches))
