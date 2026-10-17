"""Offline source audit; reads evidence and writes only adjacent research output."""
import csv
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

OUT = Path(__file__).resolve().parent
WORKSPACE = OUT.parents[4]
CAPTURE = WORKSPACE / 'evidence/milestones-20261016/craftopia_diag_20261016_181842_9364a392874f467b9334b501b2962ae0.json'
data = json.loads(CAPTURE.read_text(encoding='utf-8-sig'))
items = {r['id']: r for r in data['items']}
mapdir = WORKSPACE / 'map-research-20261015'
with (mapdir/'coordinates-full.csv').open(encoding='utf-8-sig', newline='') as f:
    coordinates = list(csv.DictReader(f))
with (mapdir/'markers-full.csv').open(encoding='utf-8-sig', newline='') as f:
    markers = list(csv.DictReader(f))
metadata = {r['Unique Identifier']: r for r in markers if r['Unique Identifier']}
target_ids = {698,746,752,699,701,702,703,704,705,706,2454,3450,3373,758,683,
              680,692,695,763,2453,723,735,5565,684,693,759,740,3445,3360,2076,
              2457,2458,2100,2452,2451,3468,3469,3470,3463,3464,3465,5305,2459}
target_ids.update(i['item_id'] for age in data['ages'] for i in age['requireItems'])
shops = []
for shop in data['world_evidence']['shop_catalog']['rows']:
    for i, entry in enumerate(shop.get('fields', {}).get('_saleInfos', {}).get('items', [])):
        f = entry.get('fields', {})
        if f.get('_id') not in target_ids:
            continue
        shops.append(dict(item_id=f['_id'], item_name=items.get(f['_id'],{}).get('display_name'),
            shop_asset=shop.get('asset_name'), entry_index=i, required_world_level=f.get('_requirWorldLv'),
            overwrite_price=f.get('_overwritePrice'), cashier_type=shop['fields'].get('_cashier',{}).get('type'),
            can_kill_drop=f.get('_canKillDrop'),
            limitation='Loaded shop definition; merchant position, stock and access not proven.'))
leads=[]
for row in coordinates:
    meta=metadata.get(row.get('Marker identifier'),{})
    name=meta.get('Name (en)','') or row.get('Marker','')
    group=meta.get('Group Name (en)','')
    if group not in ('Plants','Ores','Bedrocks','Animals','Fishing') and not re.search(
        'snow mint|hell pepper|poisonous mushroom|yellow herb',name,re.I):
        continue
    leads.append(dict(external_row_id=row['ID'], name=name, group=group,
        coordinates=row['Coordinates'], marker_identifier=row['Marker identifier']))
result=dict(research_only=True, capture_sha256=hashlib.sha256(CAPTURE.read_bytes()).hexdigest(),
    map_source='https://kregap.github.io/craftopia-map/', map_snapshot_date='2026-10-15',
    map_coordinates_sha256=hashlib.sha256((mapdir/'coordinates-full.csv').read_bytes()).hexdigest(),
    shop_entries=shops, map_leads=leads,
    map_counts=dict(sorted(Counter(r['name'] for r in leads).items())),
    limitations=['Map rows are author dataset leads, not game IDs, biome polygons or verified access rules.',
                'No nearest-tower region assignment is made.',
                'No exact drop tables or harvest tool requirements were captured.',
                'No game state or generated seed is changed.'])
(OUT/'material-sources.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(shop_entries=len(shops),map_leads=len(leads),map_counts=result['map_counts']),indent=2))
