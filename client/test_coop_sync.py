import copy
from dataclasses import asdict, replace
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from coop_progress import Source, enroll, record
from coop_sync import SlotReplica
from test_coop_progress import A, B, CATALOG, IDENTITY, enrolled


class SyncTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.path = Path(self.tmp.name) / 'peer.json'
        self.first = record(enrolled(), A, Source('orb', -20), CATALOG)
        self.second = record(self.first, B, Source('age', 3), CATALOG)

    def test_send_without_confirmation_retries_after_restart(self):
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.accept(self.first), {2001})
            self.assertEqual(peer.pending(), [2001])
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.accept(self.first), set())
            self.assertEqual(peer.pending(), [2001])
            peer.confirm({2001})
            peer.confirm({2001})
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.pending(), [])
            self.assertEqual(peer.accept(self.second), {3002})
            self.assertEqual(peer.pending(), [3002])

    def test_stale_snapshot_does_not_erase_progress(self):
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            peer.accept(self.second)
            self.assertEqual(peer.accept(self.first), set())
            self.assertEqual(peer.pending(), [2001, 3002])

    def test_conflicting_or_regressive_snapshots_are_atomic(self):
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            peer.accept(self.first)
            conflicting = record(enrolled(), A, Source('dungeon', 42), CATALOG)
            removed_source = record(conflicting, A, Source('age', 3), CATALOG)
            removed_member = copy.deepcopy(self.second)
            removed_member['members'][0] = asdict(type(A)('789', 1))
            malformed = copy.deepcopy(self.second)
            malformed['revision'] = True
            for snapshot in (conflicting, removed_source, removed_member, malformed):
                before = self.path.read_bytes()
                with self.assertRaises(ValueError): peer.accept(snapshot)
                self.assertEqual(self.path.read_bytes(), before)
                self.assertEqual(peer.pending(), [2001])

    def test_wrong_group_catalog_or_member_is_refused(self):
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            for key, value in [('world', 'B' * 64), ('seed', 'another'), ('group', '00000000-0000-0000-0000-000000000002'), ('mode', 1)]:
                snapshot = copy.deepcopy(self.first)
                snapshot['identity'][key] = value
                with self.assertRaises(ValueError): peer.accept(snapshot)
            snapshot = copy.deepcopy(self.first)
            snapshot['catalog_digest'] = '0' * 64
            with self.assertRaises(ValueError): peer.accept(snapshot)
            snapshot = record(enroll(__import__('coop_progress').new_state(IDENTITY), A), A, Source('orb', -20), CATALOG)
            with self.assertRaises(ValueError): peer.accept(snapshot)
            self.assertEqual(peer.pending(), [])

    def test_invalid_confirmations_and_write_failure_preserve_pending(self):
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            peer.accept(self.first)
            for invalid in ({2001, 9999}, {True}, [2001], {'2001'}):
                with self.assertRaises(ValueError): peer.confirm(invalid)
                self.assertEqual(peer.pending(), [2001])
            with patch('coop_store.os.replace', side_effect=OSError('failure')):
                with self.assertRaises(OSError): peer.confirm({2001})
                with self.assertRaises(OSError): peer.accept(self.second)
            self.assertEqual(peer.pending(), [2001])
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.pending(), [2001])

    def test_replica_identity_and_catalog_cannot_change_on_restart(self):
        with SlotReplica(self.path, IDENTITY, B, CATALOG) as peer: peer.accept(self.first)
        modified = dict(CATALOG)
        modified[Source('orb', -20)] = 2999
        for identity, member, catalog in [(replace(IDENTITY, seed='other'), B, CATALOG),
                                          (IDENTITY, A, CATALOG), (IDENTITY, B, modified)]:
            with self.assertRaises(ValueError), SlotReplica(self.path, identity, member, catalog): pass


if __name__ == '__main__': unittest.main()
