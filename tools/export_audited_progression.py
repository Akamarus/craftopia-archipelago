"""Export sanitized, build-specific definitions; never infer acquisition access."""
import argparse
import json
from pathlib import Path


def export(capture, research, output):
    read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
    data, audit, env, recipes = (read(capture), read(research/'towers-ages.json'),
                               read(research/'environment.json'), read(research/'recipes.json'))
    if data['game_assembly_sha256'].lower() != audit['assembly_sha256'].lower() or audit['assembly_sha256'] != env['assembly_sha256']:
        raise ValueError('Audit and capture builds differ')
    items = {r['id']: r for r in data['items']}
    listed = {i for bench in data['craft_bench_map'] for i in bench['item_ids']}
    pending = {r['id'] for r in recipes['recursive_item_records']} | {r['id'] for r in env['items']}
    pending.update(i['item_id'] for age in data['ages'] for i in age['requireItems'])
    pending.update(c['item_id'] for t in audit['towers'] for stage in t['stages'] for c in stage['costs'])
    pending.update([200, 206, 209, 212, 248, 272, 356, 416, 141, 296, 332, 404, 500, 1919, 711])
    rows = {}
    while pending:
        iid = pending.pop()
        if str(iid) in rows: continue
        r = items[iid]
        ingredients = {str(r[f'material{s}_Id']): r[f'material{s}_count'] for s in 'ABCDE'
                       if r.get(f'material{s}_Id', 0) > 0 and r.get(f'material{s}_count', 0) > 0}
        rows[str(iid)] = dict(name=r['display_name'], status=r['status'],
            family=r['workbenchId'], age=r['workbenchLv'], output=max(1,r['playerCraftCount']),
            ingredients=ingredients, active_recipe=iid in listed and r['status'] in (1,2)
            and r['workbenchId'] not in (0,20001) and bool(ingredients)
            and not r.get('<IsBlueprintRequired>k__BackingField', True))
        pending.update(int(i) for i in ingredients if i not in rows)
    resistance = {str(r['id']): dict(status=r['status'],
        **{key:r[field] for key,field in dict(fire='resistFireLv',cold='resistColdLv',
        poison='resistPoisonLv',hellfire='resistHellFireLv',buff_fire='buffResistFireLv',
        buff_cold='buffResistColdLv',buff_poison='buffResistPoisonLv',
        buff_hellfire='buffResistHellFireLv',duration='effectiveTime').items()}) for r in env['items']}
    result = dict(schema=1, catalog_id='progression_audit_20261016_r2',
        assembly_sha256=audit['assembly_sha256'], evidence='docs/research/2026-10-16/README.md',
        items=dict(sorted(rows.items(),key=lambda x:int(x[0]))), towers=audit['towers'],
        age_transitions=audit['age_transitions'], resistance=resistance)
    output.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(f'Exported {len(rows)} item definitions, {len(resistance)} resistance records')


if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('capture',type=Path)
    parser.add_argument('research',type=Path)
    parser.add_argument('output',type=Path)
    args=parser.parse_args()
    export(args.capture,args.research,args.output)
