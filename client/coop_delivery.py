"""Shared-slot delivery policy and persistence. No game or network side effects."""
from dataclasses import asdict
from uuid import UUID, uuid4
from coop_items import UNLOCKS, TEAM_ITEMS, decode_receipt, validate_receipt
from coop_progress import digest, integer, require, validate_identity, validate_member
from coop_store import LockedJSON


class TeamDelivery:
    """Fixed-roster, serial-owner journal. APIs are for trusted adapters only."""
    def __init__(self, path, identity, members):
        validate_identity(identity)
        require(identity.mode == 1, 'team delivery requires shared slot')
        require(type(members) is tuple and bool(members), 'nonempty fixed roster required')
        for member in members:
            validate_member(member)
        require(len({m.character for m in members}) == len(members) and
                len({m.slot for m in members}) == 1, 'invalid shared-slot roster')
        self._identity = identity
        self._members = tuple(sorted(members, key=lambda m: m.character))
        initial = dict(format_version=1, identity=asdict(identity),
                       members=[asdict(m) for m in self._members], receipts=[], grants=[])
        self._store = LockedJSON(path, initial, self._validate)

    def _validate(self, state):
        require(type(state) is dict and set(state) == {'format_version', 'identity', 'members',
                'receipts', 'grants'}, 'invalid delivery journal shape')
        require(type(state['format_version']) is int and state['format_version'] == 1,
                'unsupported delivery journal version')
        require(digest(state['identity']) == digest(asdict(self._identity)) and
                digest(state['members']) == digest([asdict(m) for m in self._members]),
                'delivery identity or roster mismatch')
        require(type(state['receipts']) is list and type(state['grants']) is list, 'invalid receipt history')
        receipts = {}
        for row in state['receipts']:
            receipt = decode_receipt(row)
            require(receipt.index not in receipts, 'duplicate receipt index')
            receipts[receipt.index] = receipt
        expected = set()
        for receipt in receipts.values():
            if receipt.kind == 'points':
                expected.update((receipt.index, m.character) for m in self._members)
            elif receipt.kind in TEAM_ITEMS:
                expected.add((receipt.index, None))
        actual = set()
        attempts = set()
        characters = {m.character for m in self._members}
        for grant in state['grants']:
            require(type(grant) is dict and set(grant) == {'index', 'character', 'state', 'attempt'},
                    'invalid grant record')
            require(integer(grant['index']) and grant['index'] in receipts, 'unknown grant receipt')
            receipt = receipts[grant['index']]
            if receipt.kind == 'points':
                require(type(grant['character']) is str and grant['character'] in characters and
                        type(grant['state']) is str and grant['state'] in
                        ('pending', 'dispatched', 'held', 'acknowledged'), 'invalid character entitlement')
                key = (grant['index'], grant['character'])
            else:
                require(receipt.kind in TEAM_ITEMS and type(grant['state']) is str and
                        grant['state'] in ('unclaimed', 'reserved', 'dispatched', 'held', 'acknowledged'),
                        'invalid team grant')
                require(grant['character'] is None if grant['state'] == 'unclaimed' else
                        type(grant['character']) is str and grant['character'] in characters,
                        'invalid team grant owner')
                key = (grant['index'], None)
            if grant['state'] in ('dispatched', 'held', 'acknowledged'):
                self._attempt(grant['attempt'])
                require(grant['attempt'] not in attempts, 'duplicate dispatch attempt')
                attempts.add(grant['attempt'])
            else:
                require(grant['attempt'] is None, 'attempt on undispatched grant')
            require(key not in actual, 'duplicate grant')
            actual.add(key)
        require(actual == expected, 'incomplete grant history')

    def __enter__(self):
        self._store.__enter__()
        try:
            state = self._store.snapshot()
            changed = False
            for grant in state['grants']:
                if grant['state'] == 'dispatched':
                    grant['state'] = 'held'
                    changed = True
            if changed:
                self._store.commit(state)
        except BaseException:
            self._store.__exit__()
            raise
        return self

    def __exit__(self, *args):
        self._store.__exit__(*args)

    def _member(self, member):
        validate_member(member)
        require(member in self._members, 'character not enrolled')

    @staticmethod
    def _attempt(attempt):
        require(type(attempt) is str and len(attempt) == 36, 'invalid dispatch attempt')
        try:
            valid = str(UUID(attempt)) == attempt and UUID(attempt).int != 0
        except ValueError:
            valid = False
        require(valid, 'invalid dispatch attempt')

    def _grant(self, state, index, member, team_only=False):
        self._member(member)
        require(integer(index), 'invalid receipt index')
        receipt = next((r for r in state['receipts'] if r['index'] == index), None)
        kinds = TEAM_ITEMS if team_only else TEAM_ITEMS + ('points',)
        require(receipt is not None and receipt['kind'] in kinds, 'not a grantable item receipt')
        return next(g for g in state['grants'] if g['index'] == index and
                    (receipt['kind'] != 'points' or g['character'] == member.character))

    def _envelope(self, state, grant):
        return dict(identity=asdict(self._identity), character=grant['character'],
                    receipt=dict(next(r for r in state['receipts'] if r['index'] == grant['index'])),
                    attempt=grant['attempt'], state=grant['state'])

    def claim(self, index, member):
        state = self._store.snapshot()
        grant = self._grant(state, index, member, team_only=True)
        if grant['state'] != 'unclaimed':
            require(grant['character'] == member.character, 'item already claimed by another character')
            return False
        grant.update(character=member.character, state='reserved')
        self._store.commit(state)
        return True

    def begin(self, index, member):
        state = self._store.snapshot()
        grant = self._grant(state, index, member)
        require(grant['character'] == member.character, 'grant does not belong to character')
        if grant['state'] in ('dispatched', 'held', 'acknowledged'):
            return None
        require(grant['state'] in ('reserved', 'pending'), 'grant is not ready')
        grant.update(state='dispatched', attempt=str(uuid4()))
        self._store.commit(state)
        return self._envelope(state, grant)

    def acknowledge(self, index, member, attempt, saved):
        require(saved is True, 'saved grant evidence required')
        self._attempt(attempt)
        state = self._store.snapshot()
        grant = self._grant(state, index, member)
        require(grant['character'] == member.character and grant['attempt'] == attempt and
                grant['state'] in ('dispatched', 'held', 'acknowledged'), 'acknowledgment does not match grant')
        if grant['state'] == 'acknowledged':
            return
        grant['state'] = 'acknowledged'
        self._store.commit(state)

    def hold_dispatched(self, member):
        self._member(member)
        state = self._store.snapshot()
        changed = False
        for grant in state['grants']:
            if grant['character'] == member.character and grant['state'] == 'dispatched':
                grant['state'] = 'held'
                changed = True
        if changed:
            self._store.commit(state)

    def holds(self):
        state = self._store.snapshot()
        return [self._envelope(state, g) for g in sorted(state['grants'], key=lambda g: (g['index'], g['character'] or ''))
                if g['state'] == 'held']

    def receive(self, receipt):
        validate_receipt(receipt)
        state = self._store.snapshot()
        previous = next((row for row in state['receipts'] if row['index'] == receipt.index), None)
        if previous is not None:
            require(previous == asdict(receipt), 'conflicting receipt content')
            return False
        state['receipts'].append(asdict(receipt))
        if receipt.kind == 'points':
            state['grants'].extend(dict(index=receipt.index, character=m.character, state='pending', attempt=None)
                                   for m in self._members)
        elif receipt.kind in TEAM_ITEMS:
            state['grants'].append(dict(index=receipt.index, character=None, state='unclaimed', attempt=None))
        self._store.commit(state)
        return True

    def unlocks(self, member):
        self._member(member)
        return sorted((decode_receipt(row) for row in self._store.snapshot()['receipts']
                       if row['kind'] in UNLOCKS), key=lambda receipt: receipt.index)

    def available(self):
        state = self._store.snapshot()
        indices = {g['index'] for g in state['grants'] if g['state'] == 'unclaimed'}
        return sorted((decode_receipt(row) for row in state['receipts'] if row['index'] in indices),
                      key=lambda receipt: receipt.index)

    def pending_points(self, member):
        self._member(member)
        state = self._store.snapshot()
        indices = {g['index'] for g in state['grants']
                   if g['character'] == member.character and g['state'] == 'pending'}
        return sorted((decode_receipt(row) for row in state['receipts'] if row['index'] in indices),
                      key=lambda receipt: receipt.index)
