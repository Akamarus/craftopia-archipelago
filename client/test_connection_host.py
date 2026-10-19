import asyncio
import json
import unittest
import connection_host as host

class IdentityTests(unittest.TestCase):
    def test_canonical_identity_and_boundaries(self):
        self.assertEqual(host.session_identity('LOCALHOST:38281', 'seed', 'Player'), host.session_identity('ws://localhost:38281/', 'seed', 'Player'))
        self.assertNotEqual(host.session_identity('localhost:38281', 'seed', 'Player'), host.session_identity('localhost:38281', 'other', 'Player'))
        self.assertNotEqual(host.session_identity('localhost:38281', 'seed', 'Player'), host.session_identity('localhost:38281', 'seed', 'Other'))
    def test_reject_embedded_credentials_and_invalid_ports(self):
        for server in ['ws://user:secret@localhost:1', 'localhost:0', 'localhost/path', 'wss://localhost:1']:
            with self.assertRaises(ValueError): host.normalize_server(server)
    def test_password_is_not_in_identity(self):
        config = host.parse_config({'server':'localhost:38281','slot':'Player','password':'supersecret','port':11341})
        self.assertEqual(config['password'], 'supersecret')
        self.assertNotIn('supersecret', host.session_identity(config['server'], 'seed', config['slot']))

class LifecycleTests(unittest.IsolatedAsyncioTestCase):
    async def test_prepared_requires_start_and_eof_stops_bridge(self):
        commands = asyncio.Queue()
        events = []
        started = asyncio.Event()
        stopped = asyncio.Event()
        async def bridge(config, session, seed, emit):
            started.set()
            try: await asyncio.Future()
            finally: stopped.set()
        async def discover(server): return 'seed'
        task = asyncio.create_task(host.run_host({'server':'localhost:38281','slot':'Player'}, commands, events.append, discover, bridge))
        for _ in range(10): await asyncio.sleep(0)
        self.assertEqual(events[0]['state'], 'prepared')
        self.assertFalse(started.is_set())
        commands.put_nowait({'cmd':'start'})
        await asyncio.wait_for(started.wait(), 1)
        commands.put_nowait(None)
        await asyncio.wait_for(task, 1)
        self.assertTrue(stopped.is_set())
        self.assertEqual(events[-1]['state'], 'disconnected')
    async def test_eof_cancels_discovery(self):
        commands = asyncio.Queue()
        events = []
        cancelled = asyncio.Event()
        async def discover(server):
            try: await asyncio.Future()
            finally: cancelled.set()
        task = asyncio.create_task(host.run_host({'server':'localhost:38281','slot':'Player'}, commands, events.append, discover))
        await asyncio.sleep(0)
        commands.put_nowait(None)
        await asyncio.wait_for(task, 1)
        self.assertTrue(cancelled.is_set())
        self.assertFalse(any(e['state'] == 'prepared' for e in events))
    async def test_errors_never_echo_password(self):
        async def discover(server): raise RuntimeError('supersecret')
        events = []
        await host.run_host({'server':'localhost:38281','slot':'Player','password':'supersecret'}, asyncio.Queue(), events.append, discover)
        self.assertNotIn('supersecret', json.dumps(events))
        self.assertEqual(events[-1]['state'], 'error')

class BridgeStatusTests(unittest.IsolatedAsyncioTestCase):
    async def test_refusal_reports_fixed_error(self):
        import craftopia_bridge as bridge
        from types import SimpleNamespace
        instance = object.__new__(bridge.Bridge)
        events = []
        instance.args = SimpleNamespace(state_callback=lambda state, message: events.append((state, message)))
        await instance.on_ap_message({'cmd':'ConnectionRefused','errors':['secret-from-remote']})
        self.assertEqual(events[0][0], 'error')
        self.assertNotIn('secret-from-remote', repr(events))
    async def test_seed_change_cannot_rebind_managed_profile(self):
        import craftopia_bridge as bridge
        from types import SimpleNamespace
        instance = object.__new__(bridge.Bridge)
        events = []
        instance.args = SimpleNamespace(expected_seed='original', state_callback=lambda state, message: events.append((state, message)))
        instance.ledger = {}
        await instance.on_ap_message({'cmd':'RoomInfo','seed_name':'changed'})
        self.assertTrue(instance.recovery_hold)
        self.assertEqual(events[0][0], 'error')
        self.assertEqual(instance.ledger, {})

class ProcessProtocolTests(unittest.IsolatedAsyncioTestCase):
    async def test_real_process_authentication_refusal_keeps_credentials_private(self):
        import pathlib
        import shutil
        import subprocess
        import sys
        import tempfile
        import websockets
        if not hasattr(websockets, '__path__'):
            # Older bridge suites replace sys.modules['websockets'] globally.
            # Run the socket integration in a clean interpreter in that case.
            result = await asyncio.to_thread(subprocess.run,
                [sys.executable, '-m', 'unittest', 'test_connection_host.ProcessProtocolTests'],
                cwd=str(pathlib.Path(__file__).parent), capture_output=True, text=True, timeout=30)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            return
        received = []
        async def ap_peer(ws, *unused):
            await ws.send(json.dumps([{'cmd':'RoomInfo','seed_name':'host-test-seed'}]))
            try:
                raw = await ws.recv()
                received.extend(json.loads(raw))
                await ws.send(json.dumps([{'cmd':'ConnectionRefused','errors':['host-test-password']}]))
                await ws.wait_closed()
            except websockets.ConnectionClosed:
                pass
        async with websockets.serve(ap_peer, '127.0.0.1', 0) as server:
            ap_port = server.sockets[0].getsockname()[1]
            with tempfile.TemporaryDirectory() as directory:
                client = pathlib.Path(directory) / 'client'
                client.mkdir()
                for source in pathlib.Path(__file__).parent.glob('*.py'):
                    shutil.copy(source, client)
                catalogs = pathlib.Path(directory) / 'worlds' / 'craftopia'
                catalogs.mkdir(parents=True)
                shutil.copy(pathlib.Path(__file__).parent.parent / 'worlds' / 'craftopia' / 'Milestones.py', catalogs)
                process = subprocess.Popen([sys.executable, str(client / 'connection_host.py')], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
                try:
                    process.stdin.write(json.dumps({'server':f'127.0.0.1:{ap_port}', 'slot':'Player', 'password':'host-test-password', 'port':11349}) + '\n')
                    process.stdin.flush()
                    line = await asyncio.wait_for(asyncio.to_thread(process.stdout.readline), 10)
                    prepared = json.loads(line)
                    self.assertEqual(prepared['state'], 'prepared')
                    self.assertEqual(received, [])
                    process.stdin.write('{"cmd":"start"}\n')
                    process.stdin.flush()
                    result = await asyncio.wait_for(asyncio.to_thread(process.stdout.readline), 10)
                    self.assertEqual(json.loads(result)['state'], 'error')
                    rest, errors = await asyncio.wait_for(asyncio.to_thread(process.communicate), 10)
                    self.assertTrue(received, result + errors)
                    self.assertEqual(received[0]['password'], 'host-test-password')
                    self.assertNotIn('host-test-password', line + result + rest + errors)
                    self.assertNotIn('host-test-password', repr(process.args))
                finally:
                    if process.poll() is None:
                        process.kill()
                        await asyncio.to_thread(process.wait)

if __name__ == '__main__': unittest.main()
