"""Offline schema-5 evidence analysis. Never assigns AP IDs or submits checks."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path


def integer(value, bits, signed=True):
    return type(value) is int and (-(2 ** (bits - 1)) if signed else 0) <= value < 2 ** (bits - 1 if signed else bits)


def orb_state(fields):
    if (not integer(fields.get('_guid'), 32)
            or type(fields.get('_firstTook')) is not bool or type(fields.get('_taken')) is not bool):
        return 'unknown'
    if fields['_firstTook']:
        return 'historical'
    return 'collected_this_load' if fields['_taken'] else 'uncollected'


def classify_event(event):
    result = {'method': event.get('method'), 'classification': 'unresolved', 'ap_check': False}
    method = event.get('method')
    before, after = event.get('before', {}).get('fields', {}), event.get('after', {}).get('fields', {})
    if method == 'Oc.OcSoulOrb_Spawner::SetTaken':
        if (orb_state(before) != 'unknown' and orb_state(after) != 'unknown'
                and before['_guid'] == after['_guid'] and before['_firstTook'] == after['_firstTook']
                and before['_taken'] is False and after['_taken'] is True):
            result.update(candidate_key=f"soul_orb:{before['_guid']}", classification=(
                'orb_repeat_collection_transition' if before['_firstTook'] else 'orb_first_collection_transition'))
    elif method == 'Oc.Maps.OcOWTreasureListHandler::OnOpenTreasure':
        args = event.get('arguments', [])
        if len(args) == 2 and {a.get('name') for a in args} == {'fieldID', 'posHash'}:
            args = {a['name']: a.get('value') for a in args}
            if integer(args['fieldID'], 32) and args['fieldID'] >= 0 and integer(args['posHash'], 64):
                result.update(candidate_key=f"random_chest:{args['fieldID']}:{args['posHash']}",
                              classification='random_chest_respawn_notification',
                              exclusion='respawning_chests_excluded_by_design')
    elif method in ('Oc.OcObj_RandomTreasure::Pickup', 'Oc.OcObj_RandomTreasure::Open_ForRpc'):
        result.update(classification='random_chest_activity', exclusion='respawning_chests_excluded_by_design')
    return result


def inventory(snapshot):
    result = {}
    for key, id_field in (('soul_orbs', '_guid'), ('treasure_boxes', '_PosHash'), ('random_chests', '_posHash')):
        section = snapshot.get('world_evidence', {}).get(key, {})
        rows = section.get('rows', [])
        fields = [r.get('fields', {}) for r in rows]
        ids = [f[id_field] for f in fields if integer(f.get(id_field), 32 if key == 'soul_orbs' else 64)]
        result[key] = {'status': section.get('status', 'not_captured'), 'rows': len(rows),
                       'truncated': section.get('truncated'), 'unique_ids': len(set(ids)),
                       'invalid_ids': len(rows) - len(ids),
                       'duplicate_ids': sorted(k for k, count in Counter(ids).items() if count > 1)}
        if key == 'soul_orbs':
            result[key]['states'] = dict(Counter(orb_state(f) for f in fields))
        if key == 'random_chests':
            result[key]['exclusion'] = 'respawning_chests_excluded_by_design'
        if key == 'treasure_boxes':
            result[key]['daily_reset_excluded'] = sum(f.get('_resetOnDayEnd') is True for f in fields)
            result[key]['unknown_reset_behavior'] = sum(type(f.get('_resetOnDayEnd')) is not bool for f in fields)
    return result


def analyze(directory):
    report = {'snapshots': [], 'events': [], 'duplicate_events_ignored': 0, 'limitations': [
        'Evidence only: no AP IDs assigned; no reward replacement or reachability proof.',
        'Orb history is character-scoped; world fingerprint is only a comparison hint.',
        'Respawning chests are excluded. Fixed chests still require persistence and reward verification.',
        'Repeated event records deduplicate by diagnostic session and sequence, not gameplay identity.']}
    seen = set()
    for path in sorted(Path(directory).glob('*.json')):
        if not path.name.startswith(('craftopia_diag_', 'observations_')):
            continue
        raw = path.read_bytes()
        data = json.loads(raw)
        if type(data.get('schema_version')) is not int or data['schema_version'] != 5:
            raise ValueError(f'Unsupported schema: {path.name}')
        provenance = {'file': path.name, 'sha256': hashlib.sha256(raw).hexdigest(),
                      'game_assembly_sha256': data.get('game_assembly_sha256'),
                      'collector_sha256': data.get('collector_sha256')}
        if path.name.startswith('craftopia_diag_'):
            report['snapshots'].append({**provenance, 'capture_id': data.get('capture_id'),
                                       'inventory': inventory(data)})
        else:
            for event in data.get('events', []):
                identity = (data.get('session'), event.get('sequence'))
                if not isinstance(identity[0], str) or not identity[0] or type(identity[1]) is not int:
                    raise ValueError(f'Missing diagnostic event identity: {path.name}')
                if identity in seen:
                    report['duplicate_events_ignored'] += 1
                    continue
                seen.add(identity)
                report['events'].append({**provenance, 'session': identity[0], 'sequence': identity[1],
                                         **classify_event(event)})
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.write_text(json.dumps(analyze(args.directory), indent=2) + '\n', encoding='utf-8')
