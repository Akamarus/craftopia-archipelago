"""Export explicit equipment decisions from schema-4 evidence; no reachability claim."""
import argparse
import hashlib
import json
from pathlib import Path
import re

CATALOG_ID = 'equipment_20261010_r1'
WEAPONS = {5: 'Sword', 6: 'Bow', 7: 'Shield', 10: 'Lance', 28: 'SwordTwoHand',
           35: 'Gun', 36: 'Katana', 37: 'MagicStaff', 43: 'VacuumGun'}
FIELDS = ('id', 'status', 'wpCategory', 'equipSlot', 'workbenchId', 'workbenchLv',
          'existPrefab', 'inventoryTabId')
FLAGS = ('isOldData', 'cantUse')


def build_catalog(data, source_hash):
    if (type(data.get('schema_version')) is not int or data['schema_version'] != 4
            or data.get('items_status') != 'collected'
            or not isinstance(data.get('items'), list) or not data['items']):
        raise ValueError('A complete schema-4 item capture is required')
    for value in (source_hash, data.get('game_assembly_sha256'), data.get('collector_sha256')):
        if not isinstance(value, str) or not re.fullmatch('[0-9a-f]{64}', value):
            raise ValueError('Missing or invalid evidence hash')
    rows, seen = [], set()
    for item in data['items']:
        if not isinstance(item, dict) or any(type(item.get(k)) is not int for k in FIELDS):
            raise ValueError('Missing or malformed item fields')
        if any(type(item.get(k)) is not bool for k in FLAGS):
            raise ValueError('Missing or malformed item flags')
        if (not 0 <= item['wpCategory'] <= 44 or not 0 <= item['equipSlot'] <= 9
                or not 0 <= item['existPrefab'] <= 4 or not 0 <= item['inventoryTabId'] <= 9):
            raise ValueError('Unrecognized equipment enum value')
        item_id = item['id']
        if item_id < 0 or item_id > 32767 or item_id in seen:
            raise ValueError('Invalid or duplicate item ID')
        seen.add(item_id)
        if item_id == 0:  # explicit sentinel, never an item permission
            continue
        row = {key: item[key] for key in FIELDS + FLAGS}
        slot = item['equipSlot']
        category = WEAPONS.get(item['wpCategory']) if slot in (0, 1, 2, 3) else None
        if slot in (0, 1, 2, 3) and item['wpCategory'] == 38:
            category = 'AssistEquip'  # Combat gloves are not ordinary tools.
        if slot in (4, 5):
            category = {4: 'Head', 5: 'Body'}[slot]
        row['decision'] = 'vanilla'
        if category:
            row.update(category=category, decision='unsupported')
            if (category != 'AssistEquip' and item['status'] in (1, 2) and not item['isOldData'] and not item['cantUse']
                    and item['existPrefab'] in (3, 4) and item['inventoryTabId'] in (1, 2, 7, 9)
                    and item['workbenchId'] > 0 and item['workbenchId'] != 20001
                    and 0 <= item['workbenchLv'] <= 9):
                row.update(decision='progressive', tier=item['workbenchLv'] + 1,
                           minimum_age=item['workbenchLv'])
        rows.append(row)
    if not rows:
        raise ValueError('Empty equipment evidence')
    return dict(catalog_id=CATALOG_ID, schema_version=1, source_sha256=source_hash,
                game_assembly_sha256=data['game_assembly_sha256'],
                collector_sha256=data['collector_sha256'], captured_at=data.get('timestamp'),
                limitations=['Recipe age projects tier = age + 1; availability is not proven.',
                             'Loot-only combat equipment has no inferred tier and is unsupported.',
                             'AssistEquip combat gloves are unsupported. Ammo, accessories and utility gear retain vanilla checks.',
                             'Vanilla rows retain original game checks; they do not grant items.',
                             'No world, character, inventory, position or account identity is exported.'],
                items=sorted(rows, key=lambda row: row['id']))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    raw = args.capture.read_bytes()
    catalog = build_catalog(json.loads(raw), hashlib.sha256(raw).hexdigest())
    # One compact item per line keeps the derived table reviewable without a huge file.
    header = {key: value for key, value in catalog.items() if key != 'items'}
    text = json.dumps(header, indent=2)[:-2] + ',\n  "items": [\n'
    text += ',\n'.join('    ' + json.dumps(row, separators=(',', ':')) for row in catalog['items'])
    args.output.write_text(text + '\n  ]\n}\n', encoding='utf-8')
    print(f"Exported {len(catalog['items'])} explicit item decisions")
