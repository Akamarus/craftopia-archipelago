"""Provisional player-reviewed regional timing; frozen runtime tiers stay intact."""
import json
from pathlib import Path

REGIONS = ('geezah', 'millewind', 'yarden_mountain', 'yarden_ruin', 'owatatsu', 'brigandine')
COMBAT_TIERS = dict(zip(REGIONS, (2, 3, 4, 5, 6, 7)))
WEAPONS = ('Sword', 'Bow', 'Lance', 'SwordTwoHand', 'Gun', 'Katana', 'MagicStaff')


def apply_balance(profile):
    entries = json.loads(Path(__file__).with_name('regional_balance_20261024.json').read_text())['entries']
    end = REGIONS.index(profile['region'])
    selected = [r for r in entries if r['deadline'] in REGIONS
                and REGIONS.index(r['deadline']) <= end]
    profile['pool'] = [r['item'] for r in selected]
    profile['pool_deadlines'] = [r['deadline'] for r in selected]
    profile['starting_items'] = [r['item'] for r in entries if r['deadline'] == 'Starting inventory']
    profile['balance_revision'] = 'regional_balance_20261024_r1'
    profile['profile_id'] = 'regional_' + profile['region'] + '_20261024_r1'
    profile['equipment_catalog'] = 'equipment_reviewed_20261024_r1'
    profile['pool'].append('Progressive Equipment: AssistEquip')
    profile['pool_deadlines'].append('geezah')
    nodes = profile['nodes']
    steps = profile['equipment_tier_steps']
    steps['AssistEquip'] = [1]

    def routes(tier):
        result = []
        for category in WEAPONS:
            # A higher first tier also satisfies a lower combat threshold.
            count = next(i + 1 for i, value in enumerate(steps[category]) if value >= tier)
            route = [{'item': 'Progressive Equipment: ' + category, 'count': count}]
            if category == 'MagicStaff':
                route += [{'item': 'Skill Tree: Magic', 'count': 1}, {'item': 'Skill Point', 'count': 1}]
            result.append(route)
        return result

    # Material gathering combat supports weapon alternatives too; crafting an
    # actual sword remains its own capability. No tier gate is added to repairs.
    nodes['basic_weapon_combat'] = routes(2)
    if 'night_skeleton' in nodes:
        nodes['night_skeleton'] = [[{'node': 'basic_weapon_combat'}, {'node': 'field'}]]
    if 'yarden_combat_drops' in nodes:
        nodes['yarden_combat_drops'] = [[{'node': 'yarden_field'}, {'node': 'basic_weapon_combat'}]]
    for region in REGIONS[:end+1]:
        tier = COMBAT_TIERS[region]
        name = 'combat_' + region
        # Preserve native opening construction/material access. Higher thresholds
        # are conservative AP permissions, not a claim of guaranteed loot or
        # that platinum/adamantite can already be crafted in these regions.
        age = dict(geezah=1, millewind=2, yarden_mountain=3,
                   yarden_ruin=3, owatatsu=4, brigandine=4)[region]
        nodes[name] = [[{'node': 'age_' + str(age)}, {'node': 'blacksmith'}] + r
                       for r in routes(tier)]
        nodes['dungeon_' + region] = [[{'node': name}]]
        for milestone in profile['milestones']:
            if milestone['region'] == region and milestone['family'] == 'dungeon':
                profile['milestone_rules'][str(milestone['ap_location_id'])] = 'dungeon_' + region
    # Historical internal aliases retained for existing recipe graph consumers.
    if end >= 2:
        nodes['dungeon_yarden'] = [[{'node': 'dungeon_yarden_ruin' if end >= 3 else 'dungeon_yarden_mountain'}]]
    if end >= 4:
        nodes['dungeon_valley'] = [[{'node': 'dungeon_brigandine' if end >= 5 else 'dungeon_owatatsu'}]]
    profile['combat_tiers'] = {r: COMBAT_TIERS[r] for r in REGIONS[:end+1]}


def apply_deadline_rules(world, locations, profile):
    """Each progressive copy has its own deadline, not the family's last tier."""
    if 'pool_deadlines' not in profile:
        return
    from worlds.generic.Rules import add_item_rule
    regions = {r['ap_location_id']: r['region'] for r in profile['milestones']}
    for loc in locations:
        # Selected mission checks are the opening tutorial set. Orb progression
        # stays excluded by its existing independent rule.
        region = regions.get(loc.address, 'geezah')
        rank = REGIONS.index(region)
        add_item_rule(loc, lambda item, rank=rank:
            not getattr(item, 'craftopia_deadline', None) or
            (item.player == world.player and rank <= REGIONS.index(item.craftopia_deadline)))
    items = [i for i in world.multiworld.itempool if i.player == world.player
             and getattr(i, 'craftopia_deadline', None)]
    choices = [(item, [loc for loc in locations if loc.name not in world.options.exclude_locations.value
                      and loc.item_rule(item)]) for item in items]
    choices.sort(key=lambda pair: len(pair[1]))
    matched = {}
    def place(index, seen):
        for loc in choices[index][1]:
            if loc.address in seen: continue
            seen.add(loc.address)
            if loc.address not in matched or place(matched[loc.address], seen):
                matched[loc.address] = index
                return True
        return False
    if any(not place(i, set()) for i in range(len(choices))):
        raise ValueError('Not enough eligible checks for regional unlock deadlines after exclusions')
