import asyncio, json, os, pathlib, sys, tempfile
# Explicit environment path to the official AP 0.6.8 NetUtils checkout
# Set CRAFTOPIA_AP_SOURCE to that directory. The bridge itself does
# not require this checkout.
sys.path.insert(0, os.environ['CRAFTOPIA_AP_SOURCE'])
from NetUtils import encode, NetworkItem
import craftopia_client as cc
from types import SimpleNamespace

async def main():
    with tempfile.TemporaryDirectory() as td:
        cc.LEDGER_DIR = td
        args = SimpleNamespace(server='localhost:9999', slot='Player1', session='regression')
        b = cc.Bridge(args)
        packet = json.loads(encode([{'cmd':'ReceivedItems','index':7,
            'items':[NetworkItem(123,1,1,0),NetworkItem(456,2,1,1)]}]))[0]
        await b.on_ap_message(packet)
        assert b.unacked == [7,8] and b.ledger['delivered'] == []
        await b.on_ap_message(packet)
        assert b.unacked == [7,8], 'replay duplicated pending items'
        sent=[]
        class Socket:
            async def send(self, data): sent.append(json.loads(data))
        b.mod_ws=Socket(); b.mod_validated=True
        await b.send_grant(b.unacked)
        assert sent[-1]['indices']==[7,8]
        epoch=b.ledger['epoch']
        await b.on_grant_ack({'epoch':epoch,'indices':[7,8]})
        assert b.ledger['delivered']==[7,8] and not b.unacked
        restarted=cc.Bridge(args)
        assert restarted.ledger['epoch']==epoch
        await restarted.on_ap_message(packet)
        assert not restarted.unacked and restarted.ledger['delivered']==[7,8]
        print('PASS official AP0.6.8 wire format, nonzero index, offline queue, replay, ack, restart')
asyncio.run(main())
