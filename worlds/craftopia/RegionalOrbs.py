"""Frozen native field membership; traversal is explicitly unverified."""
import json
from pathlib import Path
from .RegionalRuns import geezah_orbs
from .SoulOrbs import load_catalog, validate_catalog

def regional_orbs(ending):
    rows = geezah_orbs()
    if ending == 'geezah':
        return rows
    if ending not in ('millewind', 'yarden_mountain', 'yarden_ruin', 'owatatsu', 'brigandine'):
        raise ValueError('Unsupported orb ending region')
    subset = json.loads(__loader__.get_data(str(Path(__file__).with_name('millewind_orbs_20261017_r1.json'))).decode("utf-8"))
    canonical = {r['guid']: r for r in validate_catalog(load_catalog())}
    if subset['region'] != 'millewind' or len(subset['locations']) != 37:
        raise ValueError('Invalid Millewind native field subset')
    for row in subset['locations']:
        frozen = canonical[row['guid']]
        if any(row[key] != frozen[key] for key in ('ap_location_id', 'position')):
            raise ValueError('Millewind orb differs from frozen catalog')
        rows.append(frozen)
    if len({r['guid'] for r in rows}) != 88:
        raise ValueError('Duplicate regional orb')
    if ending.startswith('yarden_') or ending in ('owatatsu', 'brigandine'):
        subset = json.loads(__loader__.get_data(str(Path(__file__).with_name('yarden_orbs_20261022_r1.json'))).decode("utf-8"))
        if subset['region'] != 'yarden' or subset['native_field_id'] != 41588 or len(subset['locations']) != 49:
            raise ValueError('Invalid Yarden native field subset')
        for row in subset['locations']:
            frozen = canonical[row['guid']]
            if any(row[key] != frozen[key] for key in ('ap_location_id', 'position')):
                raise ValueError('Yarden orb differs from frozen catalog')
            rows.append(frozen)
        if len({r['guid'] for r in rows}) != 137:
            raise ValueError('Duplicate Yarden orb')
    if ending in ('owatatsu', 'brigandine'):
        for region, field in [('owatatsu', 41591)] + ([('brigandine', 41585)] if ending == 'brigandine' else []):
            subset = json.loads(__loader__.get_data(str(Path(__file__).with_name(region + '_orbs_20261022_r1.json'))).decode("utf-8"))
            if subset['region'] != region or subset['native_field_id'] != field or len(subset['locations']) != 51:
                raise ValueError('Invalid valley native field subset')
            for row in subset['locations']:
                frozen = canonical[row['guid']]
                if any(row[k] != frozen[k] for k in ('ap_location_id', 'position')):
                    raise ValueError('Valley orb differs from frozen catalog')
                rows.append(frozen)
        expected = 188 if ending == 'owatatsu' else 239
        if len(rows) != expected or len({r['guid'] for r in rows}) != expected:
            raise ValueError('Duplicate valley orb')
    return rows
