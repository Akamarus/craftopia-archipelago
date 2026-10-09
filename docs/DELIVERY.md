> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# PoC Delivery & Persistence Strategy (v0.3.1 protocol)

How item grants and location checks survive disconnects, restarts, and new
seeds without duplicating or losing rewards — and where the guarantees end.
This is the PoC-grade version of what the full client will do with a proper
database. **No exactly-once delivery is claimed**; the residual windows are
listed explicitly below.

## Identity model

Three identities must line up before a reward is applied. All three are
validated at runtime; mismatches fail loudly, never silently.

| Identity | Binds | Mechanism | On mismatch |
|---|---|---|---|
| Seed/session | client ledger ↔ mod applied-set | Random 63-bit epoch per fresh ledger; sent in every `grant_items`; mod rotates its applied set on a new epoch; acks echo the epoch and stale-epoch acks are ignored | Mod drops old state and starts clean; client warns instead of auto-resetting |
| Slot | bridge client ↔ mod config | Mod's grant-state file is per slot name (`...-grants-<SlotName>.json`); a mod socket becomes eligible for grants only after a handshake with a matching non-blank slot name | Missing/blank/non-handshake first message: socket rejected and closed. Mismatching non-blank name: bridge aborts — genuine misconfiguration |
| Character/save | mod applied-set ↔ game character | **Procedural in the PoC** (see Limitations): one disposable test character per test session; the applied set is per session-epoch so a new session starts clean | N/A — documented limitation |

## Item delivery (server → game)

Indices are AP `ReceivedItems` positions, stable per seed.

**Client side** (`craftopia_client.py`):
1. On `ReceivedItems`, compute new indices: not in `ledger.delivered`, not in
   in-memory `unacked`.
2. `send_grant` delivers only to a handshake-validated mod socket. Otherwise
   indices stay in `unacked` and are re-sent after the next validated
   handshake (every grant path — new items and reconnect resends — routes
   through this gate). Nothing is marked delivered yet.
3. On `grant_ack` with matching epoch: build the new delivered set, write the
   ledger, and **only then** commit (move indices `unacked` → `delivered`). A
   ledger IO failure leaves indices unacked; the mod dedups the re-send.
4. Validation resets on every disconnect: a reconnecting socket must
   re-handshake before it becomes eligible again. Connection ownership: this
   PoC serves exactly one game — a second mod socket is rejected (closed)
   while the first owns the connection, even while its handshake is pending;
   it can never overwrite the owner's socket or validation state.

**Mod side** (`GrantPipeline`, pure logic + `Plugin` wiring):
- Threading: the network thread only enqueues parsed `grant_items` into a
  thread-safe inbound queue. Unity's main thread owns all pipeline mutation:
  draining, classification, epoch rotation, application, persistence, and ack
  decisions. Each ack carries the epoch in effect for its message.
- Item states: `pending` (queued, not applied) → `dirty` (applied in memory,
  not yet durably persisted) → `applied` (persisted). Only **persisted**
  indices are re-acked; a duplicate of a dirty/pending index is neither
  re-applied nor acknowledged.
- `OnGrantItems` (drain): rotate on new epoch (clears applied/dirty/pending/
  queued); dedup the message including duplicates inside it; partition into
  already-applied → re-ack immediately, dirty/pending → drop (their job's ack
  will cover them), fresh → queue as a job.
- `OnFrame`: per job, when backoff has elapsed —
  a. recheck indices against applied+dirty **at the point of application**;
  b. apply one skill point per fresh index via the game's `AddSkillPoint`;
     on exception: attempts++, backoff (1s…10s cap), keep pending, **never ack**;
  c. persist the applied set (applied ∪ dirty); on IO failure: ERROR log,
     withhold the ack, keep indices dirty (no re-apply), retry the persist.
     The ack goes out only after a successful persist.
- If no player object exists, applies wait; persist retries still run.

## Check reporting (game → server)

- Mission reward claims are detected by a Harmony postfix on
  `Mission.TryGetReward()`. The `Patches` class carries a class-level
  `[HarmonyPatch]` (required: the `PatchAll` assembly scan skips classes
  without one — the v0.3.3 hook never registered). At startup the plugin
  verifies via Harmony's patch metadata that the exact
  `Mission.TryGetReward()` method has our postfix under owner
  `com.craftopia.archipelago.poc`; on failure it logs `[AP] FATAL`, stops
  initialization, and never starts the bridge.

## Reward application (server → game)

