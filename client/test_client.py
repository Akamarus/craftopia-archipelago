#!/usr/bin/env python3
"""Headless tests for craftopia_client.py delivery logic.

Run: python3 test_client.py
Covers the Codex review blockers: no silent loss, no restart duplicates,
offline-check survival (no heuristic deletion), epoch-gated acks,
slot-identity abort, and commit-after-save ledger semantics.
"""
import asyncio
import importlib.util
import json
import os
import sys
import tempfile

CLIENT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                      "craftopia_client.py")

failures = []


def check(cond, label, detail=""):
    if cond:
        print(f"  PASS {label}  [{detail}]")
    else:
        print(f"  FAIL {label}  [{detail}]")
        failures.append(label)


def load_client(tmpdir, argv_extra=None):
    spec = importlib.util.spec_from_file_location("cc_test", CLIENT)
    cc = importlib.util.module_from_spec(spec)
    sys.argv = ["craftopia_client.py", "--server", "localhost:9999",
                "--slot", "Player1"] + (argv_extra or [])
    spec.loader.exec_module(cc)
    # isolate ledger files per test (must be set AFTER exec, which defines it)
    cc.LEDGER_DIR = tmpdir
    return cc


def make_args(cc, **kw):
    d = {"server": "localhost:9999", "slot": "Player1", "game": "APQuest",
         "password": None, "port": 11341, "session": "default",
         "reset_ledger": False}
    d.update(kw)
    return type("A", (), d)()


async def test_session_isolation():
    print("test_session_isolation: ledgers are per-session")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b1 = cc.Bridge(make_args(cc, session="seed1"))
    b1.ledger["delivered"] = [0]
    b1.save()
    b2 = cc.Bridge(make_args(cc, session="seed2"))
    check(b2.ledger["delivered"] == [], "separate-file", f"delivered={b2.ledger['delivered']}")
    check(b1.ledger_path != b2.ledger_path, "separate-path", "")
    check(b1.ledger["epoch"] != b2.ledger["epoch"], "distinct-epochs",
          f"{b1.ledger['epoch']} vs {b2.ledger['epoch']}")


async def test_no_heuristic_deletion():
    print("test_no_heuristic_deletion: offline check survives reconnect with empty checked set")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    # Codex #3 repro: claim while server disconnected, on an otherwise fresh seed
    b.ap_connected = False
    await b.report_check(101, "First Mission")
    assert len(b.ledger["pending_checks"]) == 1
    sent = []

    async def fake_send_ap(msg):
        sent.append(msg)

    async def fake_send_mod(msg):
        pass

    b.send_ap = fake_send_ap
    b.send_mod = fake_send_mod
    # reconnect: server reports ZERO checked locations (fresh seed)
    await b.on_connected({"cmd": "Connected", "team": 0, "slot": 1,
                          "checked_locations": [], "slot_data": {}})
    check(len(b.ledger["delivered"]) == 0, "no-delivered-wipe", "")
    check(b.ledger["pending_checks"] == [], "queue-reconciled-after-send", "")
    check({"cmd": "LocationChecks", "locations": [1]} in sent,
          "queued-check-sent", f"sent={sent}")


async def test_stale_epoch_ack_ignored():
    print("test_stale_epoch_ack_ignored")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    await b.on_grant_ack({"cmd": "grant_ack", "epoch": b.ledger["epoch"] + 999,
                          "indices": [0]})
    check(b.unacked == [0] and b.ledger["delivered"] == [],
          "stale-ack-ignored", f"unacked={b.unacked}")
    await b.on_grant_ack({"cmd": "grant_ack", "epoch": b.ledger["epoch"],
                          "indices": [0]})
    check(b.ledger["delivered"] == [0] and b.unacked == [],
          "current-epoch-ack-accepted", "")


async def test_commit_after_save():
    print("test_commit_after_save: ledger IO failure keeps items unacked")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0, 1]
    real_save = cc.save_ledger

    def boom(path, data):
        raise OSError("disk on fire")

    cc.save_ledger = boom
    await b.on_grant_ack({"cmd": "grant_ack", "epoch": b.ledger["epoch"],
                          "indices": [0, 1]})
    check(b.ledger["delivered"] == [] and b.unacked == [0, 1],
          "nothing-committed-on-io-failure", f"delivered={b.ledger['delivered']}")
    cc.save_ledger = real_save
    await b.on_grant_ack({"cmd": "grant_ack", "epoch": b.ledger["epoch"],
                          "indices": [0, 1]})
    check(sorted(b.ledger["delivered"]) == [0, 1] and b.unacked == [],
          "committed-after-retry", "")


