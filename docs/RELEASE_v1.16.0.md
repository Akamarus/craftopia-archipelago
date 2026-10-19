# v1.16.0 — downloadable alpha

Ready-to-use Windows x64 package: compiled BepInEx plugin, isolated Python bridge, matching APWorld, player YAML and installer. The alpha.2 setup downloads and verifies BepInEx 5.4.23.5 automatically. Game assemblies are not included.

AP preferences and persistent progress now live in `%LOCALAPPDATA%\CraftopiaArchipelago`. Existing installations migrate on first launch, preserving the original files. Updates and uninstalls must leave this user-data folder intact.

The current playable endpoints are Geezah and Millewind. Both have completed controlled playtests; later regions and native multiplayer remain unvalidated. Soul Orb map markers and Field Guide pause have automated coverage but still await live visual/input acceptance. This is an experimental alpha, not a stable release.

Workshop is a separate private acceptance candidate. A downloadable GitHub package does not imply that Workshop installation has passed.

Third-party runtime notices are in `mod/bridge/licenses` and `PROVENANCE.json`. CPython and websockets are redistributed with their licenses. Craftopia and Archipelago are owned by their respective creators; this is an unofficial integration.

## alpha.2 setup update

Double-click Setup.cmd: Steam library detection, verified loader download, existing manual or Workshop installation handling, original-file backups, and a restore command are included. Repeat installs retain the first backup. Restore checks file hashes and refuses to overwrite later changes. No compiler or separate Python installation is needed.
