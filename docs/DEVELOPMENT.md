# Development and controlled testing

This snapshot is intended for development. Use the current [status](STATUS.md), not the older runbooks, to determine verified behavior.

## Build and tests

Place the references named by the `.csproj` files in `mod/refs/` from your own game/BepInEx installation. Build the main project in Release configuration. Individual C# tests are executable projects, for example:

```sh
dotnet run --project mod/EnrollmentRegressionTests
dotnet run --project mod/ValidationDecisionTests
```

For `SkillCatalogTests`, set `SKILL_CATALOG_PATH` to the absolute path of `worlds/craftopia/skill_catalog_20261008_r1.json`. For real AP generation tests, set `CRAFTOPIA_AP_SOURCE` to an external Archipelago 0.6.8 checkout with its dependencies installed. Older Python tests may require additional fixture or path updates; this repository does not claim a fully portable all-tests command yet.

## Fresh-character enrollment test sequence

1. Back up local saves and AP state. Use a new seed, unique slot/session, and a new disposable character/world.
2. Keep the bridge disconnected and `Enrollment.ApproveFreshCharacter=false`. Load the new character and inspect the mod's read-only character-baseline log. Require matching verified identity, ready player, level 1, known totals, and zero assigned/unspent/mission points. Save and close normally.
3. Start the AP server with `--release_mode disabled --collect_mode disabled --disable_item_cheat`. Start the native bridge with a new `--session`; never reset an existing ledger for this procedure. Confirm the actual seed/slot, empty reward history, and exact integer epoch.
4. While the game is closed, set `General.SlotName` and `LocalPort` to match the bridge, and set `Enrollment.CharacterGuid`, `Epoch`, and `ApproveFreshCharacter=true` to the explicitly reviewed new character/session. Do not round or convert the epoch through floating-point tools.
5. Launch menu-first, verify all nine hooks and session binding, then load only the approved character. Require enrollment success followed by validation Pass and readiness before completing a check. Do not remove holds or consumed approval receipts to force success.
6. Use the generated spoiler to identify one accessible Skill Point reward. Observe mission, grant index, exact balance increase, durable commit, and bridge acknowledgment. Save/reload and reconnect to check persistence.

The same mission does not necessarily contain the same reward in another seed. Tests of spending points must account for missions triggered by learning skills.

This is not an existing-character migration/recovery procedure. Shared-slot enrollment and general user-facing setup remain unfinished.
