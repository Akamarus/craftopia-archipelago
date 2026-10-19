import copy
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from dataclasses import replace
from coop_progress import Source
from coop_store import HostJournal
from test_coop_progress import IDENTITY, A, B, CATALOG


class StoreTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.path = Path(self.tmp.name) / 'host.json'

    def test_restart_preserves_members_progress_and_duplicate_result(self):
        with HostJournal(self.path, IDENTITY, CATALOG) as host:
            host.enroll(A)
            host.enroll(B)
            self.assertTrue(host.record(A, Source('orb', -20)))
            expected = host.snapshot()
            escaped = host.snapshot()
            escaped['completed'].clear()
        with HostJournal(self.path, IDENTITY, CATALOG) as host:
            self.assertEqual(host.snapshot(), expected)
            self.assertFalse(host.record(B, Source('orb', -20)))

    def test_corruption_and_catalog_or_identity_changes_preserve_original(self):
        with HostJournal(self.path, IDENTITY, CATALOG) as host: host.enroll(A)
        original = self.path.read_bytes()
        catalog = dict(CATALOG)
        catalog[Source('orb', -20)] = 2999
        for identity, mapping in [(replace(IDENTITY, world='B' * 64), CATALOG), (IDENTITY, catalog)]:
            with self.assertRaises(ValueError), HostJournal(self.path, identity, mapping): pass
            self.assertEqual(self.path.read_bytes(), original)
        for content in ('{', '{}', '{"format_version":1,"format_version":2}'):
            self.path.write_text(content)
            with self.assertRaises(ValueError), HostJournal(self.path, IDENTITY, CATALOG): pass
            self.assertEqual(self.path.read_text(), content)

    def test_failed_replace_or_flush_does_not_acknowledge_or_mutate(self):
        with HostJournal(self.path, IDENTITY, CATALOG) as host:
            host.enroll(A)
            before = host.snapshot()
            disk = self.path.read_bytes()
            for boundary in ('coop_store.os.replace', 'coop_store.os.fsync'):
                with patch(boundary, side_effect=OSError('injected failure')):
                    with self.assertRaises(OSError): host.record(A, Source('orb', -20))
                self.assertEqual(host.snapshot(), before)
                self.assertEqual(self.path.read_bytes(), disk)
            self.assertTrue(host.record(A, Source('orb', -20)))

    def test_second_writer_refused_and_lock_released(self):
        with HostJournal(self.path, IDENTITY, CATALOG):
            with self.assertRaises(OSError), HostJournal(self.path, IDENTITY, CATALOG): pass
        with HostJournal(self.path, IDENTITY, CATALOG) as host: host.enroll(A)

    def test_process_exit_releases_os_lock(self):
        script = '''
import os, sys
from pathlib import Path
from coop_progress import GroupIdentity, Source
from coop_store import HostJournal
identity = GroupIdentity('seed',0,'A'*64,'00000000-0000-0000-0000-000000000001',0,1,'test-v1')
catalog = {Source('orb',-20):2001, Source('tower',1,1):3001, Source('age',3):3002, Source('dungeon',42):3003}
host = HostJournal(Path(sys.argv[1]),identity,catalog)
host.__enter__()
print('locked',flush=True)
sys.stdin.readline()
os._exit(0)
'''
        env = dict(os.environ, PYTHONPATH=str(Path(__file__).parent))
        child = subprocess.Popen([sys.executable, '-c', script, str(self.path)],
                                 stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 text=True, env=env)
        try:
            self.assertEqual(child.stdout.readline().strip(), 'locked')
            with self.assertRaises(OSError), HostJournal(self.path, IDENTITY, CATALOG): pass
            child.communicate('\n', timeout=10)
            self.assertEqual(child.returncode, 0)
            with HostJournal(self.path, IDENTITY, CATALOG) as host: host.enroll(A)
        finally:
            if child.poll() is None: child.kill()
            child.communicate()


if __name__ == '__main__': unittest.main()
