"""Instance-based mission ID mapper using runtime evidence.

Uses mission_catalog_20261007_r1.json (125 observed missions) to map
specific numeric mission IDs to AP location IDs.

This is SEPARATE from the legacy class-based mapping in __init__.py.
The class-based mapping is preserved for existing seeds; this mapper
is versioned and opt-in via the mission_catalog_version option.

Provenance: Craftopia runtime 2026-10-07, Unity 2022.3.62,
AD__Overcraft via DiagnosticCollector F9.
See mission_catalog_20261007_r1.json for full provenance.
"""

import json
from pathlib import Path

CATALOG_VERSION = "20261007_r1"
# AP location IDs for instance-based mapping.
# Base offset avoids collision with legacy class-based IDs.
# Legacy uses 1-53 for class-based; instance-based uses 1000+.
INSTANCE_BASE_ID = 1000

def load_catalog():
    """Load the versioned runtime mission catalog."""
    path = Path(__file__).parent / f"mission_catalog_{CATALOG_VERSION}.json"
    with open(path) as f:
        return json.load(f)

def build_id_map():
    """Build mission_id -> AP location_id mapping.

    Uses FROZEN AP IDs from the catalog. These are immutable;
    never recomputed. Each observed mission gets a unique AP ID.
    Multiple missions sharing a class remain distinct.
    """
    catalog = load_catalog()
    # Verify frozen allocation exists
    if not catalog.get("id_allocation", {}).get("frozen"):
        raise ValueError("Catalog missing frozen ID allocation")
    id_map = {}
    for mission in catalog["missions"]:
        # Use the frozen ap_location_id from the catalog
        ap_id = mission.get("ap_location_id")
        if ap_id is None:
            raise ValueError(f"Mission {mission['id']} missing frozen AP ID")
        id_map[mission["id"]] = {
            "ap_location_id": ap_id,
            "ap_location_name": mission.get(
                "ap_location_name",
                f"Mission {mission['id']}: {mission['title']}"),
            "class": mission["class"],
            "title": mission["title"],
            "category": mission["category"],
        }
    return id_map

# Lazily built on first use.
_id_map_cache = None

def get_id_map():
    global _id_map_cache
    if _id_map_cache is None:
        _id_map_cache = build_id_map()
    return _id_map_cache

def mission_id_to_ap_location(mission_id):
    """Map a runtime mission ID to its AP location ID, or None."""
    entry = get_id_map().get(mission_id)
    return entry["ap_location_id"] if entry else None

def mission_id_to_ap_name(mission_id):
    """Map a runtime mission ID to its AP location name, or None."""
    entry = get_id_map().get(mission_id)
    return entry["ap_location_name"] if entry else None
