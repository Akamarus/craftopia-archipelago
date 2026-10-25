"""Read installed evidence and workbook; write only this research directory.
Run from the Craftopia workspace with its existing research Python environment.
"""
from pathlib import Path
import hashlib, json, mmap, re, struct
import openpyxl

root = Path.cwd()
repo = root / 'public-repo/craftopia-archipelago'
out = repo / 'docs/research/yarden-20261021'
base = Path('G:/SteamLibrary/steamapps/common/Craftopia/Craftopia_Data')
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
expected = '88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea'
assert sha(base/'Managed/AD__Overcraft.dll') == expected, 'Reaudit changed assembly'

workbook = root/'outputs/logic-review-20261016/Craftopia - Outstanding Logic and Items.xlsx'
before = sha(workbook)
wb = openpyxl.load_workbook(workbook, read_only=True, data_only=True)
answers=[]
for sheet in wb:
    for number, row in enumerate(sheet.values, 1):
        if row and str(row[0]) in {'Q002','Q026','Q027','701','763','2453','2048','2460','6581','6582','6586','6587'}:
            answers.append(dict(sheet=sheet.title,row=number,values=row))
wb.close()
assert sha(workbook)==before, 'Workbook changed during read'

catalog=json.loads((repo/'worlds/craftopia/progression_audit_20261016_r2.json').read_text(encoding='utf-8'))
towers=[t for t in catalog['towers'] if t['item_id'] in (3808,3809)]
queue=[str(c['item_id']) for t in towers for s in t['stages'] for c in s['costs']]
items={}
while queue:
    iid=queue.pop(0)
    if iid in items: continue
    row=catalog['items'].get(iid)
    assert row is not None, iid
    items[iid]=row
    if row['active_recipe']: queue.extend(row['ingredients'])

# Reuse established read-only Unity decoder setup, not its scan/write loop.
exec((root/'artifacts/remaining-equipment/extract-fixed.py').read_text(encoding='utf-8').split('rows=[];errors=[]')[0])
environment=UnityPy.load(str(base/'sharedassets6.assets')); environment.typetree_generator=g
rows=[]; errors=[]
for obj in environment.objects:
    if obj.assets_file.name!='sharedassets6.assets' or obj.type.name!='MonoBehaviour': continue
    try:
        header=obj.parse_monobehaviour_head(); cls=header.m_Script.deref().read().m_ClassName
        if cls not in ('OcDungeonSo','OcDungeonEntranceSo'): continue
        data=obj.read_typetree(check_read=True)
        rows.append(dict(asset=obj.assets_file.name,path_id=obj.path_id,cls=cls,data=data))
    except Exception as exc:
        errors.append(dict(path_id=obj.path_id,error=str(exc)[:160]))

localization=[]
with (base/'resources.assets').open('rb') as stream, mmap.mmap(stream.fileno(),0,access=mmap.ACCESS_READ) as data:
    for match in re.finditer(rb'800_DungeonName/[0-9]+\x00', data):
        start=match.start(); term=match.group()[:-1]
        end=(match.end()+3)//4*4
        try:
            typ,count,length=struct.unpack_from('<iii',data,end)
            if typ!=0 or not 1<=count<=30 or not 1<=length<=256: continue
            name=data[end+12:end+12+length].decode('utf-8').rstrip('\0')
            if name not in ('Ruin of Hunters','Old Garden of Storms'): continue
            localization.append(dict(term=term.decode(),offset=start,english=name,
                record_hex=data[start-4:end+12+length].hex()))
        except (ValueError,UnicodeDecodeError,struct.error): continue
ids={int(x['term'].split('/')[-1]) for x in localization}
dungeons=[r for r in rows if r['cls']=='OcDungeonSo' and r['data'].get('sourceId') in ids]
paths={r['path_id'] for r in dungeons}
entrances=[r for r in rows if r['cls']=='OcDungeonEntranceSo' and
           r['data']['defaultDungeon']['m_FileID']==0 and r['data']['defaultDungeon']['m_PathID'] in paths]
assert {x['english'] for x in localization}=={'Ruin of Hunters','Old Garden of Storms'}
assert {(r['data']['sourceId'],r['data']['battleDungeonType']) for r in dungeons}=={(3,0),(4,0),(203,1),(204,1)}
assert len(entrances)==2 and all(r['data']['_underDevelop']==0 for r in entrances)
assert len(towers)==2 and all(len(t['stages'])==3 for t in towers)
assert len(items)==31
resource_tables=[]
for filename in ('assets0','assets-rest'):
    previous=json.loads((root/f'artifacts/harvesting-research/{filename}.json').read_text(encoding='utf-8'))
    for r in previous['rows']:
        if r['cls']!='SoEnemyData': continue
        d=r['data']
        if (r['asset'],r['path_id']) not in {('sharedassets0.assets',80664),('sharedassets13.assets',197),('sharedassets13.assets',198),('sharedassets13.assets',199)}: continue
        resource_tables.append(dict(asset=r['asset'],path_id=r['path_id'],id=d['id'],rarity=d['rarity'],
            drops=[dict(item=d.get(f'd{n:02}_dropItemId'),count=d.get(f'd{n:02}_dropItemNum'),rate=d.get(f'd{n:02}_dropRate'))
                   for n in range(10) if d.get(f'd{n:02}_dropItemId') in (701,763)]))
assert len(resource_tables)==4
code=(root/'artifacts/harvesting-research/all.cs').read_text(encoding='utf-8').splitlines()
code_evidence=[dict(first_line=a,last_line=z,text='\n'.join(code[a-1:z]))
               for a,z in ((14130,14137),(185822,185861),(303800,303820))]
result=dict(assembly_sha256=expected,workbook_sha256=before,workbook_answers=answers,
            towers=towers,recipe_closure=items,
            age_transitions=[a for a in catalog['age_transitions'] if a['from_world_level'] in (2,3)],
            localization=localization,dungeons=dungeons,entrances=entrances,
            resource_tables=resource_tables,stool_code=code_evidence,
            decode_errors=errors,scope='sharedassets6 dungeon definitions; resources localization; existing audited recipe catalog')
(out/'evidence.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('Workbook unchanged:',before)
print('Localization:',[(r['term'],r['english']) for r in localization])
print('Dungeons:',[(r['path_id'],r['data']['sourceId'],r['data']['battleDungeonType'],r['data']['bossEmType'],r['data']['bossLevel']) for r in dungeons])
print('Entrances:',[(r['path_id'],r['data']['m_Name'],r['data']['defaultDungeon']) for r in entrances])
print('Recipe closure:',len(items),'items; decode errors:',len(errors))
