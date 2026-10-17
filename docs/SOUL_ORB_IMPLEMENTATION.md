# Soul Orb integration — v1.10.0 experimental

## Scope

The working source now connects catalog generation, first-collection replacement, durable bridge checks, typed currency delivery, and character-save verification. It is not yet live-game verified or installed.

Enable `enable_soul_orb_checks: true` only with `logic_mode: expedition` and `mission_catalog_version: 20261007_r1`, in single-player mode. It defaults off. Existing seeds keep their original locations. A fresh approved character and new seed are required for this test; historical orb progress is not imported.

The frozen catalog contains 439 observed candidates (AP IDs 2000–2438). With the 125 mission instances, this produces 564 network locations and items. The pool adds exactly 439 one-unit Soul Orb Currency items; it retains 19 Skill Point items and 106 progression items. Objective victory still counts missions only. Respawning chests remain excluded, and Tamer remains vanilla.

Orb traversal, quest and age requirements are not established by the capture. Orb locations currently use the experimental Overworld access rule. Successful restrictive fill proves only the modeled logic, not real-world beatability. Currency quantity is conserved for Anubis; availability timing and any dependencies on Anubis rewards remain unverified.

## Delivery and replacement

- Both mod capabilities must negotiate protocol 1 before an orb-enabled session can send grants or unlocks. Older clients are held in either connection order.
- Session data uses one main-thread queue. Currency setup finishes as part of the same session application, before fresh-character enrollment.
- Explicit fresh enrollment writes an immutable per-character seed/slot/epoch binding marker before initializing the currency journal. Missing or uncertain initialization requires review, never automatic recovery.
- A marked character cannot collect orbs after restart until its matching orb session is negotiated. Wrong seeds, absent orb contracts and damaged markers do not release that protection. Preserve the `.bound` file with the journals.
- Selected, previously uncollected orbs persist an observation before suppressing the vanilla item-3840 reward and marking the orb taken. Already historical or unselected orbs retain vanilla behavior once the correct session is established. An already-taken selected orb cannot emit another reward.
- Collection waits while currency delivery is ineligible or the mod is disconnected. Previously persisted observations replay after reconnect. The bridge persists its own receipt before acknowledging the observation and retries location reports until the server confirms them.
- Currency receipts are separate from skill-point entitlement and bundles. They bind to seed, slot, epoch, character, AP item index and amount. Seed identity is included in the journal filename hash; slot/epoch/GUID are also checked inside the journal.
- A durable intent precedes mutation. One unit is inserted through the lower-level inventory path, with exact count and remainder verification. Full inventory with no change defers; partial or ambiguous mutation holds without replay.
- After mutation, the save observer captures a fresh PlayerSave/InGame pair for the exact player and path. It verifies GUID and the important-inventory item-3840 total. A read-only SQLite query compares both captured records against committed storage in one database snapshot. Only then can the receipt commit and acknowledgment occur.
- Receipt-write failures retry persistence without remutating. Crashes with an unresolved intent require reviewed reconciliation. The game save and mod journal are separate files and are not one atomic transaction.

A normal save callback is insufficient: installed game save code catches write errors, and SaveFinishEvent runs even after the save error handler. The reader uses ReadOnly + FullMutex, without Create, and never logs save contents.

## Verification

Production currency/replacement/binding tests: 86 assertions. All 14 C# test projects passed before the final session/binding repair; CurrencyTests and GrantPipelineTests were rerun after it. Main build: zero warnings/errors. The SQLite read-back probe uses the game's SQLite library and a disposable fixture, never a real character save.

The 36 real AP 0.6.8/world tests cover legacy compatibility, the expanded pool, restrictive fill, separate mission goals, and generated slot-data routing. The 132 client unittest cases cover identity rejection, persistence failure, reconnect replay, duplicate suppression and explicit saved acknowledgments. These do not substitute for live runtime verification.

## Next live test

1. Preserve current installation, saves, bridge ledger and configuration. Stage a separate experimental seed and explicitly approved fresh character; do not change the existing opening playthrough.
2. Verify startup, exact session/character binding and zero recovery holds before collecting anything.
3. Collect one selected, previously untouched orb. Expect one persisted/reported check and no immediate vanilla currency reward from that orb. A randomized received currency item is a separate event.
4. Deliver one controlled currency item. Confirm inventory rises by one, save verification completes, and bridge receipt is acknowledged. Verify full-inventory behavior separately if feasible.
5. Save and reload. Verify retained currency and no second grant when the same receipt is replayed.
6. Restart without the bridge: the marked character must be unable to consume a fresh orb. Reconnect the same session and verify collection resumes. Do not switch seeds or erase binding files to bypass a hold.
7. Verify Anubis still consumes its normal five-currency cost once enough currency is available.

Stop on unexpected currency changes, save verification failures, repeated grants or recovery holds; preserve all evidence rather than clearing files.


## v1.10.1 live-test follow-up (2026-10-15)

The second live orb check delivered one currency item. The character database contained
that item, but the mod receipt stayed pending. Save-proof parsing accepted the actual
saved rows. A Windows path-comparison defect was reproduced: the observer compared
its canonical path with an unnormalized cache path. Both paths now normalize before
comparison; distinct/missing paths still fail. Observer stage diagnostics were added.
This corrects a demonstrated defect, but fresh in-game confirmation is still required;
there was insufficient logging in v1.10.0 to prove it was the only runtime cause.

One witnessed test receipt was reconciled offline with user authorization, original
journal/database backups, exact identity and database hash checks, and atomic receipt
replacement. The saved inventory was not edited; the bridge must receive its ordinary
identity-bound acknowledgment on reconnect. This is an audited test-session repair,
not an automatic recovery rule. Unknown pending intents still hold after restart.

The HUD now has a small corner expansion toggle. Orb collection shows one server-confirmed
`Check completed: Soul Orb` popup without a raw object ID or duplicate queued popup.
Currency history distinguishes delivery pending from delivered, refreshes on acknowledgment,
and the `Received: Soul Orb x1` popup occurs only on a newly confirmed live save, not replay.
