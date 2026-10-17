"""Create a new frozen candidate catalog once; never overwrite an allocation."""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path


def export(source, destination):
    raw = source.read_bytes()
    data = json.loads(raw)
    if type(data.get('schema_version')) is not int or data['schema_version'] != 5:
        raise ValueError('Expected schema 5')
    section = data['world_evidence']['soul_orbs']
    if section.get('status') != 'collected' or section.get('truncated') is not False:
        raise ValueError('Incomplete orb capture')
    rows = section['rows']
    if type(section.get('observed_count')) is not int or section['observed_count'] != len(rows):
        raise ValueError('Capture count does not match rows')
    for key in ('game_assembly_sha256', 'collector_sha256'):
        if not isinstance(data.get(key), str) or not re.fullmatch('[0-9a-fA-F]{64}', data[key]):
            raise ValueError('Missing provenance hash')
    for row in rows:
        pos = row.get('scene_position')
        if not isinstance(pos, dict) or set(pos) != {'x', 'y', 'z'} or any(
                type(v) not in (int, float) or not math.isfinite(v) for v in pos.values()):
            raise ValueError('Invalid orb position')
    guids = [r['fields']['_guid'] for r in rows]
    if (not rows or len(rows) > 1000 or any(type(g) is not int or not -(2**31) <= g < 2**31 for g in guids)
            or len(set(guids)) != len(guids)):
        raise ValueError('Invalid or duplicate orb IDs')
    result = {'catalog_id': 'soul_orbs_20261015_r1', 'schema': 1, 'frozen_ids': True,
              'runtime_ready': False, 'status': 'observed_candidates_access_unverified',
              'source': {'sha256': hashlib.sha256(raw).hexdigest(), 'captured_at': data['timestamp'],
                         'game_assembly_sha256': data['game_assembly_sha256'],
                         'collector_sha256': data['collector_sha256'], 'collector_version': data['collector_version']},
              'limitations': ['One loaded-world capture, not a complete game catalog.',
                              'No region, quest, age or traversal requirements verified.',
                              'Only one orb has live collection and reload evidence.',
                              'Cross-character and cross-world identity stability unverified.',
                              'Currency replacement and durable delivery required before activation.',
                              'Respawning chests are excluded; no chest rows are included.'],
              'orbs': []}
    for index, row in enumerate(sorted(rows, key=lambda r: r['fields']['_guid'])):
        guid = row['fields']['_guid']
        result['orbs'].append({'guid': guid, 'ap_location_id': 2000 + index,
                               'ap_location_name': f'Soul Orb {guid}', 'position': row['scene_position']})
    with destination.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2); stream.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    args = parser.parse_args()
    export(args.source, args.destination)
