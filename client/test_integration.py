"""Real AP 0.6.8 slot-data/bridge integration. No AP installation writes."""
import json
import os
from pathlib import Path
import sys
import tempfile
import types
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'worlds/craftopia/test'))
from ap_runtime import load_world
World=load_world()
from test.general import setup_multiworld
mw=setup_multiworld(World,seed=1,options={'goal_objective_count':2})
world=mw.worlds[1]
slot_data=world.fill_slot_data()
sys.path.insert(0,str(ROOT/'client'))
sys.modules['websockets']=types.ModuleType('websockets')
import craftopia_bridge as cb

def run(c):
    try: c.send(None)
    except StopIteration as done:return done.value
    raise AssertionError('unexpected external wait')
class Sock:
    def __init__(self):self.sent=[]
    async def send(self,s):self.sent.append(json.loads(s))
    async def close(self):pass
class Integration(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(); cb.LEDGER_DIR=self.tmp.name
        self.args=types.SimpleNamespace(server='localhost:1',slot='TestPlayer',session='test',password=None,game_id_dump=False,starter_choice='Sword')
        self.b=self.new()
        self.classes=list(world.class_to_location)[:2]
    def tearDown(self):self.tmp.cleanup()
    def new(self):
        b=cb.Bridge(self.args);b.sent=[]
        async def sink(m):b.sent.append(m)
        b.send_ap=sink
        return b
    def ap(self,b=None):
        b=b or self.b
        run(b.on_ap_message({'cmd':'RoomInfo','seed_name':'integration-seed'}))
        run(b.on_connected({'slot_data':slot_data,'checked_locations':[]}))
    def data(self,b=None):
        run((b or self.b).on_datapackage({'data':{'games':{'Craftopia':world.get_data_package_data()}}}))
    def mod(self):
        self.b.mod_ws=Sock()
        self.assertTrue(run(self.b.try_handshake(self.b.mod_ws,{'cmd':'connect','name':'TestPlayer','game':'Craftopia','unlock_protocol':2})))
    def test_ap_first(self):
        self.ap();self.mod()
        self.assertTrue(any(m['cmd']=='session' for m in self.b.mod_ws.sent))
        self.assertTrue(any(m.get('category')=='Sword' for m in self.b.mod_ws.sent))
    def test_mod_first(self):
        self.mod();self.assertFalse(self.b.mod_ws.sent);self.ap()
        self.assertTrue(any(m.get('category')=='Sword' for m in self.b.mod_ws.sent))
    def test_catalog_routing(self):
        self.ap();self.data();run(self.b.report_check(42,'test',self.classes[0]))
        name=world.class_to_location[self.classes[0]]
        self.assertIn(slot_data['objective_locations'][name],self.b.completed_objectives)
    def test_exact_goal_count(self):
        self.ap();self.data();run(self.b.report_check(42,'test',self.classes[0]))
        self.assertFalse(self.b.check_goal_complete())
        run(self.b.report_check(43,'test',self.classes[1]))
        self.assertTrue(self.b.check_goal_complete())
        self.assertTrue(any(m.get('status')==30 for m in self.b.sent))
    def test_deferred_restart(self):
        self.ap();run(self.b.report_check(42,'test',self.classes[0]))
        fresh=self.new();self.ap(fresh);self.data(fresh)
        self.assertEqual(len(fresh.completed_objectives),1)
        self.assertTrue(any(m['cmd']=='LocationChecks' for m in fresh.sent))
    def test_victory_alone_cannot_win(self):
        self.ap();run(self.b.on_victory())
        self.assertFalse(self.b.check_goal_complete())
        self.assertFalse(any(m.get('status')==30 for m in self.b.sent))
    def test_pending_until_server_confirms(self):
        self.ap();self.data();run(self.b.report_check(42,'test',self.classes[0]))
        self.assertEqual(len(self.b.ledger['pending_checks']),1)
        loc=self.b.ledger['pending_checks'][0]['location_id']
        run(self.b.on_room_update({'checked_locations':[loc]}))
        self.assertEqual(self.b.ledger['pending_checks'],[])
        self.assertEqual(len(self.b.completed_objectives),1)
    def test_reconnect_restores_same_starter(self):
        self.ap();self.mod()
        first=self.b.ledger['starter_choice'];epoch=self.b.ledger['epoch']
        self.b=self.new();self.ap();self.mod()
        self.assertEqual(self.b.ledger['starter_choice'],first)
        self.assertEqual(self.b.ledger['epoch'],epoch)
if __name__=='__main__':unittest.main()
