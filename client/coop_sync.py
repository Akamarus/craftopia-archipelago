"""Durable slot outbox for authenticated host snapshots (transport supplied later)."""
import copy
from dataclasses import asdict
from coop_progress import (catalog_digest, digest, integer, project, require,
                           validate_identity, validate_member, validate_state)
from coop_store import LockedJSON


class SlotReplica:
    def __init__(self, path, identity, member, catalog):
        validate_identity(identity)
        validate_member(member)
        self.identity, self.member = identity, member
        self._catalog = dict(catalog)
        self._signature = catalog_digest(self._catalog)
        initial = dict(format_version=1, identity=asdict(identity), member=asdict(member),
                       catalog_digest=self._signature, snapshot=None, snapshot_digest=None,
                       pending=[], confirmed=[])
        self._store = LockedJSON(path, initial, self._validate)

    def _locations(self, snapshot):
        validate_state(snapshot, self.identity, self._catalog)
        require(snapshot['catalog_digest'] == self._signature, 'catalog mismatch')
        return project(snapshot, self.member, self._catalog)

    def _validate(self, state):
        require(type(state) is dict and set(state) == {'format_version', 'identity', 'member',
                'catalog_digest', 'snapshot', 'snapshot_digest', 'pending', 'confirmed'}, 'invalid replica shape')
        require(type(state['format_version']) is int and state['format_version'] == 1, 'unsupported replica version')
        # Compare canonical JSON as well as values, so bool cannot impersonate int.
        require(digest(state['identity']) == digest(asdict(self.identity)) and
                digest(state['member']) == digest(asdict(self.member)), 'replica identity mismatch')
        require(state['catalog_digest'] == self._signature, 'replica catalog mismatch')
        for field in ('pending', 'confirmed'):
            values = state[field]
            require(type(values) is list and all(integer(v, 1) for v in values) and
                    len(set(values)) == len(values), 'invalid replica check list')
        pending, confirmed = set(state['pending']), set(state['confirmed'])
        require(not pending & confirmed, 'conflicting check state')
        if state['snapshot'] is None:
            require(state['snapshot_digest'] is None and not pending and not confirmed, 'checks without snapshot')
        else:
            locations = self._locations(state['snapshot'])
            require(state['snapshot_digest'] == digest(state['snapshot']), 'snapshot digest mismatch')
            require(pending | confirmed == locations, 'checks do not match snapshot')

    def __enter__(self):
        self._store.__enter__()
        return self

    def __exit__(self, *args):
        self._store.__exit__(*args)

    def accept(self, snapshot):
        snapshot = copy.deepcopy(snapshot)
        locations = self._locations(snapshot)
        before = self._store.snapshot()
        previous = before['snapshot']
        if previous is not None:
            if snapshot['revision'] < previous['revision']:
                return set()
            if snapshot['revision'] == previous['revision']:
                require(digest(snapshot) == before['snapshot_digest'], 'conflicting snapshot revision')
                return set()
            require(all(m in snapshot['members'] for m in previous['members']), 'membership regression')
            require(all(s in snapshot['completed'] for s in previous['completed']), 'source regression')
            require(not previous['completed'] or snapshot['members'] == previous['members'],
                    'enrollment changed after progress')
        known = set(before['pending']) | set(before['confirmed'])
        added = locations - known
        proposed = copy.deepcopy(before)
        proposed.update(snapshot=snapshot, snapshot_digest=digest(snapshot),
                        pending=sorted(locations - set(before['confirmed'])))
        self._store.commit(proposed)
        return added

    def pending(self):
        return sorted(self._store.snapshot()['pending'])

    def confirm(self, locations):
        require(type(locations) is set and all(integer(v, 1) for v in locations), 'invalid confirmation')
        before = self._store.snapshot()
        pending, confirmed = set(before['pending']), set(before['confirmed'])
        require(locations <= pending | confirmed, 'unknown confirmation')
        if not locations & pending:
            return
        proposed = copy.deepcopy(before)
        proposed.update(pending=sorted(pending - locations), confirmed=sorted(confirmed | locations))
        self._store.commit(proposed)
