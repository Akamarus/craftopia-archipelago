"""Full generation through AP's real archive discovery, never extracted source.
Run with AP 0.6.8 Python and CRAFTOPIA_AP_SOURCE set.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import time
import traceback
import unittest  # Enables AP's scoped world loader without unrelated ROM worlds.

REGIONS = ('geezah','millewind','yarden_mountain','yarden_ruin','owatatsu','brigandine')

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apworld',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--matrix',action='store_true')
    parser.add_argument('--runs',type=int,default=1)
    parser.add_argument('--mixed',action='store_true')
    parser.add_argument('--players',type=int,default=2,help='Player count for mixed-game cases')
    args=parser.parse_args()
    if args.players < 2:parser.error('--players must be at least 2')
    archive=args.apworld.resolve();out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
    temp=out/'temporary';temp.mkdir(exist_ok=True);tempfile.tempdir=str(temp)
    installed=out/'installation'/'worlds';installed.mkdir(parents=True,exist_ok=True)
    shutil.copy2(archive,installed/'craftopia.apworld')
    source=Path(os.environ['CRAFTOPIA_AP_SOURCE']).resolve();sys.path.insert(0,str(source))
    os.environ['AP_TEST_WORLDS']='apquest,craftopia';os.environ['SKIP_REQUIREMENTS_UPDATE']='1'
    import Utils
    assert tuple(Utils.version_tuple)==(0,6,8)
    Utils.user_path.cached_path=str(installed.parent)
    import worlds
    assert 'Craftopia' in worlds.AutoWorldRegister.world_types,worlds.failed_world_loads
    import worlds.craftopia as craftopia
    assert '.apworld' in craftopia.__file__,craftopia.__file__
    import Generate,Main,yaml
    root=Path(__file__).resolve().parents[1]
    template=yaml.safe_load((root/'worlds/craftopia/Craftopia.yaml').read_text())
    results=[]
    for region in REGIONS if args.matrix else ('geezah',):
      for mode in ('solo','mixed','two_craftopia','craftopia_second') if args.matrix else (('mixed',) if args.mixed else ('solo',)):
       for run in range(args.runs):
        label=f'{region}-{mode}-{run}';folder=out/label;players=folder/'players';players.mkdir(parents=True,exist_ok=True)
        data=json.loads(json.dumps(template));data['name']='Akamarus';data['Craftopia']['ending_region']=region
        data['Craftopia']['starting_combat']=('player_choice','random_category','all_basic')[run%3]
        (players/('Z.yaml' if mode=='craftopia_second' else 'A.yaml')).write_text(yaml.safe_dump(data,sort_keys=False),encoding='utf-8')
        if mode!='solo':
          other={'name':'OtherGame','game':'APQuest','APQuest':{}}
          if mode=='two_craftopia':other=json.loads(json.dumps(data));other['name']='OtherCraftopia'
          (players/('A.yaml' if mode=='craftopia_second' else 'B.yaml')).write_text(yaml.safe_dump(other,sort_keys=False),encoding='utf-8')
          if mode in ('mixed','craftopia_second'):
            for extra in range(2,args.players):
              (players/f'B{extra:03}.yaml').write_text(yaml.safe_dump(
                  {'name':f'OtherGame{extra}','game':'APQuest','APQuest':{}},sort_keys=False),encoding='utf-8')
        start=time.monotonic();record=dict(case=label)
        try:
          os.chdir(folder)
          count=1 if mode=='solo' else 2 if mode=='two_craftopia' else args.players
          opts=Generate.mystery_argparse(['--player_files_path',str(players),'--outputpath',str(folder),'--seed',str(2026102700+run),'--multi',str(count),'--spoiler','2'])
          gen,seed=Generate.main(opts);mw=Main.main(gen,seed)
          assert mw.can_beat_game(),'not beatable'
          placements=[];cross=[]
          for loc in mw.get_locations():
            if loc.item:
              assert loc.item.location is loc
              placements.append((loc.player,loc.name,loc.item.player,loc.item.name))
              if loc.item.advancement and loc.item.player!=loc.player:cross.append((loc.player,loc.name,loc.item.player,loc.item.name))
          for player,w in mw.worlds.items():
            if w.game!='Craftopia':continue
            slot=w.fill_slot_data();assert slot['starter_kit_protocol']==1
            assert len(slot['goal_location_ids'])==2
            early=w.regional_profile['early_item_locations']
            # AP clears local_items in solo runs; every placement is local there.
            if mw.players > 1:
              assert set(early).issubset(w.options.local_items.value)
            for loc in mw.get_locations():
              if not loc.item or loc.item.player!=player:continue
              if loc.item.name in early:
                assert loc.player==player and loc.address in early[loc.item.name],loc
              if loc.player==player:
                assert loc.item_rule(loc.item),loc
          record.update(status='passed',seconds=round(time.monotonic()-start,3),cross_progression=len(cross),placement_hash=hashlib.sha256(json.dumps(sorted(placements)).encode()).hexdigest())
        except Exception:
          record.update(status='failed',traceback=traceback.format_exc())
        results.append(record);(out/'results.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
        print(label,record['status'],flush=True)
    print(json.dumps({'passed':sum(r['status']=='passed' for r in results),'total':len(results)}),flush=True)
    if any(r['status']=='failed' for r in results):sys.exit(1)

if __name__=='__main__':main()
