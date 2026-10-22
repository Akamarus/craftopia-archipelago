"""Frozen native field membership; traversal is explicitly unverified."""
import json
from pathlib import Path
from .RegionalRuns import geezah_orbs
from .SoulOrbs import load_catalog, validate_catalog

def regional_orbs(ending):
    rows = geezah_orbs()
    if ending == 'geezah':
        return rows
    # Yarden boundaries are not yet frozen. Reuse only earlier verified fields.
    if ending not in ('millewind', 'yarden_mountain', 'yarden_ruin'):
        raise ValueError('Unsupported orb ending region')
    subset = json.loads(Path(__file__).with_name('millewind_orbs_20261017_r1.json').read_text())
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
    return rows
