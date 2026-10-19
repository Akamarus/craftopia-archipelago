# Soul orb map and Field Guide pause — v1.15.0

The full in-game map displays every selected AP soul orb with verified coordinates in Geezah and Millewind immediately, without an exploration prerequisite. Gold means available; grey means collected or already completed on AP. Hover text distinguishes a physical pickup from an AP-completed check (including released checks). Hide collected AP soul orbs is optional and off by default. Coordinates outside these verified catalogs are omitted with a diagnostic, never guessed.

The expanded F8 Field Guide pauses single-player simulation and suppresses player/camera input. Closing it, losing focus, loading another world, or unloading the plugin releases its own pause/input state. The collapsed badge does not pause. In multiplayer only local input is suppressed; the world stays live. Connection settings remain usable at the main menu.

The bridge keeps running while paused. Seed, slot and epoch bind map state, and the existing connection token protects snapshots. Existing durable observations restore pending pickup markers; marker rendering never submits checks or grants items. No seed regeneration or re-enrollment is needed.

## Verification

Release build: zero warnings/errors. OrbMapTests, OrbMapViewTests, FieldGuidePauseTests, GrantPipelineTests, ConnectionTests, EnrollmentRegressionTests and NotificationTests passed. Python: 8 orb snapshot/delivery tests and 38 activity, observation, currency and enrollment tests passed. Full-catalog snapshot fits the protocol limit. Adapter tests cover native-parent attachment, X/Z projection, zoom sizing, reopening, filtering and scene/profile cleanup using fakes; they do not prove visual placement in Unity.

Independent review found a possible native real-time effect overwriting the paused rate. The guide now maintains its owned pause and remembers the latest positive native rate to restore when closed. Tests cover repeated overwrites, pre-existing pause and multiplayer. A third-party mod writing the identical zero rate cannot be distinguished from our ownership; LateUpdate maintenance also cannot guarantee no simulation during an intervening native time write.

## Pending live acceptance

Open the normal full map and check markers in unexplored selected regions, pan/zoom alignment, hover text and the hide-collected toggle. Verify existing completed markers are grey after reconnect. Open/close F8 from gameplay and a native menu: camera must stay still, gameplay resume correctly, and the bridge remain connected. Test Escape and focus loss. These checks do not require further progression in the completed Millewind seed.
