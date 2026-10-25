import copy
import unittest
from dataclasses import replace
from coop_progress import GroupIdentity, Member, Source, new_state, enroll, record, project, validate_state


IDENTITY = GroupIdentity('seed', 0, 'A' * 64, '00000000-0000-0000-0000-000000000001', 0, 1, 'test-v1')
A, B = Member('123', 1), Member('-456', 2)
CATALOG = {Source('orb', -20): 2001, Source('tower', 1, 1): 3001,
           Source('age', 3): 3002, Source('dungeon', 42): 3003}


def enrolled(identity=IDENTITY):
    state = enroll(new_state(identity), A)
    return enroll(state, B if identity.mode == 0 else Member('-456', 1))


class ProgressTests(unittest.TestCase):
    def test_team_credit_and_duplicate_do_not_mutate_input(self):
        original = enrolled()
        before = copy.deepcopy(original)
        state = record(original, A, Source('orb', -20), CATALOG)
        self.assertEqual(original, before)
        self.assertEqual(project(state, A, CATALOG), {2001})
        self.assertEqual(project(state, B, CATALOG), {2001})
        self.assertEqual(record(state, B, Source('orb', -20), CATALOG), state)
        self.assertEqual(state['revision'], original['revision'] + 1)

    def test_each_milestone_stage_is_distinct(self):
        catalog = dict(CATALOG, **{})
        catalog[Source('tower', 1, 2)] = 3004
        state = enrolled()
        for source in catalog:
            state = record(state, A, source, catalog)
        self.assertEqual(project(state, B, catalog), {2001, 3001, 3002, 3003, 3004})

    def test_membership_rules_and_freeze(self):
        state = enrolled()
        self.assertEqual(enroll(state, A), state)
        for member in (Member('789', 1), Member('123', 3)):
            with self.assertRaises(ValueError): enroll(state, member)
        progressed = record(state, A, Source('orb', -20), CATALOG)
        self.assertEqual(enroll(progressed, A), progressed)
        with self.assertRaises(ValueError): enroll(progressed, Member('789', 3))
        for fn in (lambda: record(state, Member('789', 3), Source('orb', -20), CATALOG),
                   lambda: project(state, Member('789', 3), CATALOG)):
            with self.assertRaises(ValueError): fn()

    def test_shared_slot_has_one_slot(self):
        state = enrolled(replace(IDENTITY, mode=1))
        with self.assertRaises(ValueError): enroll(state, Member('789', 2))
        state = record(state, A, Source('orb', -20), CATALOG)
        self.assertEqual(project(state, Member('-456', 1), CATALOG), {2001})

    def test_invalid_values_are_rejected(self):
        for field, value in [('mode', True), ('mode', 2), ('protocol', 2), ('team', True),
                             ('world', 'B'), ('group', 'bad'), ('seed', ''), ('catalog', ' x')]:
            with self.subTest(field=field), self.assertRaises(ValueError):
                new_state(replace(IDENTITY, **{field: value}))
        for member in (Member('0', 1), Member('0123', 1), Member('123', True), Member('2147483648', 1)):
            with self.assertRaises(ValueError): enroll(new_state(IDENTITY), member)
        for source in (Source('mission', 1), Source('category', 1), Source('orb', True),
                       Source('tower', 1, 0), Source('age', 1, 1), Source('orb', 99)):
            with self.assertRaises(ValueError): record(enrolled(), A, source, CATALOG)

    def test_state_validation_rejects_wrong_world_and_invalid_history(self):
        state = record(enrolled(), A, Source('orb', -20), CATALOG)
        with self.assertRaises(ValueError): validate_state(state, replace(IDENTITY, world='B' * 64), CATALOG)
        for field, value in [('revision', True), ('completed', state['completed'] * 2),
                             ('members', []), ('format_version', 2)]:
            damaged = copy.deepcopy(state)
            damaged[field] = value
            with self.assertRaises(ValueError): validate_state(damaged, IDENTITY, CATALOG)


if __name__ == '__main__': unittest.main()
