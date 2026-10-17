"""Produce a sanitized review queue; never infer loot tiers or change a catalog."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
from export_equipment_catalog import FIELDS, FLAGS


def audit(catalog, capture):
    if (capture.get('items_status') != 'collected'
            or not catalog.get('game_assembly_sha256')
            or catalog['game_assembly_sha256'] != capture.get('game_assembly_sha256')):
        raise ValueError('A complete capture from the catalog game build is required')
    def index(rows):
        result = {}
        for row in rows:
            key = row.get('id')
            if type(key) is not int or key in result:
                raise ValueError('Invalid or duplicate item ID')
            result[key] = row
        return result
    live = index(capture['items'])
    frozen = index(catalog['items'])
    comparisons = {}
    for key, row in frozen.items():
        source = live.get(key)
        if row['decision'] != 'progressive' or source is None:
            continue
        if any(source.get(k) != row.get(k) for k in FIELDS + FLAGS):
            continue
        comparisons.setdefault(row['category'], []).append(dict(
            id=key, name=source.get('display_name'), tier=row['tier'],
            minimum_age=row['minimum_age'], atk=source.get('atk'),
            matk=source.get('matk'), defense=source.get('def')))
    entries = []
    for key, row in sorted(frozen.items()):
        if row['decision'] != 'unsupported':
            continue
        source = live.get(key)
        if source is None:
            reason = 'missing_from_capture'
        elif any(source.get(k) != row.get(k) for k in FIELDS + FLAGS):
            reason = 'metadata_changed'
        elif (row['status'] not in (1, 2) or row['isOldData'] or row['cantUse']
              or row['existPrefab'] not in (3, 4) or row['inventoryTabId'] not in (1, 2, 7, 9)):
            reason = 'unavailable_metadata'
        elif row['category'] == 'AssistEquip':
            reason = 'unsupported_category'
        elif row['workbenchId'] == 20001:
            reason = 'loot_only'
        else:
            reason = 'other_recipe_metadata'
        source = source or {}
        entries.append(dict(id=key, name=source.get('display_name'),
            category=row['category'], reason=reason,
            review_status='needs_individual_review', proposed_tier=None,
            proposed_minimum_age=None, acquisition_evidence=[],
            atk=source.get('atk'), matk=source.get('matk'), defense=source.get('def')))
    return dict(schema_version=1, active_in_game=False,
        game_assembly_sha256=catalog['game_assembly_sha256'],
        counts=dict(sorted(Counter(row['reason'] for row in entries).items())),
        entries=entries, craftable_comparisons=comparisons,
        limitations=['Metadata eligibility does not prove acquisition or live availability.',
                    'Stats are comparisons only; rank, rarity and loot workbench level are not tier rules.',
                    'Every item requires acquisition evidence and an explicit tier decision.',
                    'This queue is not loaded by the generator, bridge or mod.'])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    catalog_path = root / 'worlds/craftopia/equipment_catalog_20261010_r1.json'
    raw = args.capture.read_bytes()
    result = audit(json.loads(catalog_path.read_text()), json.loads(raw))
    result['capture_sha256'] = hashlib.sha256(raw).hexdigest()
    result['catalog_sha256'] = hashlib.sha256(catalog_path.read_bytes()).hexdigest()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result['counts'], sort_keys=True))


if __name__ == '__main__': main()
