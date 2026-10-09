"""Production Bridge regressions: real isolated files, no sockets/event loop."""
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import types
import unittest

sys.path.insert(0,str(Path(__file__).parent))
sys.modules.setdefault('websockets', types.ModuleType('websockets'))
spec = importlib.util.spec_from_file_location('repair_bridge', Path(__file__).with_name('craftopia_bridge.py'))
cb = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cb)

def run(c):
    try:
        c.send(None)
    except StopIteration as done:
        return done.value
    raise AssertionError('Unexpected external wait')

class Socket:
    def __init__(self): self.sent = []
    async def send(self, message): self.sent.append(json.loads(message))
    async def close(self): pass

class DeliveryRepair(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        cb.LEDGER_DIR = self.tmp.name
        self.args = types.SimpleNamespace(server='test:1', slot='TestPlayer', session='repair', password=None, game_id_dump=False, starter_choice='Sword')
        self.b = self.new()
    def tearDown(self): self.tmp.cleanup()
    def new(self):
        b = cb.Bridge(self.args)
        b.sent = []
        async def sink(m): b.sent.append(m)
        b.send_ap = sink
        b.slot_data = {'goal_objectives': True, 'goal_objective_count': 1, 'starting_combat': 1,
                       'class_to_location': {'ExampleMission': 'Example'}, 'objective_locations': {'Example': 'location:1'}}
        b.class_to_location = {'ExampleMission': 'Example'}
        b.location_name_to_id = {'Example': 1}
        b.datapackage_ready = True
        b.skill_point_item_id = 1
        b.victory_item_id = 2
        b.item_id_to_name = {3:'Production: Refiner'}
        return b
    def connect(self):
        self.b.ap_connected = True
        self.b.mod_ws = Socket()
        run(self.b.try_handshake(self.b.mod_ws, {'cmd':'connect','name':'TestPlayer','game':'Craftopia','unlock_protocol':2}))
        return self.b.mod_ws
    def receive(self): run(self.b.process_received_items({'index':0,'items':[{'item':3}]}))
    def test_pending_survives_restart(self):
        self.receive()
        self.assertEqual(self.new().ledger['pending_unlocks'], self.b.ledger['pending_unlocks'])
    def test_failed_write_retries(self):
        original = cb.save_ledger
        def fail(*a): raise OSError('disk full')
        cb.save_ledger = fail
        try: self.receive()
        finally: cb.save_ledger = original
        self.assertNotIn(0, self.b.seen_non_grant)
        self.receive()
        self.assertTrue(self.b.ledger['pending_unlocks'])
    def test_ack_keeps_history_and_mod_restart_replays(self):
        self.connect(); self.receive()
        msg = next(m for m in self.b.mod_ws.sent if m.get('cmd') == 'unlock_production')
        run(self.b.on_mod_message(dict(msg, cmd='unlock_ack')))
        self.assertFalse(any(p['receipt_id']==0 for p in self.b.ledger['pending_unlocks']))
        sock = self.connect()
        self.assertTrue(any(m.get('cmd')=='unlock_production' for m in sock.sent))
        self.assertTrue(any(u['receipt_id']==0 for u in self.b.ledger['pending_unlocks']), 'restore replay remains pending until ack')
    def test_wrong_epoch_ack_ignored(self):
        self.connect(); self.receive()
        run(self.b.on_mod_message({'cmd':'unlock_ack','receipt_id':0,'epoch':-1,'connection_token':'old','slot':'TestPlayer'}))
        self.assertTrue(any(p['receipt_id']==0 for p in self.b.ledger['pending_unlocks']))
    def test_hold_blocks_all_unlocks(self):
        self.connect(); self.b.mod_ws.sent.clear(); self.b.recovery_hold=True
        self.receive(); run(self.b.apply_starters())
        self.assertFalse(self.b.mod_ws.sent)
    def test_ack_failed_write_retains_pending(self):
        self.connect(); self.receive()
        msg=next(m for m in self.b.mod_ws.sent if m.get('cmd')=='unlock_production')
        original=cb.save_ledger
        def fail(*a): raise OSError('disk full')
        cb.save_ledger=fail
        try: run(self.b.on_mod_message(dict(msg,cmd='unlock_ack')))
        finally: cb.save_ledger=original
        self.assertTrue(any(p['receipt_id']==0 for p in self.b.ledger['pending_unlocks']))
    def test_starter_survives_bridge_restart(self):
        self.connect(); saved=self.b.ledger['starter_choice']
        fresh=self.new()
        self.assertTrue(fresh.ledger['starters_applied'])
        self.assertEqual(saved,fresh.ledger['starter_choice'])
    def test_choice_starter_actually_unlocks(self):
        self.b.slot_data['starting_combat']=0
        sock=self.connect()
        self.assertTrue(any(m.get('category')=='Sword' for m in sock.sent))
    def test_unselected_choice_not_marked_applied(self):
        self.args.starter_choice=None; self.b.slot_data['starting_combat']=0
        self.connect()
        self.assertFalse(self.b.ledger.get('starters_applied',False))
    def test_offline_goal_reannounced(self):
        run(self.b.report_check(9,'test','ExampleMission'))
        run(self.b.on_connected({'slot_data':self.b.slot_data,'checked_locations':[]}))
        self.assertTrue(any(m.get('cmd')=='StatusUpdate' for m in self.b.sent))
    def test_class_instances_count_one_catalog_objective(self):
        run(self.b.report_check(9,'test','ExampleMission'))
        run(self.b.report_check(10,'test','ExampleMission'))
        self.assertEqual(len(self.b.completed_objectives),1)
    def test_deferred_restart_records_objective(self):
        self.b.datapackage_ready=False
        run(self.b.report_check(9,'test','ExampleMission'))
        b=self.new(); b.ap_connected=True
        run(b.on_datapackage({'data':{'games':{'Craftopia':{'location_name_to_id':{'Example':1},'item_name_to_id':{'Skill Point':1}}}}}))
        self.assertEqual(b.completed_objectives, {'location:1'})
        self.assertTrue(any(m.get('cmd')=='StatusUpdate' for m in b.sent))
    def test_goal_failure_does_not_mutate_ledger(self):
        before=json.dumps(self.b.ledger,sort_keys=True)
        original=cb.save_ledger
        def fail(*a): raise OSError('disk full')
        cb.save_ledger=fail
        try: self.assertFalse(self.b.record_objective_complete('location:1'))
        finally: cb.save_ledger=original
        self.assertEqual(before,json.dumps(self.b.ledger,sort_keys=True))
    def test_strict_age_and_objective(self):
        for value in [True,2.9,'2',0,-1]: self.assertFalse(self.b.record_age_advance(value))
        for value in ['', ' ', 'not-in-catalog']: self.assertFalse(self.b.record_objective_complete(value))
    def test_missing_boss_not_evidence(self):
        self.b.slot_data={'goal_boss':True,'goal_boss_choice':0}
        run(self.b.on_mod_message({'cmd':'boss_defeat'}))
        self.assertFalse(self.b.check_goal_complete())
    def test_malformed_ledger_preserved(self):
        path=Path(self.b.ledger_path); path.write_text('{"format_version":2,"epoch":true}')
        original=path.read_bytes(); fresh=self.new()
        self.assertTrue(fresh.recovery_hold)
        self.assertEqual(path.read_bytes(),original)
    def test_new_seed_holds_original_history(self):
        run(self.b.on_ap_message({'cmd':'RoomInfo','seed_name':'seed-A'}))
        self.receive()
        original=Path(self.b.ledger_path).read_bytes()
        run(self.b.on_ap_message({'cmd':'RoomInfo','seed_name':'seed-B'}))
        self.assertTrue(self.b.recovery_hold)
        self.assertEqual(Path(self.b.ledger_path).read_bytes(),original)
    def test_stale_mission_cannot_complete_new_session(self):
        self.connect()
        run(self.b.on_mod_message({'cmd':'check','mission_id':9,'mission_class':'ExampleMission','epoch':-1,'slot':'TestPlayer'}))
        self.assertFalse(self.b.completed_objectives)
    def test_bound_mission_still_counts(self):
        self.connect()
        run(self.b.on_mod_message({'cmd':'check','mission_id':9,'mission_class':'ExampleMission',
                                  'epoch':self.b.ledger['epoch'],'slot':'TestPlayer'}))
        self.assertEqual(self.b.completed_objectives,{'location:1'})
    def test_ap_indices_cannot_impersonate_starter_receipts(self):
        for index in [True,-1,1.5,'0']:
            run(self.b.process_received_items({'index':index,'items':[{'item':3}]}))
            self.assertFalse(self.b.ledger['unlocks'])
    def test_maintenance_retains_work_on_exception(self):
        class StopTick(BaseException): pass
        calls=[]
        async def sleep(_):
            if calls: raise StopTick()
            calls.append(1)
        async def fail(_): raise OSError('transient send failure')
        packet={'index':0,'items':[{'item':3}]}
        self.b.retry_item_messages=[packet]
        self.b.process_received_items=fail
        old=cb.asyncio.sleep;cb.asyncio.sleep=sleep
        try:
            with self.assertRaises(StopTick):run(self.b.maintenance())
        finally:cb.asyncio.sleep=old
        self.assertEqual(self.b.retry_item_messages,[packet])

if __name__=='__main__': unittest.main()

