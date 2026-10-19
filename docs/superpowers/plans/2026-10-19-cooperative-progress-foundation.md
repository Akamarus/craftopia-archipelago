# Cooperative Progress Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Build and verify durable team membership, shared accomplishments, and offline slot catch-up for both shared-world modes.

**Architecture:** A transport-independent host journal owns group membership and completed world-source identities. Slot replicas persist pending location checks independently and acknowledge completion only from AP confirmation. No live networking, generation flags, or installed game files change in this stage.

**Tech Stack:** Python standard library, unittest, existing Python bridge/catalog conventions.

**Spec:** `docs/superpowers/specs/2026-10-19-shared-world-multiplayer-design.md` (approved by user on 2026-10-19).

## Global Constraints

- "Tower repairs, age advancements, main dungeon clears, and soul-orb collection are team accomplishments."
- "Enrolled players who were offline receive permanent world credit upon reconnecting."
- "Individual missions remain personal in separate-slot mode."
- "Offline membership is durable and never expires with presence heartbeats."
- "Do not cap correctness state to a recent-event history size."
- "Host writes precede acknowledgment."
- "A catalog mismatch blocks synchronization with a clear explanation."
- Preserve all existing live single-player state, guards, and generation behavior.
- This stage produces a tested library and integration contract, not a multiplayer-ready release.

## Review Focus

1. Two worlds with the same AP seed must not share records (Task 1).
2. Reordered, stale, or conflicting snapshots must not erase accepted progress (Task 3).
3. A write failure must not acknowledge work that exists only in memory (Task 2).
4. Offline players must recover more than 500 accomplishments after host restart (Task 4).
5. An AP send without server confirmation must remain pending on restart (Task 3).

## File boundaries

- Create `client/coop_progress.py`: immutable identity/member/event types, validation, enrollment, source-to-location projection.
- Create `client/coop_store.py`: atomic host journal and peer replica persistence, exclusive writer ownership.
- Create `client/coop_sync.py`: validated snapshot reconciliation and durable per-slot check outbox.
- Create corresponding `client/test_coop_progress.py`, `client/test_coop_store.py`, `client/test_coop_sync.py`, and `client/test_coop_scenarios.py`.
- Create `docs/COOPERATIVE_PROGRESS.md`: public module contract and explicit limitations.
- Do not expand the large bridge module or rewrite its older multiplayer code in this stage.

## Task 1: Identity, membership, and accomplishment rules

**Interfaces:** Frozen dataclasses `GroupIdentity(seed: str, team: int, world: str, group: str, mode: int, protocol: int, catalog: str)`, `Member(character: str, slot: int)`, and `Source(family: str, source_id: int, stage: int = 0)`. `new_state(identity: GroupIdentity) -> dict`; `enroll(state: dict, member: Member) -> dict`; `record(state: dict, reporter: Member, source: Source, catalog: dict[Source, int]) -> dict`. Functions return validated copies and never mutate caller state. `project(state: dict, member: Member, catalog: dict[Source, int]) -> set[int]` returns only team-world checks for that member.

- [ ] Write failing tests for immutable identity, same-seed/different-world rejection, and strict input validation. Reject bool as int, unsupported mode/protocol, zero or noncanonical character GUID, missing identity, and unknown source families. Use modes 0/1 and protocol 1; character GUIDs follow the current signed 32-bit nonzero convention.
- [ ] Run `../../ap-0.6.8/.venv/Scripts/python.exe -m unittest discover -s client -p test_coop_progress.py -v`; expect failure due to missing module.
- [ ] Implement the interfaces in `client/coop_progress.py`. State has format_version=1, identity, revision, members, and completed sources. World fingerprint follows the existing 64-character uppercase hex format. Group is a canonical UUID. Team and slot are nonnegative/positive integers respectively. Identity text is nonempty, trimmed, and capped at 256 characters.
- [ ] Add tests asserting duplicate record leaves revision unchanged; different reporters produce one completed source; all enrolled separate slots project the same team sources; shared-slot membership requires the same slot; separate-slot membership requires distinct slots and characters; repeated enrollment is idempotent; new enrollment after first completion fails; absent reporter fails. All failures leave state unchanged.
- [ ] Implement source rules: only orb/tower/age/dungeon; signed 32-bit source IDs; stage 0 except tower stages 1..99; membership and exact catalog lookup mandatory. Mission/category sources are rejected by this team-only module and stay on the existing personal path. Each successful new mutation increments revision once.
- [ ] Run the same test command; require all passing. Commit only this module and its tests.

## Task 2: Durable host storage and exclusive ownership

**Interfaces:** `HostJournal(path: Path, identity: GroupIdentity, catalog: dict[Source, int])` is a context manager. Methods `enroll(member: Member) -> None`, `record(member: Member, source: Source) -> bool` (true only for a newly committed source), and `snapshot() -> dict`. File format serializes sources as records, never JSON object keys. Acquire an OS-backed exclusive lifetime lock; release automatically on process exit, without timeout-based lock stealing.

