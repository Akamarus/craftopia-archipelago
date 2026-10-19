# Cooperative progress foundation

This is a development library, not enabled native multiplayer support. It is not packaged into the player mod and does not change the existing bridge, seed options, or installed game. Native transport, item delivery, enrollment UI, and two-client acceptance are later stages.

## Ownership

| Source | Shared slot | Separate slots |
| --- | --- | --- |
| Soul orb | One team check | One check per enrolled slot |
| Tower repair stage | One team check | One check per enrolled slot |
| Age advancement | One team check | One check per enrolled slot |
| Main dungeon clear | One team check | One check per enrolled slot |
| Personal mission/category | Future shared-slot adapter | Existing personal path; rejected by this team journal |

Each AP location releases its randomized item to the recipient assigned by the seed. Team credit does not mean everyone receives the same item.

## Modules and boundaries

`client/coop_progress.py` owns pure identity, enrollment, source validation, and check projection. `client/coop_store.py` owns atomic persistence and an exclusive OS writer lock. `client/coop_sync.py` owns a slot's durable pending/confirmed check state.

Group identity includes AP seed and team, world fingerprint, a group UUID, mode (0 separate / 1 shared), protocol (1), and catalog identifier. A deterministic catalog mapping digest additionally rejects changed source/location mappings even when a catalog label is reused. Catalogs must contain unique positive location IDs and valid orb/tower/age/dungeon sources.

Membership is permanent. Separate-slot members must have distinct characters and slots; shared-slot members must share a slot. New enrollment closes on first team accomplishment. An already enrolled character may return at any time. Offline members are never removed because their heartbeat stopped.

One host owns the journal. In shared-slot mode one authoritative host adapter must own the AP connection/outbox; this library does not enforce cross-PC authority by itself. Lock files enforce local process ownership only. Call each store serially from its owner; this is not a threaded or distributed database.

## Minimal example

Run with `client` on Python's import path. Example IDs are illustrative, not a production catalog.

```python
from pathlib import Path
from coop_progress import GroupIdentity, Member, Source
from coop_store import HostJournal
from coop_sync import SlotReplica

identity = GroupIdentity('seed-name', 0, 'A' * 64,
    '00000000-0000-0000-0000-000000000001', 0, 1, 'example-v1')
alice, bob = Member('123', 1), Member('-456', 2)
catalog = {Source('orb', -20): 2001}

with HostJournal(Path('host/progress.json'), identity, catalog) as host:
    host.enroll(alice)
    host.enroll(bob)
    host.record(alice, Source('orb', -20))
    snapshot = host.snapshot()

# A future authenticated transport carries the snapshot to Bob's PC.
with SlotReplica(Path('bob/checks.json'), identity, bob, catalog) as peer:
    peer.accept(snapshot)
    assert peer.pending() == [2001]
    # Send pending locations through Bob's AP connection.
    # Only AFTER AP confirms location 2001:
    peer.confirm({2001})
```

`record()` acknowledges a new source only after persistence succeeds. Duplicates return false. `accept()` validates the complete snapshot before mutation, ignores older valid revisions, rejects conflicting equal revisions, and prevents progress/membership removal. It returns newly added locations; callers must send `pending()` for retries, not only that return value. `confirm()` requires a set of known positive location IDs. Filter AP's full checked-location list to this outbox's known locations before calling it. Duplicate confirmations are harmless; unknown confirmations reject the whole operation.

Use context managers to release ownership. Corrupt or incompatible files raise an error and remain untouched; never reset them automatically. A failed atomic write leaves in-memory and on-disk committed state unchanged. Check confirmations are durable, so an interrupted AP send remains pending. Files are flushed before replacement; this is process-crash recovery, not a guarantee against storage-device failure or loss of acknowledged saves from an external backup restore.

## Transport requirements still to implement

Snapshots here are local decoded objects supplied by a trusted caller. The future adapter must verify host/peer identity, membership and current authority, bound message sizes, and authenticate reports against the joined Craftopia session. A host journal cannot establish that an orb was really collected: verified game hooks must supply that evidence. Do not expose the existing loopback bridge to other machines to simulate support.

The old bridge's local-file multiplayer registry remains unchanged. It is not a cross-PC transport and is not used by this library. No runtime migration is attempted.

## Verification

Run `python -m unittest discover -s client -p "test_coop_*.py" -v` from the repository root. Tests cover separate and shared slots, independent confirmations, 750 offline accomplishments surviving host restart, world/catalog isolation, duplicate events, stale/conflicting snapshots, failed writes, and process lock release. Existing client tests should run in separate processes because some install module-level dependency mocks.
