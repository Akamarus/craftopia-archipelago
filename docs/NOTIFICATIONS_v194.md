# v1.9.4 in-game feedback

Transient notices show up to three messages for six seconds, deduplicate repeats for ten seconds, and remove rich-text delimiters/control characters from external text. F8 toggles the status panel. Keyboard polling uses the KeyCode overload in Update rather than OnGUI.

- **Check queued**: the mod durably recorded the observation locally. This does not promise it belongs to the active seed or that the AP server accepted it.
- **Check confirmed**: the bridge observed the location in server-confirmed checked_locations and committed its pending-list removal. The message is epoch/slot/connection bound and rechecked on the Unity thread. Observation custody ACKs are not confirmation notices.
- **Blocked action**: skill assignment, direct crafting, production UI action, equip attempt or saved-loadout attempt explains the missing permission. Browsing queries and machine ticks retain enforcement without transient notices. A disabled native button that never invokes its action may still need a dedicated tooltip in a later UI pass.

Notices are cosmetic and best-effort. Disconnect/crash after confirmation can lose a toast; the durable check itself remains complete. Replayed already-confirmed checks do not produce new confirmation messages. A full mod restart may show a queued notice again for a new observation callback; it does not create a new AP check.

Grant gates log changes per gate instead of every frame. Passive equipment/production messages are bounded and deduplicated. Logging failures cannot override a denial. Existing skill/production/equipment rules and point transactions remain authoritative.

## Live verification (pending)

Keep the same character/world/seed. After save-and-close and backed-up installation:

1. Confirm startup registrations, then load normally. F8 opens/closes the panel once per press.
2. Complete an eligible new mission. Observe queued then confirmed; reconnect must not generate a burst of historical confirmations. Offline check should say queued only until server confirmation.
3. Attempt a locked skill and a locked craft with sufficient materials. Expect a readable notice and no spend. Merely browsing recipes should not flood notices.
4. Confirm allowed crafting still succeeds and existing points/unlocks survive reload. Keep optional Tamer installed.

The new opening profile is tested separately on a future seed; do not change the current seed for this overlay test.
