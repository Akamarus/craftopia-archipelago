"""Reproduce tower/material evidence using only existing read-only captures.

Run with any Python 3; no optional dependencies or game interaction required.
Writes only the sibling evidence.json. Recipe closure describes dependencies,
not proved access. Workspace captures are intentionally not bundled in repo.
"""
from pathlib import Path
from collections import Counter
import hashlib
import json
import xml.etree.ElementTree as ET
import zipfile

OUT = Path(__file__).resolve().parent
REPO = OUT.parents[2]
ROOT = REPO.parents[1]
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
catalog_path = REPO/'worlds/craftopia/progression_audit_20261016_r2.json'
catalog = read(catalog_path)
towers = [t for t in catalog['towers'] if t['item_id'] in (3810,3811)]
assert len(towers) == 2
def closure(targets):
    result = {}
    queue = list(map(str, targets))
    while queue:
        iid = queue.pop()
        if iid in result: continue
        row = catalog['items'][iid]
        result[iid] = row
        if row['active_recipe']: queue.extend(row['ingredients'])
    return dict(sorted(result.items(), key=lambda p:int(p[0])))
targets = {c['item_id'] for t in towers for s in t['stages'] for c in s['costs']}
recipes = closure(targets)
station_closure = closure((206,209))
assert max(r['age'] for r in recipes.values() if r['active_recipe']) == 4
assert recipes['5305']['output'] == 10
# Each path uses full batches; no inventory carryover or source substitutions.
def expand(iid, quantity):
    row = catalog['items'][str(iid)]
    if not row['active_recipe']: return Counter({str(iid): quantity})
    batches = (quantity + row['output'] - 1)//row['output']
    result = Counter()
    for ingredient,count in row['ingredients'].items():
        result.update(expand(ingredient,batches*count))
    return result
expanded = []
for tower in towers:
    for stage in tower['stages']:
        costs = Counter()
        for row in stage['costs']: costs.update(expand(row['item_id'],row['count']))
        expanded.append(dict(tower=tower['item_id'],stage=stage,
            recipe_only_leaf_costs=dict(costs)))
capture_path = ROOT/'evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json'
capture = read(capture_path)
equipment = [r for r in capture['items'] if r['id'] in (47,48,49,59,60,201)]
sources=[]
source_hashes={}
for name in ('assets0','assets-rest','assets-resources'):
    path=ROOT/f'artifacts/harvesting-research/{name}.json'
    source_hashes[str(path.relative_to(ROOT))]=sha(path)
    for row in read(path)['rows']:
        data=row['data']
        if row['cls']=='SoEnemyData' and any(data.get(f'd{n:02}_dropItemId') in (702,723,735) for n in range(10)):
            sources.append(dict(asset=row['asset'],path_id=row['path_id'],id=data['id'],
                rarity=data['rarity'],name=data['m_Name'],
                drops=[dict(item=data.get(f'd{n:02}_dropItemId'),count=data.get(f'd{n:02}_dropItemNum'),rate=data.get(f'd{n:02}_dropRate'))
                    for n in range(10) if data.get(f'd{n:02}_dropItemId') in (702,723,735)]))
workbook=ROOT/'outputs/logic-review-20261016/Craftopia - Outstanding Logic and Items.xlsx'
static_path=ROOT/'artifacts/harvesting-research/level13-full.json'
gold_static_components=[r for r in read(static_path)['rows']
    if r['cls'].startswith('OcStaticObj') and
    r['data'].get('_SoEnemyData',{}).get('m_FileID')==3 and
    r['data'].get('_SoEnemyData',{}).get('m_PathID') in (200,201,202)]
assert len(gold_static_components)==3
before=sha(workbook)
ns={'m':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
answers=[]
with zipfile.ZipFile(workbook) as archive:
    strings=([''.join(t.itertext()) for t in ET.fromstring(archive.read('xl/sharedStrings.xml')).findall('m:si',ns)]
             if 'xl/sharedStrings.xml' in archive.namelist() else [])
    for path in archive.namelist():
        if not (path.startswith('xl/worksheets/sheet') and path.endswith('.xml')):continue
        for row in ET.fromstring(archive.read(path)).findall('.//m:row',ns):
            values=[]
            for cell in row:
                val=cell.find('m:v',ns)
                text=val.text if val is not None else ''.join(cell.itertext())
                values.append(strings[int(text)] if cell.get('t')=='s' else text)
            if values and values[0] in {'Q028','Q029','702','723','735','758','2100','6590','6591','6592','6593','6595','6596','6597','6598'}:
                answers.append(dict(sheet_xml=path,row=int(row.get('r')),values=values))
assert sha(workbook)==before
age_rows=[r for r in catalog['age_transitions'] if r['from_world_level'] in (3,4)]
age_ids={r['item_id'] for a in age_rows for r in a['requirements']}
harvesting=read(REPO/'docs/research/2026-10-16-followup/harvesting.json')
age_sources=[r for r in harvesting['materials'] if r['item_id'] in age_ids]
assert len(age_sources)==12
result=dict(research_only=True,assembly_sha256=catalog['assembly_sha256'],
    catalog_sha256=sha(catalog_path),capture_sha256=sha(capture_path),
    workbook_sha256=before,source_hashes=source_hashes,towers=towers,
    recipe_closure=recipes,additional_station_recipe_closure=station_closure,
    per_stage_recipe_only_expansion=expanded,age_transitions=age_rows,
    age_harvesting_evidence=age_sources,equipment_capture_rows=equipment,
    gold_and_herb_drop_table_matches=sources,workbook_answers=answers,
    gold_static_components=gold_static_components,
    user_confirmation_20261022=dict(source='User statement relayed by parent agent on 2026-10-22',
        herbs='Bright Red Herb723 and Ultramarine Herb735 can both be picked in Owatatsu and Brigandine.',
        logic='Direct regional pickup source supported; do not require incomplete crop barter route.'),
    limitations=['No new gameplay, asset decode, or geographic placement proof performed.',
      'Recipe-only leaf expansion excludes stations and age costs; it is not a minimal gathering shopping list.',
      'No herb matches in scanned SoEnemyData is not evidence that pickup, crop or shop sources are absent.',
      'Gold modern tables rarity5 support Silver Pickaxe rank4; source placement and legacy rarity6 require separate treatment.',
      'Equipment recipe existence and attack values do not prove dungeon combat sufficiency.'])
(OUT/'evidence.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{len(recipes)} tower closure items, {len(age_sources)} age sources, {len(sources)} gold/herb table matches; workbook unchanged.')
