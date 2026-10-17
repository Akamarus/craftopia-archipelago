"""Experimental full captured mission pool, not a proof of game beatability.

Rules cover permission dependencies; unknown resource/age/enemy availability
remains an explicit playtest assumption. No custom Tamer objectives are added.
"""
import json
import re
from pathlib import Path
from .mission_id_mapper import get_id_map


def build_profile(item_table):
    catalog = json.loads(Path(__file__).with_name('equipment_catalog_20261010_r1.json').read_text())
    steps = {}
    for row in catalog['items']:
        if row['decision'] == 'progressive':
            steps.setdefault(row['category'], set()).add(row['tier'])
    steps = {category: sorted(tiers) for category, tiers in sorted(steps.items())}
    nodes = {'field': [[]]}
    missions = {str(mid): 'field' for mid in get_id_map()}

    def item(name, count=1): return {'item': name, 'count': count}
    def production(name): return item('Production: ' + name)
    def equipment(category, tier):
        # Receipts advance through actual catalog tiers, including gaps.
        return item('Progressive Equipment: ' + category,
                    next(i + 1 for i, value in enumerate(steps[category]) if value >= tier))
    def rule(ids, *requirements):
        name = 'mission_' + str(ids[0])
        nodes[name] = [list(requirements)]
        for mid in ids: missions[str(mid)] = name

    # Craft/use dependencies supported by the captured titles and current hooks.
    # Station construction itself remains vanilla; operating it can be gated.
    rule([60, 565, 7], equipment('Sword', 1))
    rule([67], production('cooking'))
    rule([73], production('smelting'))
    rule([80, 34], equipment('Bow', 1))
    rule([82], production('smelting'), equipment('Shield', 2))
    rule([106, 107], production('farming'))
    rule([529, 530, 575], production('farming'), production('cooking'))
    rule([46, 525], production('alchemy'))
    rule([541, 543], production('recycling'), production('equipment_care'))
    rule([542, 573], production('smelting'), equipment('Sword', 3), production('equipment_care'))
    rule([572, 574], production('equipment_care'))
    rule([519, 556], equipment('Body', 1))
    rule([557], equipment('Head', 2))
    rule([37, 38, 531, 532], production('smelting'), production('automation'))
    rule([560, 570], production('smelting'), item('Skill Tree: Tamer'))
    rule([544, 47], item('Skill Tree: Magic'), item('Skill Point'))
    rule([99], item('Skill Point'))

    # Hunt difficulty is a playtest assumption, not a verified damage threshold.
    # Several combat routes are valid; never force a particular weapon or Tamer.
    for tier in range(1, 5):
        nodes['combat_' + str(tier)] = [
            [equipment(category, tier)] for category in
            ('Sword', 'Bow', 'Lance', 'SwordTwoHand', 'Gun', 'Katana', 'MagicStaff')]
        nodes['combat_' + str(tier)].append(
            [item('Skill Tree: Tamer'), item('Skill Point', tier)])
    for mid, info in get_id_map().items():
        match = re.search(r'Lv (\d+) or higher', info['title'])
        if match:
            level = int(match[1])
            missions[str(mid)] = 'combat_' + str(4 if level >= 35 else 3 if level >= 20 else 2 if level >= 10 else 1)
    for mid in (566, 568): missions[str(mid)] = 'combat_1'
    missions['88'] = 'combat_3'

    pool = [name for name in item_table if name.startswith(('Skill Tree:', 'Production:'))]
    for category, tiers in steps.items():
        pool.extend(['Progressive Equipment: ' + category] * len(tiers))
    if len(pool) >= len(missions):
        raise ValueError('Expedition needs room for skill points; review catalog growth')
    return dict(schema=1, reviewed=False, experimental=True,
                profile_id='expedition_20261015_r1', evidence='docs/EXPEDITION_PROFILE.md',
                nodes=nodes, missions=missions, pool=pool, equipment_tier_steps=steps)
