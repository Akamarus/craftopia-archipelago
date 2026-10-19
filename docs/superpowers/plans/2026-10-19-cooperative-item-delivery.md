# Cooperative Item Delivery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add durable shared-slot receipt ownership and character entitlements, ready for subsequent game/network adapters.

**Architecture:** Reuse GroupIdentity, Member, validated host snapshots, and LockedJSON from the progress foundation. A separate shared-slot delivery journal records immutable AP receipt definitions, claims, dispatch attempts, and acknowledgments. No existing single-player delivery path or installed mod changes in this stage.

**Tech Stack:** Python standard library and unittest; existing cooperative storage primitives.

**Spec:** `docs/superpowers/specs/2026-10-19-shared-world-multiplayer-design.md`.

## Global Constraints

- Physical items and currency are delivered once to the team through an explicit claim by an enrolled character.
- Reserving a claim is persisted before dispatch; it remains assigned through disconnects.
- Skill-point entitlements apply once to each enrolled character, including offline catch-up.
- Unlocks are shared; separate slots retain their current personal delivery behavior.
- Ambiguous grants enter recovery hold; no blind retry or automatic reassignment.
- AP received-item index plus scoped session identity identifies a receipt.
- No live peer transport, game inventory mutation, seed enablement, installer change, or migration of existing receipts in this stage.

## Review Focus

1. Two claimants racing for the same receipt: only one durable owner (Task 2).
2. Same AP index replayed with different content: refuse rather than overwrite (Task 1).
3. Crash between dispatch and acknowledgment: hold without a fresh grant (Task 2).
4. Offline character spends earned points before reconnecting: never infer grants from remaining balance (Task 3).
5. Stale or wrong-character acknowledgment: cannot complete someone else's receipt (Tasks 2–3).

## Files and interfaces

- Create `client/coop_items.py`: frozen `ItemReceipt(index: int, item_id: int, kind: str, target: str, amount: int)` and strict record validation. Kinds are `points`, `physical`, `currency`, `unlock_equipment`, `unlock_production`, `unlock_skill_tree`. These are normalized inputs from a future verified AP classifier, not an arbitrary network item API.
- Create `client/coop_delivery.py`: `TeamDelivery(path: Path, identity: GroupIdentity, members: tuple[Member, ...])` context manager using LockedJSON. Require shared-slot identity and a nonempty, fixed, valid roster sharing one slot. Caller must obtain that roster from validated enrollment; this library does not authenticate peers.
- Create `client/test_coop_items.py`, `client/test_coop_delivery.py`, and `client/test_coop_delivery_scenarios.py`.
- Extend `docs/COOPERATIVE_PROGRESS.md` with delivery APIs and adapter prerequisites.

Receipt validation: strict nonnegative AP index, positive item ID and amount, trimmed nonempty target capped at 256 characters; reject booleans as integers and unknown fields/kinds. Points target is `skill_points`; currency and physical targets are canonical game item identifiers supplied by the classifier. Equipment amount is the resolved absolute tier; production and skill-tree amount is 1. Progressive-equipment classification remains the existing classifier's responsibility; do not count unlock packet arrival order.

Persist format version 1, full group identity, exact roster, receipt definitions, and delivery records. Keys use AP index plus character for points, AP index alone for physical/currency. Unlock receipt definitions are replayable desired state, not inventory mutations. Copy inputs/outputs to prevent external mutation. Reject malformed, incompatible, or corrupt files without rewriting them.

## Task 1: Immutable received-item history and team unlocks

**Interfaces:** `receive(receipt: ItemReceipt) -> bool` returns true only for a newly persisted receipt; identical replay returns false. `unlocks(member: Member) -> list[ItemReceipt]` returns all unlock receipts in AP-index order for a roster member. `available() -> list[ItemReceipt]` lists unclaimed physical/currency receipts. `pending_points(member: Member) -> list[ItemReceipt]` lists that character's points not acknowledged or dispatched.

- [ ] Write tests asserting repeated AP packets create one receipt; conflicting content at the same index rejects without disk changes; unknown member/mode/kind and boolean numeric fields reject. Test receipts arriving out of order and an offline member seeing all shared unlocks. Test returned-list mutation does not alter persisted state.
- [ ] Run `python -m unittest discover -s client -p test_coop_items.py -v` and the corresponding delivery tests; observe missing behavior.
- [ ] Implement receipt validation, TeamDelivery storage, receive, and read APIs. Initialize one independent pending entitlement per enrolled character for every points receipt. Receipt insertion and entitlement creation must be one atomic commit.
- [ ] Run targeted tests until passing, then commit this task.

