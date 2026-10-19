import json
from dataclasses import replace
from pathlib import Path
import tempfile
import unittest
from coop_progress import Member, Source
from coop_store import HostJournal
from coop_sync import SlotReplica
from test_coop_progress import IDENTITY, A, B, CATALOG


class ScenarioTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)

    def test_separate_slots_receive_one_orb_each_with_independent_confirmation(self):
        with HostJournal(self.root / 'host/journal.json', IDENTITY, CATALOG) as host:
            host.enroll(A)
            host.enroll(B)
            host.record(A, Source('orb', -20))
            packet = json.dumps(host.snapshot())
        with SlotReplica(self.root / 'a/checks.json', IDENTITY, A, CATALOG) as peer:
            self.assertEqual(peer.accept(json.loads(packet)), {2001})
            peer.confirm({2001})
        with SlotReplica(self.root / 'b/checks.json', IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.accept(json.loads(packet)), {2001})
        with SlotReplica(self.root / 'a/checks.json', IDENTITY, A, CATALOG) as peer:
            self.assertEqual(peer.pending(), [])
        with SlotReplica(self.root / 'b/checks.json', IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.pending(), [2001])

    def test_offline_member_recovers_750_sources_after_host_restart(self):
        catalog = {Source('orb', n): 10000 + n for n in range(750)}
        path = self.root / 'host/journal.json'
        with HostJournal(path, IDENTITY, catalog) as host:
            host.enroll(A)
            host.enroll(B)
            for source in catalog: host.record(A, source)
        with HostJournal(path, IDENTITY, catalog) as host:
            packet = json.dumps(host.snapshot())
        with SlotReplica(self.root / 'offline/checks.json', IDENTITY, B, catalog) as peer:
            self.assertEqual(peer.accept(json.loads(packet)), set(range(10000, 10750)))
            self.assertEqual(peer.pending(), list(range(10000, 10750)))
            self.assertEqual(peer.accept(json.loads(packet)), set())

    def test_shared_slot_has_single_team_source_and_slot_outbox(self):
        identity = replace(IDENTITY, mode=1)
        guest = Member(B.character, A.slot)
        with HostJournal(self.root / 'host/journal.json', identity, CATALOG) as host:
            host.enroll(A)
            host.enroll(guest)
            self.assertTrue(host.record(guest, Source('orb', -20)))
            self.assertFalse(host.record(A, Source('orb', -20)))
            self.assertEqual(len(host.snapshot()['completed']), 1)
            packet = json.dumps(host.snapshot())
        # Only the host's production adapter will own the shared-slot AP outbox.
        with SlotReplica(self.root / 'host/checks.json', identity, A, CATALOG) as peer:
            self.assertEqual(peer.accept(json.loads(packet)), {2001})
            self.assertEqual(peer.pending(), [2001])

    def test_world_milestones_shared_but_personal_missions_rejected(self):
        with HostJournal(self.root / 'host/journal.json', IDENTITY, CATALOG) as host:
            host.enroll(A)
            host.enroll(B)
            for source in (Source('tower', 1, 1), Source('age', 3), Source('dungeon', 42)):
                host.record(A, source)
            for family in ('mission', 'category'):
                with self.assertRaises(ValueError): host.record(A, Source(family, 1))
            packet = json.dumps(host.snapshot())
        with SlotReplica(self.root / 'b/checks.json', IDENTITY, B, CATALOG) as peer:
            self.assertEqual(peer.accept(json.loads(packet)), {3001, 3002, 3003})


if __name__ == '__main__': unittest.main()
