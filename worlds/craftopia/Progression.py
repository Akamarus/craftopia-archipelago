"""Evidence-bound prerequisite evaluation. No bundled profile is yet playable.

Nodes describe repeatable access, not quantities of consumable inventory. Every
alternative is a conjunction; an explicit empty conjunction is a verified free
source. Computing the least fixed point prevents a recipe cycle proving itself.
"""
from copy import deepcopy


def load_reviewed_profile():
    # Deliberately no inferred/default profile: item recipes alone do not prove
    # gathering access, station families, mission conditions or boss access.
    raise ValueError('No reviewed Craftopia progression profile is available yet; '
                     'candidate mode retains the existing experimental logic')


class ProgressionModel:
    def __init__(self, profile, known_items, *, allow_experimental=False):
        if (not isinstance(profile, dict) or type(profile.get('schema')) is not int
                or profile['schema'] != 1
                or not (profile.get('reviewed') is True or
                        (allow_experimental and profile.get('reviewed') is False
                         and profile.get('experimental') is True))
                or not isinstance(profile.get('evidence'), str)
                or not profile['evidence'].strip()):
            raise ValueError('Progression requires a reviewed schema-1 evidence profile')
        nodes, missions = profile.get('nodes'), profile.get('missions')
        if not isinstance(nodes, dict) or not nodes or not isinstance(missions, dict) or not missions:
            raise ValueError('Progression nodes and mission mappings are required')
        for name, sources in nodes.items():
            if not isinstance(name, str) or not name or not isinstance(sources, list) or not sources:
                raise ValueError('Each node needs named, explicit sources')
            for conjunction in sources:
                if not isinstance(conjunction, list):
                    raise ValueError('Source prerequisites must be a list')
                for requirement in conjunction:
                    if not isinstance(requirement, dict):
                        raise ValueError('Invalid prerequisite')
                    if set(requirement) == {'node'}:
                        if not isinstance(requirement['node'], str) or requirement['node'] not in nodes:
                            raise ValueError('Unknown prerequisite node')
                    elif set(requirement) == {'item', 'count'}:
                        if (not isinstance(requirement['item'], str) or requirement['item'] not in known_items
                                or type(requirement['count']) is not int or requirement['count'] <= 0):
                            raise ValueError('Unknown item or invalid prerequisite count')
                    else:
                        raise ValueError('Unsupported prerequisite fields')
        for mission_id, node in missions.items():
            if (not isinstance(mission_id, str) or not mission_id.isascii()
                    or not mission_id.isdecimal() or str(int(mission_id)) != mission_id
                    or not isinstance(node, str) or node not in nodes):
                raise ValueError('Mission IDs must be canonical nonnegative integers with known nodes')
        self.nodes = deepcopy(nodes)
        self.missions = dict(missions)

    def require_coverage(self, mission_ids):
        missing = [str(mid) for mid in mission_ids if type(mid) is not int or str(mid) not in self.missions]
        if missing:
            raise ValueError('Missing reviewed mission prerequisites: ' + ', '.join(missing))

    def can_complete(self, mission_id, item_count):
        self.require_coverage([mission_id])
        reachable = set()
        counts = {}

        def satisfied(requirement):
            if 'node' in requirement:
                return requirement['node'] in reachable
            name = requirement['item']
            if name not in counts:
                value = item_count(name)
                counts[name] = value if type(value) is int and value >= 0 else 0
            return counts[name] >= requirement['count']

        while True:
            added = {name for name, sources in self.nodes.items() if name not in reachable
                     and any(all(satisfied(req) for req in source) for source in sources)}
            if not added:
                return self.missions[str(mission_id)] in reachable
            reachable.update(added)


# Verified enum values from the installed assembly. These are candidates only;
# classification is not proof that an item or its materials are obtainable.
WEAPON_CATEGORIES = {5: 'Sword', 6: 'Bow', 7: 'Shield', 10: 'Lance',
                     28: 'SwordTwoHand', 35: 'Gun', 36: 'Katana',
                     37: 'MagicStaff', 43: 'VacuumGun'}


def equipment_candidate(row):
    if not isinstance(row, dict):
        return None
    for field in ('id', 'status', 'wpCategory', 'equipSlot', 'workbenchId', 'workbenchLv'):
        if type(row.get(field)) is not int:
            return None
    if (row['id'] <= 0 or row['status'] not in (1, 2)
            or row.get('isOldData') is not False or row.get('cantUse') is not False
            or row['workbenchId'] <= 0 or row['workbenchId'] == 20001
            or not 0 <= row['workbenchLv'] <= 9):
        return None
    slot = row['equipSlot']
    category = WEAPON_CATEGORIES.get(row['wpCategory']) if slot in (0, 1, 2, 3) else None
    if slot in (4, 5):
        category = {4: 'Head', 5: 'Body'}[slot]
    if category is None:
        return None
    return {'item_id': row['id'], 'category': category,
            'tier': row['workbenchLv'] + 1, 'minimum_age': row['workbenchLv']}
