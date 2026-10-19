"""Shared-slot delivery policy and persistence. No game or network side effects."""
from dataclasses import asdict
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
        for grant in state['grants']:
            require(type(grant) is dict and set(grant) == {'index', 'character', 'state', 'attempt'},
                    'invalid grant record')
            require(integer(grant['index']) and grant['index'] in receipts, 'unknown grant receipt')
            receipt = receipts[grant['index']]
            if receipt.kind == 'points':
                require(grant['character'] in [m.character for m in self._members] and
                        grant['state'] == 'pending', 'invalid character entitlement')
                key = (grant['index'], grant['character'])
            else:
                require(receipt.kind in TEAM_ITEMS and grant['character'] is None and
                        grant['state'] == 'unclaimed', 'invalid team grant')
                key = (grant['index'], None)
            require(grant['attempt'] is None and key not in actual, 'invalid or duplicate grant')
            actual.add(key)
        require(actual == expected, 'incomplete grant history')

    def __enter__(self):
        self._store.__enter__()
        return self

    def __exit__(self, *args):
        self._store.__exit__(*args)

    def _member(self, member):
        validate_member(member)
        require(member in self._members, 'character not enrolled')

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