async def test_replay_suppression():
    print("test_replay_suppression: server replay after restart grants nothing twice")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    await b.on_grant_ack({"cmd": "grant_ack", "epoch": b.ledger["epoch"],
                          "indices": [0]})
    # simulate restart: new instance, same session file
    b2 = cc.Bridge(make_args(cc))
    granted = []

    async def fake_send(indices):
        granted.extend(indices)

    b2.send_grant = fake_send
    await b2.on_received_items({"cmd": "ReceivedItems", "index": 0,
                                "items": [[10, 1, 1, 0], [11, 2, 1, 0]]})
    check(granted == [1], "only-new-index-granted", f"granted={granted}")


async def test_location_mapping():
    print("test_location_mapping: distinct missions -> distinct locations")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    l1 = b.location_for_mission(101)
    l2 = b.location_for_mission(202)
    l1b = b.location_for_mission(101)
    check((l1, l2, l1b) == (1, 2, 1), "mapping", f"{l1},{l2},{l1b}")
    check(set(cc.APQUEST_LOCATION_POOL) >= {1, 2, 3, 4, 5, 6, 10}, "pool", "")


class FakeWS:
    """Minimal async-iterable websocket double for mod_handler tests.

    Scripted messages are preloaded; unless stay_open=True the stream ends
    after them. With stay_open=True the socket stays open until push()/eof()
    are called, which lets tests hold two overlapping connections.
    """
    def __init__(self, incoming=(), stay_open=False):
        self.queue = asyncio.Queue()
        for m in incoming:
            self.queue.put_nowait(m)
        if not stay_open:
            self.queue.put_nowait(None)  # EOF
        self.sent = []
        self.closed = False

    def __aiter__(self):
        return self._gen()

    async def _gen(self):
        while True:
            m = await self.queue.get()
            if m is None:
                return
            yield m

    async def send(self, data):
        self.sent.append(json.loads(data))

    async def close(self):
        self.closed = True

    async def push(self, s):
        await self.queue.put(s)

    async def eof(self):
        await self.queue.put(None)


def grant_indices(sent):
    return [m["indices"] for m in sent if m.get("cmd") == "grant_items"]


async def test_no_grants_before_handshake():
    print("test_no_grants_before_handshake: socket with no handshake gets nothing")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    ws = FakeWS([])
    await b.mod_handler(ws)
    check(grant_indices(ws.sent) == [], "no-grants", f"sent={ws.sent}")
    check(b.mod_validated is False, "not-validated", "")


async def test_non_handshake_first_message_rejected():
    print("test_non_handshake_first_message_rejected")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    ws = FakeWS(['{"cmd":"check","mission_id":1,"mission_title":"x"}'])
    await b.mod_handler(ws)
    check(grant_indices(ws.sent) == [], "no-grants", "")
    check(ws.closed is True, "socket-closed", "")
    check(b.mod_validated is False, "not-validated", "")


async def test_blank_handshake_rejected():
    print("test_blank_handshake_rejected: missing/blank slot name gets nothing")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    for payload in ('{"cmd":"connect"}',
                    '{"cmd":"connect","name":""}',
                    '{"cmd":"connect","name":"   "}'):
        b = cc.Bridge(make_args(cc))
        b.unacked = [0]
        ws = FakeWS([payload])
        await b.mod_handler(ws)
        check(grant_indices(ws.sent) == [] and ws.closed is True,
              "rejected", f"payload={payload}")
        check(b.mod_validated is False, "not-validated", "")


async def test_mismatch_handshake_aborts():
    print("test_mismatch_handshake_aborts")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    ws = FakeWS(['{"cmd":"connect","name":"SomeoneElse"}'])
    try:
        await b.mod_handler(ws)
        check(False, "aborted", "no SystemExit raised")
    except SystemExit as e:
        check(e.code == 1, "aborted", f"exit code {e.code}")
    check(grant_indices(ws.sent) == [], "no-grants-before-abort", "")


async def test_matching_handshake_releases_grants():
    print("test_matching_handshake_releases_grants")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0, 1]
    ws = FakeWS(['{"cmd":"connect","name":"Player1"}'])
    await b.mod_handler(ws)
    check(b.mod_validated is False, "validation-reset-on-disconnect", "")
    got = grant_indices(ws.sent)
    check(got == [[0, 1]], "pending-grants-released", f"grants={got}")