## Task 2: Team item claim, dispatch, and recovery

**Interfaces:** `claim(index: int, member: Member) -> bool`; `begin(index: int, member: Member) -> dict | None`; `acknowledge(index: int, member: Member, attempt: str, saved: bool) -> None`; `holds() -> list[dict]`.

Physical/currency states: unclaimed → reserved → dispatched → acknowledged. First successful claim returns true; the same owner's repeat returns false; another claimant is refused in every later state. No automatic release/reassignment API. `begin` persists a random UUID attempt before returning a grant envelope containing identity, character, receipt, and attempt. The owner calling begin on an already dispatched/acknowledged record receives None; never generate a second grant attempt. Grant envelopes must be handed only to the verified owner by a future adapter.

Unacknowledged dispatched records reopened from disk become held before any grant is returned. Held receipts accept only a matching saved acknowledgment from the original character and attempt; this allows the future character receipt ledger to resolve a completed grant. No reset/regrant API exists. The future adapter must mark unresolved live dispatches held when a connection is lost using `hold_dispatched(member: Member) -> None`. It must not use timeouts to transfer ownership.

- [ ] Write tests for two sequential competing claims, two competing journal processes, repeat claim, dispatch persistence failure, owner reconnect, and wrong-character/wrong-attempt/unsaved acknowledgment. Confirm only one owner/envelope exists. Use real journals; inject faults only at fsync/replace boundaries.
- [ ] Run tests to observe failures, then implement claim/begin/acknowledge/hold_dispatched/holds with atomic transitions. Acknowledgment is valid only for dispatched or held records with `saved is True` and the exact attempt/member; duplicates of an already acknowledged matching attempt are idempotent.
- [ ] Add crash/reopen tests: reserved stays safely reserved; dispatched becomes held; held does not appear as available and begin returns None; valid saved acknowledgment clears the hold. Unknown receipt and corrupted state cannot become fresh defaults.
- [ ] Run all delivery and progress tests, then commit.

## Task 3: Character entitlement delivery and regression scenarios

**Interfaces:** The same begin/acknowledge/hold_dispatched methods operate on a points receipt's member-specific entitlement. Points require no team claim; each member's pending record can begin independently. Unlock receipts cannot be claimed, begun, or acknowledged through the inventory-grant API.

- [ ] Write tests where A receives and acknowledges points while B is offline; restart and confirm only B is pending. Replaying the receipt or reconnecting A never creates a fresh entitlement. No method accepts current unspent point balance. Test one character's acknowledgment cannot clear the other's pending state.
- [ ] Observe failures, implement point dispatch/acknowledgment using the Task 2 state machine per character, then run the tests. Apply crash holds to points as well as consumables until future per-character save evidence resolves them.
- [ ] Add scenarios combining shared unlocks, claimed currency, points, AP replay, disconnect, and restart across independently stored test instances. Include write failures on receive, claim, begin, acknowledgment, and recovery transition; verify old committed bytes and memory survive failures.
- [ ] Run all `test_coop_*.py` tests and all 26 existing client test files with their documented entry points. Use the local AP source for AP integration tests and separate processes to isolate dependency mocks.
- [ ] Update documentation with the state machine, fixed-roster assumption, replay guarantees, recovery limits, required authenticated adapter/save evidence, and explicit uninstalled status. Record deferred live integration and lack of automatic manual recovery tooling.
- [ ] Obtain independent whole-branch review, address blocking findings, and commit. Keep this stage local until runtime integration is validated.

## Handoff boundaries

The future adapter must validate received AP packets against the DataPackage, resolve progressive equipment correctly, authenticate the current owner/connection, validate group/world/seed identity, and provide persistent character-save evidence. `saved=True` is not proof by itself; these module APIs accept only trusted adapter calls. Currency save receipts already have a separate in-game observer, which must be extended rather than bypassed during integration.

The older bridge's first-connected-player bundle delivery is unchanged until a reviewed runtime adapter switches to this journal. Native transport, connection UI, and two-client tests remain later stages. This plan implements delivery policy and persistence, not an advertised playable shared-slot build.

## Self-review

All five review risks have owning tests. Immutable receipt history prevents conflicting AP replay; atomic reservation and dispatch separate safe retry from uncertain inventory mutation. Fixed roster is intentional for this stage and matches the first release's membership freeze; runtime group finalization must precede activating delivery. Tests must demonstrate persistence rather than merely assert mocked calls.
