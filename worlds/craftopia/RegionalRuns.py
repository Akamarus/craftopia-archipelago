"""Region-bounded runs. Keep the historical Geezah pilot contract unchanged.

Only an explicitly described region can be an ending. Geography is a selection
boundary, not an invented combat-level barrier. Resource and combat assumptions
remain experimental even when the corresponding checks have passed live tests.
"""
from collections import Counter
from .Expedition import build_profile
from .Milestones import load_catalog, validate_catalog
from .mission_id_mapper import get_id_map
from .Progression import ProgressionModel
from .AuditedProgression import material_nodes, repair_nodes, evolution_node, tower_costs, age_cost, CATALOG_ID


PROFILE_ID = 'regional_geezah_20261016_r5'
SUPPORTED_ENDINGS = ('geezah',)


def build_run_profile(ending_region, item_table):
    if ending_region not in SUPPORTED_ENDINGS:
        raise ValueError(f'Ending region {ending_region!r} is not ready: dungeon identity, '
                         'regional check selection and material/survival rules are required')
    def item(name, count=1): return dict(item=name, count=count)
    def node(name): return dict(node=name)
    def production(name): return item('Production: ' + name)
    def equipment(name, count=1): return item('Progressive Equipment: ' + name, count)

    # Repeatable material capabilities, not inventory balances. See the evidence
    # table in REGIONAL_RUNS.md for item IDs and remaining resource assumptions.
    nodes = {
        'field': [[]],
        'stone_tools': [[node('field')]],
        'workbench': [[node('stone_tools')]],
        'local_sand': [[node('stone_tools')]],
        'local_wildlife': [[node('field')]],
        'old_rag': [[node('workbench'), node('field')]],
        'copper_ore': [[node('stone_tools')]],
        'placeable_torch': [[node('old_rag'), node('field')]],
        'tower_1': [[node('local_sand'), node('field')]],
        'tower_2': [[node('tower_1'), node('old_rag'), node('local_wildlife')]],
        'tower_3': [[node('tower_2'), node('copper_ore'), node('placeable_torch')]],
        'age_materials': [[node('field')]],
        'age_1': [[node('tower_3'), node('age_materials')]],
        'furnace': [[node('age_1'), node('workbench')]],
        'copper_ingot': [[node('furnace'), node('copper_ore'), production('smelting')]],
        'blacksmith': [[node('copper_ingot'), node('workbench')]],
        'cooking_pot': [[node('copper_ingot'), node('workbench')]],
        'stone_sword': [[node('workbench'), equipment('Sword')]],
        'glider': [[node('workbench'), node('local_wildlife')]],
        'campfire': [[node('field')]],
        'updraft': [[node('glider'), node('campfire'), production('cooking')]],
        'market': [[node('workbench')]],
        'wheat_field': [[node('age_1'), node('workbench')]],
        'water_surface': [[node('field')]],
        'water_bucket': [[node('material_1919'), node('water_surface')]],
        'farm_use': [[node('wheat_field'), node('water_bucket'), production('farming')]],
        # Capabilities for an actual fight, not gates on geographic travel.
        'goblin': [[node('stone_sword')], [node('workbench'), equipment('Bow')],
                   [item('Skill Tree: Tamer'), item('Skill Point', 3), node('copper_ingot')]],
        'dungeon_geezah': [
            [node('age_1'), node('blacksmith'), equipment('Sword', 2)],
            [node('age_1'), node('workbench'), equipment('Bow')],
            [node('age_1'), node('copper_ingot'), item('Skill Tree: Tamer'), item('Skill Point', 3)]],
    }
    missions = {}
    # Keep the existing explicit Geezah gathering assumptions; derive crafted
    # material dependencies from audited IDs instead of hand-written aliases.
    raw_sources = {iid: [[node(route)]] for iid, route in {
        698: 'stone_tools', 752: 'local_sand', 746: 'field', 2454: 'field',
        758: 'local_wildlife', 683: 'local_wildlife', 699: 'copper_ore'}.items()}
    costs = {stage: tower_costs(3806, stage) for stage in (1, 2, 3)}
    gold, age_items = age_cost(0)
    if gold != 0: raise ValueError('Geezah opening model has no reviewed gold source')
    # Exact ore/wood source definitions accept rank-zero stone tools. The other
    # two Plateau age materials are pickups; no arbitrary metal-tool gate.
    raw_sources.update({iid: [[node('stone_tools' if iid in (6576, 6577) else 'field')]] for iid in age_items})
    raw_sources[839] = [[node('water_bucket')]]
    ages = {0:'field', 1:'age_1'}
    stations = {20002:'field', 20011:'workbench', 20018:'furnace'}
    construction = {'workbench':200, 'furnace':356, 'blacksmith':272,
                    'cooking_pot':248, 'wheat_field':296, 'glider':141,
                    'market':500, 'campfire':404, 'copper_ingot':711}
    nodes.update(material_nodes(set(construction.values()) | {1919} | set(age_items),
        sources=raw_sources, ages=ages, stations=stations))
    nodes.update(repair_nodes(3806, sources=raw_sources, ages=ages, stations=stations))
    for name, iid in construction.items():
        nodes[name] = [[node('material_' + str(iid))]]
    nodes['age_materials'] = [[node('material_'+str(iid)) for iid in age_items]]
    for stage in costs:
        nodes['tower_'+str(stage)] = [[node('tower_3806_' + str(stage))]]
    nodes['age_1'] = evolution_node(0, tower_node='tower_3')
    def assign(ids, requirement):
        for mid in ids:
            if str(mid) in missions: raise ValueError(f'Duplicate regional mission {mid}')
            missions[str(mid)] = requirement
    assign((50, 53, 54, 57, 514, 512), 'field')
    assign((51, 52, 55, 56), 'stone_tools')
    assign((30, 58, 95, 96, 97, 98, 563, 564), 'workbench')
    assign((7, 60, 565), 'stone_sword')
    assign((10, 59, 79), 'glider')
    assign((63,), 'campfire')
    assign((67,), 'updraft')
    assign((16, 36, 516), 'market')
    assign((70,), 'cooking_pot')
    assign((72,), 'furnace')
    assign((78,), 'blacksmith')
    assign((15,), 'wheat_field')
    # Installed mission105 targets item839; it has no source-station condition.
    assign((105,), 'water_bucket')
    assign((106, 107), 'farm_use')
    assign((566,), 'goblin')
    assign((513,), 'claim_category')

    # Exact mission targets and recipe evidence: GEEZAH_EXPANSION.md.
    nodes.update({
        'branch_bow': [[node('workbench'), equipment('Bow')]],
        'branch_arrow': [[node('workbench'), node('local_wildlife')]],
        'cow_headshot': [[node('branch_bow'), node('branch_arrow'), node('local_wildlife')]],
        'copper_shield': [[node('blacksmith'), equipment('Shield', 2), node('local_wildlife')]],
        'bread': [[node('cooking_pot'), node('farm_use'), production('cooking')]],
        'heal_skill': [[item('Skill Tree: Magic'), item('Skill Point')]],
    })
    for ids, requirement in (
        ((34,), 'cow_headshot'), ((61,), 'local_wildlife'), ((73,), 'copper_ingot'),
        ((80,), 'branch_bow'), ((81,), 'branch_arrow'), ((82,), 'copper_shield'),
        ((87,93), 'field'), ((88,), 'dungeon_geezah'), ((99,544), 'heal_skill'),
        ((519,), 'old_rag'), ((529,530), 'bread')):
        assign(ids, requirement)

    selected = {3000, 3001, 3002, 4000, 4100, 4202, 4203, 4204,
                4205, 4207, 4208, 4214, 4352, 4354}
    milestones = [r for r in validate_catalog(load_catalog()) if r['ap_location_id'] in selected]
    if {r['ap_location_id'] for r in milestones} != selected:
        raise ValueError('Missing selected regional milestone')
    rules, category_sources = {}, []
    for row in milestones:
        if row['region'] != ending_region: raise ValueError('Milestone outside selected region')
        family = row['family']
        if family == 'category':
            if not all(str(mid) in missions for mid in row['members']):
                raise ValueError('Category has an unselected member')
            name = 'category_' + str(row['source_id'])
            nodes[name] = [[node(missions[str(mid)]) for mid in row['members']]]
            category_sources.append([node(name)])
        else:
            name = {'tower': 'tower_', 'age': 'age_', 'dungeon': 'dungeon_geezah'}[family]
            if family != 'dungeon': name += str(row['stage'])
        rules[str(row['ap_location_id'])] = name
    nodes['claim_category'] = category_sources
    catalog = get_id_map()
    if any(int(mid) not in catalog for mid in missions): raise ValueError('Unknown selected mission')
    opening = (512, 50, 51, 52, 53, 514, 54, 55, 56, 57, 58, 30, 563, 564, 95, 96, 97, 98)
    early = [catalog[mid]['ap_location_id'] for mid in opening]
    pool = ['Production: smelting', 'Production: cooking', 'Production: farming',
            'Progressive Equipment: Sword', 'Progressive Equipment: Sword',
            'Progressive Equipment: Bow', 'Progressive Equipment: Bow',
            'Progressive Equipment: Body', 'Skill Tree: Tamer', 'Skill Tree: Magic',
            'Progressive Equipment: Shield', 'Progressive Equipment: Shield']
    profile = dict(schema=1, reviewed=False, experimental=True, profile_id=PROFILE_ID,
        evidence='docs/REGIONAL_RUNS.md', region=ending_region, included_regions=[ending_region],
        audited_progression_catalog=CATALOG_ID,
        nodes=nodes, missions=missions, milestone_rules=rules, milestones=milestones,
        goal_location_ids=[3002, 4100], goal_policy='regional_tower_and_dungeon',
        early_item_locations={name: list(early) for name in ('Production: smelting', 'Production: cooking')},
        pool=pool, equipment_tier_steps=build_profile(item_table)['equipment_tier_steps'])
    model = ProgressionModel(profile, item_table, allow_experimental=True)
    if not all(model.can_complete(mid, lambda _: 0) for mid in opening):
        raise ValueError('Opening production destinations require randomized permissions')
    return profile


