> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# v1.2.1 repair contract (supersedes earlier implementation claims)

This is a repair of the reviewed v1.2.0 delivery and goal behavior. It is not the completed full gameplay randomizer. No game installation, save, historical grant index or live ledger was modified during development.

## Unlock ownership and delivery

The bridge's version-2 ledger is the durable authority for slot/epoch unlock ownership. `unlocks` retains every accepted receipt and its immutable payload. `pending_unlocks` is only a retry queue. Acknowledging a receipt removes it from pending after a successful candidate-state write; it never removes ownership history. File writes flush/fsync a temporary file and replace the primary. An unsuccessful write cannot advance in-memory state. This is an application persistence guarantee, not a guarantee against every storage device or power-loss failure.

Every newly validated mod connection gets a fresh connection token. Session and unlock messages carry slot, epoch and token. All retained unlocks are made pending and replayed on connection, even previously acknowledged ones. Failed replay setup is retried. The mod checks these identities and its own socket generation, queues worker input, and exposes permissions only after player/load validation and recovery gates pass. It does not require a skill-point grant merely to initialize permission delivery.

The mod is an in-memory mirror, not a competing durable entitlement database. `unlock_ack` means the mirror accepted the bridge's already durable receipt; it does NOT mean the mod wrote a local unlock file. A mod restart restores history from the bridge; a bridge restart restores the complete ledger. Load/identity/connection changes suspend exposed permissions, then eligible reloads reconstruct the same slot entitlements. Skill-point character entitlement accounting and historical recovery remain separate.

Receipts are absolute equipment tier unlocks or idempotent production/tree unlocks. Progressive item generation is not implemented; do not mistake these messages for a completed progressive-item pool. A conflicting receipt holds the mirror. Invalid/blank/coerced messages are not acknowledged. Unknown actual game catalog IDs still require catalog validation before future enforcement hooks use them.

Legacy ledgers cannot prove which memory-only unlocks were already acknowledged. This version holds on legacy/unreadable/incomplete state and preserves it. It does not auto-migrate, reset or reconstruct lost ownership. Use a reviewed recovery procedure or a genuinely new test session; never reset an existing character's history to make the hold disappear. RoomInfo seed changes under an existing ledger also hold rather than silently rebinding.

## Starters

Random category selection uses the slot and durable session identity; its result and receipts commit together. `random_category` is the YAML option name (AP reserves `random` for its own option randomization). `all_basic` uses the existing seven-category list. For `player_choice`, select explicitly through `--starter-choice Sword` (or another allowed category); without a choice the ledger stays unapplied and prints a diagnostic. There is no invented in-game prompt. Choices do not reroll/reapply on reconnect. Starters can be recorded while the mod is offline and delivered after connection.

## Goals and experimental catalog

Supported generated goal: a configurable count of distinct mission-class checks. `objective_locations` maps each network location name to its canonical `location:<AP ID>` objective. Multiple instances of a single class count once, consistently with the current class-based locations. Arbitrary external objective strings cannot count. This is temporary class granularity, not the eventual individual-mission design.

The AP world creates one local `Completed Objective` event per network mission check, inheriting that check's reachability. Completion requires the configured number of those events. The pool now has 53 Skill Points; the old arbitrary locked Victory is removed. Received Victory items cannot substitute for objective evidence. Default goal is objective count 30; unsupported boss/age generation and zero-location configurations are rejected.

Normal, offline and DataPackage-deferred checks use the same atomic goal/check bookkeeping. Deferred disk records regain their objective credit after restart. Goal state is reevaluated and re-announced after AP reconnect. Pending checks remain until server confirmation. Explicit future boss/age events use strict IDs/integers and identity-bound acknowledgments after successful persistence, but no boss/age producer or generated goal is claimed.

The 53 class candidates still have a flat logical region and unverified runtime availability. Passing generation does not prove those classes all exist or are completable in the installed game. Real resource, age, equipment and production dependencies must be added with verified catalogs before claiming logic-safe full gameplay seeds.

## Remaining work / limitations

- Equipment, production and skill-tree enforcement hooks, progressive unlock pool and material/money filler are not implemented by this repair.
- Replacement rewards and non-mission check categories are not implemented. Existing category options are placeholders, not working systems.
- Shared/separate-slot multiplayer fanout, durable enrollment and an in-game UI remain unimplemented.
- Unity Mono runtime hook behavior, actual in-game reload/reconnect, and live websocket behavior require controlled testing. Headless tests do not prove them.
- Mission observations are not a durable game-side outbox. A game exit before the bridge has stored a check, or a check made before any session binding, can lose that observation. This patch does not promise crash-proof game-to-bridge event delivery or historical rescan. Session-bound checks prevent queued events being attributed to a different seed.
- Legacy goal schema/ledger migration and recovery of previously lost skill points/unlocks are separate reviewed procedures. Indices 0–3 are untouched.

## Reproduce tests

Copy the existing game/BepInEx references into `mod/refs` locally; they are intentionally excluded from the handoff.

- `dotnet run --project mod/RepairRegressionTests -c Release`
- `dotnet run --project mod/GrantPipelineTests -c Release`
- `dotnet run --project mod/ValidationDecisionTests -c Release`
- `dotnet run --project mod/TransactionRegressionTests -c Release`
- `dotnet run --project mod/UnlockMirrorTests -c Release`
- `python client/test_delivery_repair.py`
- `python client/test_goal_events.py`
- `python client/test_bridge.py`
- `python worlds/craftopia/test/test_generation.py`
- `python worlds/craftopia/test/test_goal_contract.py`

For actual AP generation/integration, set `CRAFTOPIA_AP_SOURCE` to an AP 0.6.8 checkout and use Python with its dependencies, then run `worlds/craftopia/test/test_real_generation.py` and `client/test_integration.py`. Their loader imports this world directly; unlike the earlier scripts, it never deletes/copies world directories inside the AP installation. The local environment used a small immediate-coroutine runner for the legacy bridge tests; that runner is included with verification evidence and is not a network test.

