"""
Craftopia Archipelago world definition.

Initial playable scope (v1.0):
- Checks: Missions detected via Mission.TryGetReward() postfix.
  Each mission completion = one AP location check.
- Rewards: Skill Points via OcPlSkillCtrl.AddSkillPoint() (verified grant).
- Victory: Complete the designated goal mission.

Excluded (no evidence yet): dungeon boss kills, tower restores, field boss
kills, inventory item grants, XP/gold grants. These have hook candidates in
docs/HOOK_INVENTORY.md but are not in the v1.0 playable scope.

Mission data lives in data/missions.json. AP location IDs are sequential
(1..N); the game mission ID (Mission.get_ID()) is stored per-location and
the client maps game ID -> AP location ID. Game IDs are never invented;
missions without a verified game ID are marked "game_id": null and skipped
at generation with a warning.
"""
from typing import Dict, List, Optional

# AP location ID base. Sequential IDs starting at 1.
# The game mission ID is stored in the location data, not the AP ID.
LOCATION_BASE = 1

# AP item IDs. Sequential starting at 1.
ITEM_BASE = 1


class CraftopiaItemDef:
    def __init__(self, name: str, ap_id: int, classification: str, max_count: int):
        self.name = name
        self.ap_id = ap_id
        self.classification = classification  # "progression", "useful", "filler", "trap"
        self.max_count = max_count


class CraftopiaLocationDef:
    def __init__(self, name: str, ap_id: int, game_id: int | None,
                 region: str, mission_class: str):
        self.name = name
        self.ap_id = ap_id
        self.game_id = game_id  # Mission.get_ID(); None if not yet captured
        self.region = region
        self.mission_class = mission_class  # e.g. "Oc.Missions.ItemCraftMission" (stable key)


def load_mission_data() -> List[dict]:
    """Load missions.json. Returns list of mission dicts."""
    import json
    import os
    path = os.path.join(os.path.dirname(__file__), "data", "missions.json")
    with open(path) as f:
        data = json.load(f)
    return data.get("missions", [])


def build_item_table() -> Dict[str, CraftopiaItemDef]:
    """Build the item table.

    v1.3: Full progression items.
    - Skill Points: verified grant mechanism.
    - Skill Trees: 8 unlock items (Magic, Life, Trick, Tamer, Combat T2-T5).
      Basic + Combat T1 are starters, not AP items.
    - Equipment: progressive per-category tiers (pending equipment analysis).
    - Production: unlock groups (pending production analysis).
    - Filler: material/money bundles.
    - Victory: goal token.
    """
    items = {}
    next_id = ITEM_BASE

    # Skill Point: the verified grant mechanism (AddSkillPoint).
    items["Skill Point"] = CraftopiaItemDef(
        name="Skill Point",
        ap_id=next_id,
        classification="progression",
        max_count=999,
    )
    next_id += 1

    # Skill Tree unlocks: category x tier granularity.
    # See /tmp/skill_trees.md for evidence and rationale.
    skill_trees = [
        "Skill Tree: Magic",
        "Skill Tree: Life",
        "Skill Tree: Trick",
        "Skill Tree: Tamer",
        "Skill Tree: Combat Tier 2",
        "Skill Tree: Combat Tier 3",
        "Skill Tree: Combat Tier 4",
        "Skill Tree: Combat Tier 5",
    ]
    for name in skill_trees:
        items[name] = CraftopiaItemDef(
            name=name,
            ap_id=next_id,
            classification="progression",
            max_count=1,  # Unique unlocks
        )
        next_id += 1

    # Production unlocks: 10 binary groups (G10 power excluded pending Q6).
    # See /tmp/production_groups.md for evidence and group definitions.
    # G1 smelting, G2 cooking, G3 farming, G4 livestock_fishing, G5 alchemy,
    # G6 recycling, G7 water_processing, G8 equipment_care, G9 automation,
    # G11 creature_combining.
    production_groups = [
        "Production: smelting",
        "Production: cooking",
        "Production: farming",
        "Production: livestock_fishing",
        "Production: alchemy",
        "Production: recycling",
        "Production: water_processing",
        "Production: equipment_care",
        "Production: automation",
        "Production: creature_combining",
    ]
    for name in production_groups:
        items[name] = CraftopiaItemDef(
            name=name,
            ap_id=next_id,
            classification="progression",
            max_count=1,  # Unique unlocks
        )
        next_id += 1

    # Filler: material and money bundles (useful, not progression-critical).
    filler_items = [
        "Material Bundle: Wood",
        "Material Bundle: Stone",
        "Material Bundle: Iron",
        "Money Bundle: Small",
        "Money Bundle: Medium",
    ]
    for name in filler_items:
        items[name] = CraftopiaItemDef(
            name=name,
            ap_id=next_id,
            classification="filler",
            max_count=999,
        )
        next_id += 1

    # Victory token: placed on the goal location.
    items["Victory"] = CraftopiaItemDef(
        name="Victory",
        ap_id=next_id,
        classification="progression",
        max_count=1,
    )

    # Append-only IDs: never change existing seed item IDs (1..25).
    # Catalog registration is not pool enablement. Equipment is excluded from
    # playable generation until reviewed prerequisites and runtime gates exist.
    for ap_id, category in enumerate(('Sword', 'Bow', 'Shield', 'Lance', 'SwordTwoHand',
                                     'Gun', 'Katana', 'MagicStaff', 'VacuumGun', 'Head', 'Body'), 26):
        name = 'Progressive Equipment: ' + category
        items[name] = CraftopiaItemDef(name, ap_id, 'progression', 10)

    # Append-only currency ID; one unit replaces each selected orb's vanilla unit.
    items['Soul Orb Currency'] = CraftopiaItemDef('Soul Orb Currency', 37, 'useful', 999)
    return items


def build_location_table(missions: List[dict]) -> Dict[str, CraftopiaLocationDef]:
    """Build the location table from mission data.

    Keyed by mission class name (verified from IL, stable). The numeric
    game_id is stored when available (captured at runtime) but the class
    name is the primary key since game_ids are data-driven.
    """
    locations = {}
    ap_id = LOCATION_BASE
    for m in missions:
        mission_class = m.get("class")
        if not mission_class:
            continue  # no stable key; skip
        game_id = m.get("game_id")  # may be None
        name = m["name"]
        # Ensure unique names (Archipelago requirement).
        suffix = 1
        base_name = name
        while name in locations:
            suffix += 1
            name = f"{base_name} ({suffix})"
        locations[name] = CraftopiaLocationDef(
            name=name,
            ap_id=ap_id,
            game_id=game_id,
            region=m.get("region", "Overworld"),
            mission_class=mission_class,
        )
        ap_id += 1
    return locations
