"""Frozen observed orb candidates. Runtime activation awaits currency delivery.

This module does not import AP, submit checks, or grant items. IDs are reserved
here, not generated from current row order. Physical access is unverified.
"""
import json
import math
from pathlib import Path

CATALOG_ID = 'soul_orbs_20261015_r1'
CATALOG_SCHEMA = 1


def _int(value, minimum, maximum):
    return type(value) is int and minimum <= value <= maximum


def load_catalog():
    return json.loads(Path(__file__).with_name(CATALOG_ID + '.json').read_text(encoding='utf-8'))


def validate_catalog(data):
    if (data.get('catalog_id') != CATALOG_ID or type(data.get('schema')) is not int
            or data['schema'] != CATALOG_SCHEMA or data.get('frozen_ids') is not True):
        raise ValueError('Unsupported or unfrozen soul orb catalog')
    rows = data.get('orbs')
    if not isinstance(rows, list) or not rows:
        raise ValueError('Missing soul orb rows')
    guids, locations, names = set(), set(), set()
    for row in rows:
        if not isinstance(row, dict): raise ValueError('Invalid orb row')
        guid, loc, name = row.get('guid'), row.get('ap_location_id'), row.get('ap_location_name')
        if not _int(guid, -(2**31), 2**31-1) or not _int(loc, 2000, 2999):
            raise ValueError('Invalid orb identity')
        if not isinstance(name, str) or not name.strip() or name != name.strip():
            raise ValueError('Invalid orb name')
        if guid in guids or loc in locations or name in names:
            raise ValueError('Duplicate orb identity')
        pos = row.get('position')
        if not isinstance(pos, dict) or set(pos) != {'x', 'y', 'z'} or any(
                type(v) not in (int, float) or not math.isfinite(v) for v in pos.values()):
            raise ValueError('Invalid orb position')
        guids.add(guid); locations.add(loc); names.add(name)
    return rows


def resolve_location(slot_data, guid):
    """Strict prospective route, usable only for an explicitly allowed orb.

    Current APWorld does not emit this contract: activation is deliberately
    pending the replacement currency transaction. Never fall back to missions.
    """
    if (not isinstance(slot_data, dict) or slot_data.get('soul_orb_catalog') != CATALOG_ID
            or type(slot_data.get('soul_orb_schema')) is not int
            or slot_data['soul_orb_schema'] != CATALOG_SCHEMA
            or not _int(guid, -(2**31), 2**31-1)):
        return None
    allowed = slot_data.get('soul_orb_locations')
    if not isinstance(allowed, dict): return None
    value = allowed.get(str(guid))
    if not _int(value, 2000, 2999): return None
    rows = validate_catalog(load_catalog())
    return value if any(r['guid'] == guid and r['ap_location_id'] == value for r in rows) else None
