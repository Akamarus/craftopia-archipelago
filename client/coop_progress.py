"""Pure cooperative world-progress rules. No sockets or live bridge integration."""
import copy
import hashlib
import json
from dataclasses import asdict, dataclass
from uuid import UUID


@dataclass(frozen=True)
class GroupIdentity:
    seed: str
    team: int
    world: str
    group: str
    mode: int
    protocol: int
    catalog: str


@dataclass(frozen=True)
class Member:
    character: str
    slot: int


@dataclass(frozen=True)
class Source:
    family: str
    source_id: int
    stage: int = 0


def require(condition, message):
    if not condition:
        raise ValueError(message)


def integer(value, low=0, high=2**63 - 1):
    return type(value) is int and low <= value <= high


def text(value):
    return type(value) is str and 0 < len(value) <= 256 and value.strip() == value


def validate_identity(identity):
    require(type(identity) is GroupIdentity, 'invalid group identity')
    require(all(text(getattr(identity, k)) for k in ('seed', 'world', 'group', 'catalog')), 'invalid identity text')
    require(integer(identity.team) and integer(identity.mode, 0, 1) and
            type(identity.protocol) is int and identity.protocol == 1, 'unsupported identity protocol or mode')
    require(len(identity.world) == 64 and all(c in '0123456789ABCDEF' for c in identity.world), 'invalid world fingerprint')
    try:
        valid = str(UUID(identity.group)) == identity.group
    except ValueError:
        valid = False
    require(valid, 'invalid group UUID')


def validate_member(member):
    require(type(member) is Member and text(member.character) and integer(member.slot, 1), 'invalid member')
    try:
        guid = int(member.character)
    except ValueError:
        raise ValueError('invalid character GUID') from None
    require(str(guid) == member.character and -2**31 <= guid < 2**31 and guid != 0, 'invalid character GUID')


def validate_source(source):
    require(type(source) is Source and source.family in ('orb', 'tower', 'age', 'dungeon'), 'not a team world source')
    require(integer(source.source_id, -2**31, 2**31 - 1), 'invalid source ID')
    require(integer(source.stage, 1, 99) if source.family == 'tower' else
            type(source.stage) is int and source.stage == 0, 'invalid source stage')


def decode(cls, value):
    require(type(value) is dict and set(value) == set(cls.__dataclass_fields__), 'invalid record shape')
    return cls(**value)


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'),
                                     ensure_ascii=True, allow_nan=False).encode()).hexdigest()


def catalog_digest(catalog):
    require(type(catalog) is dict and bool(catalog), 'empty or invalid catalog')
    locations = set()
    rows = []
    for source, location in catalog.items():
        validate_source(source)
        require(integer(location, 1) and location not in locations, 'invalid or duplicate catalog location')
        locations.add(location)
        rows.append([source.family, source.source_id, source.stage, location])
    return digest(sorted(rows))


def new_state(identity):
    validate_identity(identity)
    return dict(format_version=1, identity=asdict(identity), revision=0,
                members=[], completed=[], catalog_digest=None)


def validate_state(state, identity=None, catalog=None):
    require(type(state) is dict and set(state) == {'format_version', 'identity', 'revision',
            'members', 'completed', 'catalog_digest'}, 'invalid journal shape')
    require(type(state['format_version']) is int and state['format_version'] == 1, 'unsupported journal version')
    actual = decode(GroupIdentity, state['identity'])
    validate_identity(actual)
    if identity is not None:
        validate_identity(identity)
        require(actual == identity, 'group identity mismatch')
    require(type(state['members']) is list and type(state['completed']) is list, 'invalid journal records')
    require(integer(state['revision']) and state['revision'] == len(state['members']) + len(state['completed']), 'invalid revision')
    members = [decode(Member, row) for row in state['members']]
    for member in members:
        validate_member(member)
    require(len({m.character for m in members}) == len(members), 'duplicate character')
    slots = {m.slot for m in members}
    require(len(slots) == len(members) if actual.mode == 0 else len(slots) <= 1, 'conflicting slots')
    sources = [decode(Source, row) for row in state['completed']]
    for source in sources:
        validate_source(source)
    require(len(set(sources)) == len(sources), 'duplicate completed source')
    require(not sources or bool(members), 'progress without members')
    signature = state['catalog_digest']
    require(signature is None or (type(signature) is str and len(signature) == 64 and
            all(c in '0123456789abcdef' for c in signature)), 'invalid catalog digest')
    require(not sources or signature is not None, 'progress without catalog')
    if catalog is not None:
        expected = catalog_digest(catalog)
        require(signature is None or signature == expected, 'catalog mismatch')
        require(all(s in catalog for s in sources), 'unknown source')
    return actual


def enroll(state, member):
    identity = validate_state(state)
    validate_member(member)
    if asdict(member) in state['members']:
        return copy.deepcopy(state)
    require(not state['completed'], 'enrollment closed after team progress')
    proposed = copy.deepcopy(state)
    proposed['members'].append(asdict(member))
    proposed['revision'] += 1
    validate_state(proposed, identity)
    return proposed


def record(state, reporter, source, catalog):
    validate_state(state, catalog=catalog)
    validate_member(reporter)
    validate_source(source)
    require(asdict(reporter) in state['members'], 'reporter not enrolled')
    require(source in catalog, 'unknown source')
    proposed = copy.deepcopy(state)
    if asdict(source) not in proposed['completed']:
        proposed['catalog_digest'] = catalog_digest(catalog)
        proposed['completed'].append(asdict(source))
        proposed['revision'] += 1
    return proposed


def project(state, member, catalog):
    validate_state(state, catalog=catalog)
    validate_member(member)
    require(asdict(member) in state['members'], 'member not enrolled')
    return {catalog[decode(Source, row)] for row in state['completed']}
