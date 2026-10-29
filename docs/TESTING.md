# Testing and issue reports

Use a separate character/world for each new seed. Keep backups of saves and AP state. Returning to an existing seed should use the same enrolled character; do not enroll again to troubleshoot.

## Useful checks

For generator/package validation, use the [27 October audit and reproduction commands](GENERATION_AUDIT_20261027.md). Always test the actual `.apworld` archive with normal AP discovery and a mixed-game generation; a source-folder test alone cannot establish that the downloadable package works.

- Fresh setup can connect and enroll without editing configuration.
- A mission sends one check; a reconnect does not duplicate its reward.
- Equipment and production remain blocked until their required unlocks arrive.
- Tower stages and main dungeon completion satisfy the chosen region's goal.
- Save/reload preserves received items and check state.

For the current build, also test full-map orb visibility, pan/zoom, hover text, hide-collected, and F8 pause/camera behavior. These remain pending native acceptance. A completed seed can be used for UI checks; it does not need more progression testing.

## Report a problem

Open an [issue](https://github.com/Akamarus/craftopia-archipelago/issues) with:

- Mod version, AP version and game build.
- Ending region and whether this is a new or returning run.
- What you did, what you expected and what happened.
- Relevant item/mission/dungeon names and a screenshot if useful.
- Whether restarting/reconnecting changed the result.

Attach the relevant excerpt of `BepInEx/LogOutput.log` after checking it for server addresses, slot names or other details you do not want public. Keep original saves, bridge ledgers and enrollment records locally; do not post passwords or entire private state directories. Never delete delivery history or bypass a recovery hold to force a test to pass.

Automated coverage and historical results are recorded in [status](STATUS.md). A passing model test does not prove every physical route or combat encounter is practical.