- Grants are applied only when the player is ready: `OcPlMng.LocalPl`
  re-evaluated per application (never cached), `isLocalPlayer`,
  `OcPlMaster._isLoaded=true` (save load finished), valid `SkillCtrl`.
  While not ready, grants stay queued — never applied, never persisted as
  applied, never acked.
- Application verifies the skill-point balance before/after. An exact
  increase is confirmed; any other outcome is ambiguous — the job is
  dropped without acking and its indices are quarantined (persisted) for
  manual review, never auto-retried. Server replays of quarantined indices
  are ignored.
- Each distinct mission ID maps to a distinct APQuest location from the pool
  `[1, 2, 3, 4, 5, 6, 10]` (client-side map, persisted in the ledger).
  Re-checking the same mission reuses its location; the AP server dedups
  `LocationChecks` natively.
- Checks fired while the bridge is offline are appended to
  `ledger.pending_checks` (persisted; IO failure is logged as ERROR). On the
  next `Connected`, pending checks whose locations are absent from the
  server's `checked_locations` are sent; already-checked ones are dropped
  with a log line.
- New seeds are **never** auto-detected. A new seed requires an explicit new
  `--session` (separate ledger file + fresh random epoch) or `--reset-ledger`.
  If the ledger has history but the server reports zero checks, the bridge
  prints a loud warning and keeps the ledger.

## Transport (mod ↔ bridge)

The mod's bridge thread keeps one `ReceiveAsync` pending for the life of the
connection and never cancels it — cancelling a `ClientWebSocket` receive
transitions the socket to Aborted and drops the TCP connection with no close
frame (this was the v0.3.2 fix; the 200ms poll only bounds the outbox-flush
wait via `Task.WhenAny`). Reconnects are automatic with a 2s backoff; the
handshake re-validates and unacked grants are re-sent after validation.

Open lifecycle question (v0.3.3 diagnostic build): in-game, the plugin's
`OnDestroy` ran near a scene unload while the game kept running, stopping
the bridge. BepInEx hosts all plugins on a single `DontDestroyOnLoad`
`BepInEx_Manager` root, so a normal scene transition should not destroy it.
v0.3.3 logs timestamped Awake/OnDestroy/OnApplicationQuit with instance IDs,
scene/parent/hideFlags detail, a destroy stack trace, and scene transition
events to identify the destroyer before a survival fix is chosen. Duplicate
plugin instances self-destroy without touching the bridge, and only the
owning instance stops it.

## Crash / failure windows (known, accepted for PoC)

- **W1 — game crash between applying points and persisting the applied set.**
  The in-memory dirty set is lost; on restart the client re-sends (still
  unacked) and the mod applies again → duplicate skill points. Window is a
  few frames wide. Cannot be closed without hooking the game's own character
  save; documented, not guarded.
- **W2 — character save rolled back** (backup restored) after grants were
  applied and acked. The applied set still lists the indices, so the rolled-back
  character never re-receives them. Procedural guard only: never restore a
  character backup mid-session without resetting PoC state on both sides.
- **W3 — bridge crash between receiving an ack and persisting the ledger.**
  Handled: commit happens only after the write succeeds; otherwise indices stay
  unacked and the mod dedups the re-send.
- **W4 — mod grant-state file deleted while the ledger survives** (or vice
  versa). The two sides disagree; the epoch still matches, so the mod treats
  re-sent indices as fresh → duplicates. Procedural guard only: reset both
  sides together.
- **W5 — apply throws forever** (game in a bad state). The job retries with
  backoff indefinitely, logging ERROR each time, blocking nothing else (each
  job is independent). It is never acked and never silently dropped.
- **W6 — check generated while the game is closed.** The mod's outbox is
  in-memory; the check never reaches the bridge. Checks only arise from
  gameplay, so this is minor. (Transport-failure loss is why the PoC does not
  claim lossless check delivery.)

## What is tested vs. assumed

- Tested headless: `GrantPipeline` (13 scenarios, 40+ assertions: dedup at
  receipt and at application, dirty-not-reacked regression, failure retry
  without false ack, persist-failure ack block, epoch rotation, restore,
  no-player wait, concurrent-enqueue thread handoff, epoch change while
  pending) and the client ledger/queue/ack/handshake logic (12 scenarios,
  28 assertions: sessions, offline-check survival, stale-epoch acks,
  commit-after-save, handshake gating incl. blank/mismatch/reconnect).
- Verified against the Archipelago source: JSON-array framing both directions,
  `Connect` fields, version-compatibility rules, APQuest location IDs and the
  `hard_mode` option.
- NOT tested: the actual in-game round trip (no game in this environment),
  real websocket behavior under the game's Mono runtime, thread timing under
  load, or any crash window above. Those are what the phased test plan is for.

