# v1.9.4 in-game feedback

## v1.9.8 expandable activity history (installed; live verification pending)

The compact draggable F8 panel now has Show activity / Hide activity. Expanding opens a 300-scaled-pixel scroll view with a scrollbar and mouse-wheel support through Unity IMGUI. Collapse keeps the existing compact status; transient notices remain separate. The expanded view shows received AP items, confirmed sent locations, confirmed check count, and goal completion.

Received means received from the AP server, not necessarily applied to the character: validation/recovery can still hold delivery. Sent lists checked locations, not guessed remote item placements or recipient names. Pending observations remain queued notices until server confirmation. Starters are not AP ReceivedItems and are not included in received history.

The bridge rebuilds history from server checked locations and indexed ReceivedItems replay. Repeated packets replace the same received index rather than adding duplicate rows. Live confirmed checks use observation order; historical checks after a bridge restart use stable location-ID order because AP does not supply their timestamps. Cosmetic history has no independent ledger and never edits delivery records. Memory is capped at 1,000 rows per section; the display packet is bounded to 48 KB, dropping oldest rows if necessary and showing an explicit older-rows-omitted label. Current opening-seed history fits in full.

Snapshots are epoch/slot/connection-token bound and rechecked on the Unity main thread. They replace previous display state; session application clears old history. Presentation send errors do not interrupt delivery or victory. Tests cover actual received-item handling, delayed DataPackage resolution, reconnect snapshots, duplicate replay, pending versus confirmed checks, packet bounds, cosmetic failure isolation, and plain-text formatting. Unity's installed metadata confirms the exact BeginScrollView, EndScrollView and Button overloads. Mouse-wheel and scrollbar interaction require live testing.

October 15 opening-playthrough evidence: the tester completed all eleven opening missions. Server logs confirm all eleven checks and goal completion, with six Skill Point deliveries acknowledged. This validates the opening profile only, not higher-tier or full-game reachability.

## v1.9.7 drag input repair (installed; dragging passed)

The tester reported that v1.9.6 dragging did not respond. Startup logs confirm v1.9.6 loaded; the installed Unity metadata confirms the reflection names exist. The exact runtime GUI event loss was not captured. The old implementation relied on unowned IMGUI events and suppressed input exceptions.

The replacement samples mouse down/held/up and position in Update, converts the bottom-left input coordinates to top-left HUD coordinates, and keeps drag state independent of GUI repaint events. It permits visible free/confined cursors, cancels on lost focus, hidden panel, or locked cursor, and saves only on release. Start/release and bounded input-error diagnostics make the next run observable. Regression tests cover grab offsets, movement, release, idle, outside clicks, cancellation, and viewport bounds. The tester confirmed dragging works in v1.9.7 on October 15. Position restoration after restart remains unverified.

## v1.9.6 positioning refinement (installed; live test pending)

The v1.9.5 live screenshot confirms the selected Wooden Shield explanation renders, and the tester prefers the smaller panel. Its fixed position still overlaps the Workbench heading.

v1.9.6 moves the default farther right and allows left-dragging the status heading while a game menu exposes the cursor. Notices follow the panel. Display/HudX and HudY persist on release in BepInEx configuration, in resolution-scaled coordinates; positions are clamped to the viewport. F8 still hides status without hiding notices. Notification tests cover boundary and invalid-position handling. Release build passes with zero warnings/errors. Drag interaction and restart restoration await live testing.

## v1.9.5 live-test correction

The v1.9.4 F8 toggle passed live testing, but the panel was oversized and the disabled Wooden Shield craft button produced no notice. The panel also included unused connection/goal fields, contradicting the live bridge status.

v1.9.5 uses a compact, resolution-scaled panel beside the minimap. It reports the actual bridge connection and slot or recovery hold; it does not infer AP server connectivity. Notifications size to their wrapped text underneath. F8 hides the status panel independently of notices.

Selected-recipe feedback observes `OcItemUI_CraftMng.RefreshInfo` after the original, reads the inspected `selectingItem` getter, and evaluates the existing equipment policy without crafting. The reason stays visible while selected and clears on allowed selection, unselect, or close. This avoids reliance on a disabled button invoking `CraftButtonExecute`. All observer bodies contain exceptions; original enforcement is unchanged. Hook registration is checked at startup.

Validation: notification regression and equipment suites pass; Release build has zero warnings/errors. Updated layout and recipe notice still require live verification. Check queued/confirmed live verification remains pending.

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

## October 15 live feedback results

Tester confirmed the selected Wooden Shield denial explanation, compact panel, v1.9.7 dragging, and queued then confirmed notifications for a new mission. Logs show HUD drag-start and position-saved events. Position restoration after restart, offline queued-only delivery, and confirmation replay behavior still require live verification; automated results do not replace those checks.
