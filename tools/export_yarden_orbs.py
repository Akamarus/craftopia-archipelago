"""Reproduce native-field membership using the versioned installed-data audit."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENV = ROOT / 'docs/research/2026-10-16/environment.json'
ORBS = ROOT / 'worlds/craftopia/soul_orbs_20261015_r1.json'
TOWERS = ROOT / 'worlds/craftopia/milestones_20261022_r3.json'
OUTPUT = ROOT / 'worlds/craftopia/yarden_orbs_20261022_r1.json'


def inside(p, c, height, radius):
    y = p['y'] - c['y']
    return ((p['x'] - c['x'])**2 + (p['z'] - c['z'])**2 < radius**2
            and (0 < y < height or height < y < 0))


def field_at(p, fields):
    winner = None
    for f in fields:
        if f['_weight'] < 0:
            continue
        for z in f['_zoneList']:
            center = {k: f['_center'][k] + z['CenterOffset'][k] - (5 if k == 'y' else 0)
                      for k in ('x', 'y', 'z')}
            if inside(p, center, z['Height'], z['Radius']):
                if winner is None or f['_weight'] > winner['_weight']:
                    winner = f
                break
    return winner['_asset_path_id'] if winner is not None else None


def export():
    env = json.loads(ENV.read_text(encoding='utf-8-sig'))
    assets = {r['_asset_path_id']: r for r in env['world_assets']}
    fields = [assets[r['m_PathID']] for r in assets[41584]['_dataArray']]
    towers = [r for r in json.loads(TOWERS.read_text(encoding='utf-8'))['locations']
              if r.get('definition_id') in (3808, 3809) and r['stage'] == 1]
    assert len(towers) == 2 and all(field_at(r['position'], fields) == 41588 for r in towers)
    orbs = json.loads(ORBS.read_text(encoding='utf-8'))['orbs']
    rows = [{k: r[k] for k in ('guid', 'ap_location_id', 'position')}
            for r in orbs if field_at(r['position'], fields) == 41588]
    assert len(rows) == len({r['guid'] for r in rows}) == 49
    assert assets[41588]['envDmgType'] == 0
    for row in rows:
        p = row['position']
        for dx in (-.01, .01):
            for dy in (-.01, .01):
                for dz in (-.01, .01):
                    assert field_at(dict(x=p['x']+dx, y=p['y']+dy, z=p['z']+dz), fields) == 41588
    return dict(schema=1, region='yarden', traversal_verified=False,
        evidence='docs/research/yarden-20261021/orb-membership.md',
        native_field_id=41588, native_field_name=assets[41588]['m_Name'],
        source_sha256={str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                       for p in (ENV, ORBS, TOWERS)}, locations=rows)


if __name__ == '__main__':
    OUTPUT.write_text(json.dumps(export(), indent=2)+'\n', encoding='utf-8')
    print('Exported49 Yarden orbs; both towers share native field41588; perturbation checks passed.')