def apply_early_placement_rules(world, locations, profile):
    """Hard constraints, including exclusions; never widen or give a free item."""
    from worlds.generic.Rules import add_item_rule
    restrictions = profile.get('early_item_locations',
        {'Production: smelting': profile.get('smelting_locations', [])})
    # Match available destinations against all required copies, not just a
    # per-item nonempty check (two items cannot occupy one remaining location).
    remaining = []
    counts = Counter(profile['pool'])
    for name, addresses in restrictions.items():
        eligible = {loc.address for loc in locations if loc.address in addresses
                    and loc.name not in world.options.exclude_locations.value
                    and loc.item_rule(world.create_item(name))}
        if not eligible: raise ValueError(f'No eligible opening {name} destination')
        remaining.extend((name, eligible) for _ in range(counts[name]))
    assigned = {}
    def place(index, visited):
        for address in sorted(remaining[index][1]):
            if address in visited: continue
            visited.add(address)
            if address not in assigned or place(assigned[address], visited):
                assigned[address] = index
                return True
        return False
    if any(not place(i, set()) for i in range(len(remaining))):
        raise ValueError('Not enough distinct opening production destinations after exclusions')
    for loc in locations:
        add_item_rule(loc, lambda item, address=loc.address:
            item.name not in restrictions or
            (item.player == world.player and address in restrictions[item.name]))


def geezah_orbs():
    """Exact native-field subset; reject drift against the frozen orb catalog."""
    import json
    from pathlib import Path
    from .SoulOrbs import load_catalog, validate_catalog
    subset = json.loads(Path(__file__).with_name('geezah_orbs_20261016_r1.json').read_text())
    canonical = {r['guid']: r for r in validate_catalog(load_catalog())}
    rows = []
    for row in subset['locations']:
        frozen = canonical[row['guid']]
        if any(row[key] != frozen[key] for key in ('ap_location_id', 'position')):
            raise ValueError('Geezah orb subset differs from frozen catalog')
        rows.append(frozen)
    if len(rows) != 51 or len({r['guid'] for r in rows}) != 51:
        raise ValueError('Geezah orb subset is incomplete or duplicated')
    return rows
