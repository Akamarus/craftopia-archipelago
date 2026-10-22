"""Generate an isolated example against a configured AP 0.6.8 source tree.

Does not start a server, install a world, or modify a running session.
Set CRAFTOPIA_AP_SOURCE to an existing AP checkout with its dependencies.
"""
import argparse
import os
from pathlib import Path
import shutil
import sys
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=20261016)
    parser.add_argument('--ending', choices=('geezah', 'millewind', 'yarden_mountain', 'yarden_ruin'), default='geezah')
    args = parser.parse_args()
    output = args.output.resolve()
    root = Path(__file__).resolve().parents[1]
    players = output / 'players'
    players.mkdir(parents=True, exist_ok=True)
    temporary = output / 'tmp'
    temporary.mkdir(exist_ok=True)
    tempfile.tempdir = str(temporary)
    template = (root / 'worlds/craftopia/test/regional_player.yaml').read_text()
    template = template.replace('name: CraftopiaRegional', 'name: ' + args.ending.title() + 'Run')
    template = template.replace('ending_region: geezah', 'ending_region: ' + args.ending)
    (players / 'CraftopiaRegional.yaml').write_text(template)
    os.environ['SKIP_REQUIREMENTS_UPDATE'] = '1'
    sys.path.insert(0, str(root / 'worlds/craftopia/test'))
    from ap_runtime import load_world
    world = load_world()
    import worlds
    worlds.network_data_package['games']['Craftopia'] = world.get_data_package_data()
    os.chdir(output)
    import Generate
    import Main
    options = Generate.mystery_argparse(['--player_files_path', str(players), '--outputpath', str(output),
        '--seed', str(args.seed), '--multi', '1', '--spoiler', '2'])
    generated, seed = Generate.main(options)
    multiworld = Main.main(generated, seed)
    assert multiworld.can_beat_game()
    data = multiworld.worlds[1].fill_slot_data()
    assert data['goal_location_ids'] == {'geezah': [3002, 4100], 'millewind': [3012, 4101],
        'yarden_mountain': [3022, 4102], 'yarden_ruin': [3032, 4103]}[args.ending]
    print(f'Regional example generated: {multiworld.seed_name}, {len([l for l in multiworld.get_locations(1) if l.address is not None])} checks, tower AND dungeon goal')


if __name__ == '__main__': main()
