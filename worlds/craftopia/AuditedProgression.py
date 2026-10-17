"""Installed-build facts, separate from explicitly reviewed acquisition routes."""
import json
from copy import deepcopy
from pathlib import Path

CATALOG_ID = 'progression_audit_20261016_r2'
PRODUCTION = {20002: None, 20011: None, 20017: None, 20013: None, 20020: None,
              20018: 'smelting', 20015: 'cooking', 20206: 'equipment_care', 20012: 'alchemy'}


def load_catalog():
    data = json.loads(Path(__file__).with_name(CATALOG_ID + '.json').read_text(encoding='utf-8'))
    if data.get('schema') != 1 or data.get('catalog_id') != CATALOG_ID:
        raise ValueError('Unsupported audited progression catalog')
    return data


def recipe_cost(item_id, quantity):
    """Immediate batch costs, rounded up. Does not assert ingredient access."""
    row = load_catalog()['items'].get(str(item_id))
    if type(quantity) is not int or quantity < 0 or not row or not row['active_recipe']:
        raise ValueError('Requires an active audited recipe and nonnegative integer quantity')
    batches = (quantity + row['output'] - 1) // row['output']
    return {int(i): count * batches for i, count in row['ingredients'].items()}


def material_nodes(targets, *, sources, ages, stations):
    """Compile repeatable capabilities for ProgressionModel's least fixed point.

    sources are explicit alternative conjunctions supplied by the regional
    profile. Missing source/station/age evidence cannot create a free route.
    Counts belong to the cost catalog; capabilities do not represent inventory.
    """
    data = load_catalog()['items']
    result = {}

    def visit(iid):
        name = 'material_' + str(iid)
        if name in result: return name
        alternatives = deepcopy(sources.get(iid, []))
        result[name] = alternatives
        row = data.get(str(iid))
        if (row and row['active_recipe'] and row['family'] in PRODUCTION
                and row['age'] in ages and row['family'] in stations):
            route = [{'node': ages[row['age']]}, {'node': stations[row['family']]}]
            group = PRODUCTION[row['family']]
            if group: route.append({'item': 'Production: '+group, 'count': 1})
            route.extend({'node': visit(int(i))} for i in row['ingredients'])
            alternatives.append(route)
        if not alternatives:
            # An explicit unsatisfied self-cycle is valid in the least-fixed-
            # point evaluator and can never manufacture its own accessibility.
            alternatives.append([{'node': name}])
        return name

    for iid in targets: visit(iid)
    return result


def tower_costs(item_id, stage):
    if type(stage) is not int or not 1 <= stage <= 3:
        raise ValueError('Tower stage must be 1..3')
    for row in load_catalog()['towers']:
        if row['item_id'] == item_id:
            return {c['item_id']: c['count'] for c in row['stages'][stage-1]['costs']}
    raise ValueError('Unknown audited tower')


def age_cost(from_world_level):
    """Cost charged at the source index, to advance to index+1."""
    if type(from_world_level) is not int:
        raise ValueError('World level must be an integer')
    for row in load_catalog()['age_transitions']:
        if row['from_world_level'] == from_world_level and row['isEnabled']:
            return row['gold'], {r['item_id']:r['count'] for r in row['requirements']}
    raise ValueError('No enabled audited age transition')


# Individually reviewed ordinary recipes; accessories and torches retain vanilla
# equipment checks. Single-item alternatives deliberately do not assume stacking.
PROTECTION_CANDIDATES = frozenset((787, 788, 2307, 2308, 2309, 2338, 2339, 2340, 2376,
                                 5546, 5548, 5550))


def protection_nodes(requirements, *, candidates, sources, ages, stations):
    """Compile sustainable recipe access to a single sufficient protection item.

    This does not assign geographical thresholds or model potion duration as
    permanent protection. Profiles must supply repeatable ingredient access.
    Hellfire is a distinct accumulation mechanic and is deliberately rejected.
    """
    candidates = set(candidates)
    if not candidates <= PROTECTION_CANDIDATES:
        raise ValueError('Protection candidate lacks reviewed ordinary acquisition/equipment policy')
    data = load_catalog()
    result = material_nodes(candidates, sources=sources, ages=ages, stations=stations)
    for name, (kind, threshold) in requirements.items():
        if kind not in ('fire', 'cold', 'poison') or type(threshold) is not int or threshold <= 0:
            raise ValueError('Unsupported environmental threshold')
        if name in result:
            raise ValueError('Protection node collides with material node')
        routes = []
        for iid in sorted(candidates):
            r = data['resistance'][str(iid)]
            value = max(r[kind], r['buff_' + kind] if r['duration'] > 0 else 0)
            if r['status'] in (1, 2) and value >= threshold:
                routes.append([{'node': 'material_' + str(iid)}])
        result[name] = routes or [[{'node': name}]]
    return result


def repair_nodes(tower_id, *, sources, ages, stations):
    """Audited three-stage sequence; missing material routes stay unreachable."""
    costs = {stage: tower_costs(tower_id, stage) for stage in (1, 2, 3)}
    result = material_nodes({iid for cost in costs.values() for iid in cost},
                            sources=sources, ages=ages, stations=stations)
    for stage, cost in costs.items():
        route = [{'node': 'material_' + str(iid)} for iid in cost]
        if stage > 1:
            route.append({'node': f'tower_{tower_id}_{stage - 1}'})
        result[f'tower_{tower_id}_{stage}'] = [route]
    return result


def evolution_node(from_world_level, *, tower_node, currency_node=None):
    """Return a conjunction; the caller supplies the actual eligible tower route."""
    gold, materials = age_cost(from_world_level)
    if gold and not currency_node:
        raise ValueError('Paid evolution requires an explicit currency capability')
    route = [{'node': tower_node}] + [{'node':'material_' + str(iid)} for iid in materials]
    if gold:
        route.append({'node': currency_node})
    return [route]