- [ ] Write `test_coop_store.py`: reopen roundtrip, duplicate report, corrupted/truncated JSON refusal, identity mismatch, second writer refusal, and simulated write/replace failure. Assert reopening sees only the last committed state and failed writes return no acknowledgment.
- [ ] Run the targeted unittest command with `-p test_coop_store.py`; expect missing-module failures.
- [ ] Implement `client/coop_store.py` using same-directory temporary files, flush/fsync, and atomic replace. Use Windows msvcrt locking with a portable fcntl branch for tests on other systems. Create a new journal only when the target is absent; never replace an invalid existing journal with defaults. Validate full state and catalog before exposing it.
- [ ] Add a subprocess test for process exit releasing ownership and a replacement-failure test that checks in-memory revision is unchanged. Keep error messages free of credentials and actual user file contents.
- [ ] Run all `test_coop_*.py` tests; require passing. Commit storage and tests.

## Task 3: Peer snapshot reconciliation and persistent check outbox

**Interfaces:** `SlotReplica(path: Path, identity: GroupIdentity, member: Member, catalog: dict[Source, int])` uses exclusive storage ownership. `accept(snapshot: dict) -> set[int]` persists newly projected locations and returns that newly added subset; `pending() -> list[int]` returns sorted unconfirmed locations; `confirm(locations: set[int]) -> None` durably acknowledges server-confirmed known locations only. The snapshot input must come from a transport-validated host; this library does not authenticate network messages.

- [ ] Write `test_coop_sync.py`: wrong identity/catalog/member refusal; equal snapshot idempotence; older snapshot ignored without regression; equal-revision conflicting snapshot refusal; newer snapshot cannot remove members or accepted sources; malformed newer snapshot leaves storage unchanged.
- [ ] Run the targeted unittest command; expect missing-module failures.
- [ ] Implement `client/coop_sync.py`, reusing storage primitives. Persist accepted snapshot revision and canonical snapshot digest, enrolled membership, observed sources, pending checks, and confirmed checks atomically. Validate the complete snapshot before any mutation. Same revision must mean identical content. Source mappings must agree with the selected catalog before projection.
- [ ] Add tests proving `pending()` does not acknowledge items, restart after a send retains checks, `confirm()` only removes known pending locations, duplicate confirmations are harmless, and a failed confirmation write retains pending state. Validate incoming collections and integer types before committing.
- [ ] Run all `test_coop_*.py` tests; require passing. Commit sync module and tests.

## Task 4: End-to-end offline scenarios and integration contract

**Interfaces:** Consume the exact Task 1–3 APIs. Test fixtures use separate temporary host/peer directories; data crosses boundaries only as serialized snapshot copies. No peer reads the host directory.

- [ ] Write `test_coop_scenarios.py` using two enrolled members and one host. Record an orb from member A; assert each separate-slot replica queues its own corresponding location, then confirm one replica and restart the other to verify pending replay. For shared slot, assert the journal has one source and one slot-level location; the production adapter must send through only the host bridge.
- [ ] Add a 750-source offline test: enroll both members first, keep B offline, record sources, restart host, then reconnect B and assert all 750 locations survive. Add tower stage, age, dungeon, and mission/category refusal cases.
- [ ] Run the scenario tests before completing any missing behavior, observe meaningful failures, implement only necessary fixes, then rerun all cooperative tests.
- [ ] Run existing regressions separately: `test_multiplayer.py`, `test_character_enrollment.py`, `test_soul_orb_delivery.py`, `test_milestone_delivery.py`, and `test_currency_delivery.py` using unittest discovery with each exact filename. Tests may install module mocks, so use separate Python processes rather than assuming a combined discovery suite is isolated.
- [ ] Write `docs/COOPERATIVE_PROGRESS.md` with a minimal host/replica example, source ownership table, membership freeze rule, catalog/identity contract, acknowledgment boundary, and explicit non-networked status. State that the old local registry remains unchanged and this foundation is not yet shipped or wired into runtime.
- [ ] Review diffs and tests against the approved spec. Commit scenario tests and documentation. Do not publish a release or modify the installed game.

## Later stages and dependencies

The rest of the approved design remains required before multiplayer support is complete:

1. Shared-slot receipts and per-character entitlements: claim reservation, one team grant, reconnect catch-up, and ambiguous-delivery recovery.
2. Native transport investigation/probe: verified host and peer identity, cross-PC communication, disconnect/rejoin; if unsuitable, review an alternative transport.
3. Production adapters and UI: connect the journal to verified detection hooks, AP check queues, goals, setup/enrollment, local input capture, and authenticated peer messages. Add bounded message framing/chunking here; this foundation accepts already-decoded local objects.
4. Cooperative seed compatibility and live two-client testing, then packaging and distribution.

Each later stage receives its own grounded implementation plan; transport APIs are not guessed in this plan. No current shared-mode generation guards are removed prematurely.

## Self-review

This plan covers the approved design's first implementation stage: scoped identity, durable membership, world-source deduplication, persistence, and offline check projection. Receipt delivery, actual peer authentication, native gameplay hooks, setup UI, generation enablement, and live acceptance are explicitly deferred above. The five review risks map to concrete task tests. No runtime integration or multiplayer-readiness claim follows from this stage alone.
