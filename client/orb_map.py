"""Read-only display state. Never grants or submits checks."""
from soul_orb_delivery import CATALOG, frozen_orbs

def orb_map_payload(slot_data, checked, observations):
    selected = {}
    if slot_data.get('soul_orb_catalog') == CATALOG and type(slot_data.get('soul_orb_schema')) is int and slot_data['soul_orb_schema'] == 1:
        raw = slot_data.get('soul_orb_locations', {})
        if isinstance(raw, dict):
            selected = {guid: loc for guid, loc in frozen_orbs().items()
                        if type(raw.get(str(guid))) is int and raw[str(guid)] == loc}
    return {'orb_map_protocol': 1,
            'checked': sorted(set(selected.values()).intersection(v for v in checked if type(v) is int)),
            'collected': sorted({r['orb_guid'] for r in observations if isinstance(r, dict)
                                and type(r.get('orb_guid')) is int and r['orb_guid'] in selected
                                and type(r.get('location_id')) is int
                                and selected[r['orb_guid']] == r['location_id']})}