async def test_reconnect_requires_validation():
    print("test_reconnect_requires_validation")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    # first socket validates and disconnects
    ws1 = FakeWS(['{"cmd":"connect","name":"Player1"}'])
    await b.mod_handler(ws1)
    check(grant_indices(ws1.sent) == [[0]], "first-validated", "")
    # second socket never handshakes: must get nothing even though unacked
    # items are still pending (the first grant was never acked)
    check(b.unacked == [0], "still-unacked", f"unacked={b.unacked}")
    ws2 = FakeWS([])
    await b.mod_handler(ws2)
    check(grant_indices(ws2.sent) == [], "second-socket-gets-nothing", "")
    # on_received_items also routes through the gate
    b.ap_connected = True
    await b.on_received_items({"cmd": "ReceivedItems", "index": 5,
                               "items": [[9, 9, 1, 0]]})
    check(b.unacked == [0, 5], "new-items-queued-not-sent",
          f"unacked={b.unacked}")


async def test_overlapping_connection_rejected():
    # Exact regression: A owns the slot with a pending handshake; B connects
    # and must be rejected without touching A's state. When A then completes
    # a valid handshake, grants go to A — never to B. Cleanup is bounded so
    # a regression fails instead of hanging.
    print("test_overlapping_connection_rejected")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    wsA = FakeWS(stay_open=True)
    wsB = FakeWS(stay_open=True)
    taskA = asyncio.create_task(b.mod_handler(wsA))
    await asyncio.sleep(0.01)
    taskB = asyncio.create_task(b.mod_handler(wsB))
    await asyncio.sleep(0.01)
    try:
        check(wsB.closed is True, "second-rejected", "")
        check(b.mod_ws is wsA, "owner-undisturbed", "")
        check(b.mod_validated is False, "owner-still-pending", "")
        # the owner completes its handshake: grants must go to A only
        await wsA.push('{"cmd":"connect","name":"Player1"}')
        await asyncio.sleep(0.01)
        check(b.mod_validated is True, "owner-validated", "")
        check(grant_indices(wsA.sent) == [[0]], "grants-to-owner",
              f"a={grant_indices(wsA.sent)}")
        check(grant_indices(wsB.sent) == [], "intruder-gets-nothing",
              f"b={grant_indices(wsB.sent)}")
    finally:
        # End both streams, then require both handlers to finish promptly.
        await wsA.eof()
        await wsB.eof()
        for t, label in ((taskA, "A"), (taskB, "B")):
            try:
                await asyncio.wait_for(t, timeout=2)
            except asyncio.TimeoutError:
                t.cancel()
                try:
                    await t
                except (asyncio.CancelledError, Exception):
                    pass
                check(False, "no-hang", f"handler {label} did not finish")
    check(b.mod_ws is None and b.mod_validated is False, "owner-cleanup", "")


async def test_fresh_connection_after_owner_disconnect():
    print("test_fresh_connection_after_owner_disconnect")
    tmp = tempfile.mkdtemp()
    cc = load_client(tmp)
    b = cc.Bridge(make_args(cc))
    b.unacked = [0]
    wsA = FakeWS(['{"cmd":"connect","name":"Player1"}'])
    await b.mod_handler(wsA)
    check(grant_indices(wsA.sent) == [[0]], "owner-got-grants", "")
    check(b.mod_ws is None and b.mod_validated is False, "slot-released", "")
    # a fresh socket may now claim the slot and validate normally
    wsC = FakeWS(['{"cmd":"connect","name":"Player1"}'])
    await b.mod_handler(wsC)
    check(b.mod_validated is False, "validation-reset-after-disconnect", "")
    check(grant_indices(wsC.sent) == [[0]], "fresh-validates",
          f"c={grant_indices(wsC.sent)}")
    check(b.mod_ws is None, "slot-released-again", "")


async def main():
    await test_session_isolation()
    await test_no_heuristic_deletion()
    await test_stale_epoch_ack_ignored()
    await test_commit_after_save()
    await test_replay_suppression()
    await test_location_mapping()
    await test_no_grants_before_handshake()
    await test_non_handshake_first_message_rejected()
    await test_blank_handshake_rejected()
    await test_mismatch_handshake_aborts()
    await test_matching_handshake_releases_grants()
    await test_reconnect_requires_validation()
    await test_overlapping_connection_rejected()
    await test_fresh_connection_after_owner_disconnect()
    print("ALL CLIENT TESTS PASSED" if not failures else f"{len(failures)} TEST(S) FAILED")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    asyncio.run(main())
