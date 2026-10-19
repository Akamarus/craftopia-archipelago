"""Exclusive, atomic cooperative journals. Local paths only; no network transport."""
import copy
import json
import os
from pathlib import Path
import tempfile
from coop_progress import catalog_digest, enroll, new_state, record, require, validate_state


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'duplicate JSON key')
        result[key] = value
    return result


class LockedJSON:
    """One OS-locked writer; publish memory only after durable file replacement."""
    def __init__(self, path, initial, validator):
        self.path = Path(path).resolve()
        self.initial = copy.deepcopy(initial)
        self.validator = validator
        self._lock = None
        self._state = None

    def __enter__(self):
        require(self._lock is None, 'store already open')
        self.path.parent.mkdir(parents=True, exist_ok=True)
        handle = open(str(self.path) + '.lock', 'a+b')
        try:
            if os.name == 'nt':
                import msvcrt
                if os.fstat(handle.fileno()).st_size == 0:
                    handle.write(b'\0')
                    handle.flush()
                handle.seek(0)
                msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
            else:
                import fcntl
                fcntl.flock(handle.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
            self._lock = handle
            if self.path.exists():
                state = json.loads(self.path.read_text(encoding='utf-8'), object_pairs_hook=_unique_object)
                self.validator(state)
                self._state = copy.deepcopy(state)
            else:
                self.commit(self.initial)
            return self
        except BaseException:
            handle.close()
            self._lock = None
            self._state = None
            raise

    def __exit__(self, *args):
        if self._lock is not None:
            self._lock.close()
            self._lock = None
        self._state = None

    def snapshot(self):
        require(self._lock is not None and self._state is not None, 'store not open')
        return copy.deepcopy(self._state)

    def commit(self, state):
        require(self._lock is not None, 'store not open')
        proposed = copy.deepcopy(state)
        self.validator(proposed)
        temporary = None
        try:
            with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', dir=self.path.parent,
                                             prefix=self.path.name + '.', suffix='.tmp', delete=False) as stream:
                temporary = Path(stream.name)
                json.dump(proposed, stream, sort_keys=True, separators=(',', ':'), allow_nan=False)
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(temporary, self.path)
            self._state = proposed
        finally:
            if temporary is not None and temporary.exists():
                temporary.unlink()


class HostJournal:
    def __init__(self, path, identity, catalog):
        self.identity = identity
        self._catalog = dict(catalog)
        initial = new_state(identity)
        initial['catalog_digest'] = catalog_digest(self._catalog)
        self._store = LockedJSON(path, initial, self._validate)

    def _validate(self, state):
        validate_state(state, self.identity, self._catalog)
        require(state['catalog_digest'] == catalog_digest(self._catalog), 'catalog mismatch')

    def __enter__(self):
        self._store.__enter__()
        return self

    def __exit__(self, *args):
        self._store.__exit__(*args)

    def snapshot(self):
        return self._store.snapshot()

    def enroll(self, member):
        before = self.snapshot()
        proposed = enroll(before, member)
        if proposed != before:
            self._store.commit(proposed)

    def record(self, member, source):
        before = self.snapshot()
        proposed = record(before, member, source, self._catalog)
        if proposed == before:
            return False
        self._store.commit(proposed)
        return True
