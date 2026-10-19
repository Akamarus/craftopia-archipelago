# v1.16.0 — downloadable alpha

Ready-to-use Windows x64 package: compiled BepInEx plugin, isolated Python bridge, matching APWorld, player YAML and installer. BepInEx 5 Mono remains a separate prerequisite. Game assemblies are not included.

AP preferences and persistent progress now live in `%LOCALAPPDATA%\CraftopiaArchipelago`. Existing installations migrate on first launch, preserving the original files. Updates and uninstalls must leave this user-data folder intact.

The current playable endpoints are Geezah and Millewind. Both have completed controlled playtests; later regions and native multiplayer remain unvalidated. Soul Orb map markers and Field Guide pause have automated coverage but still await live visual/input acceptance. This is an experimental alpha, not a stable release.

Workshop is a separate private acceptance candidate. A downloadable GitHub package does not imply that Workshop installation has passed.

Third-party runtime notices are in `mod/bridge/licenses` and `PROVENANCE.json`. CPython and websockets are redistributed with their licenses. Craftopia and Archipelago are owned by their respective creators; this is an unofficial integration.
