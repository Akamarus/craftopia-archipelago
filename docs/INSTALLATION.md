# Installation and setup

## What you need

- Craftopia's **Seamless World** version on Windows, with a separate character/world for AP.
- **BepInEx 5, Windows x64 Mono**, installed in the Craftopia folder. Follow the [official BepInEx installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html). Launch the game once to create the BepInEx folders, then close it.
- A matching Craftopia mod + bridge build and `craftopia.apworld`.
- An Archipelago room with a Craftopia slot. The baseline used here is AP **0.6.8**.

**There is no public ready-to-install release yet.** To obtain the files, follow [Build from source](DEVELOPMENT.md#build-from-source), or use a matching test package supplied by the maintainer. Do not install the repository ZIP as a mod.

## 1. Install the game files

With Craftopia closed, place the mod and its bridge folder together:

```text
Craftopia/
  BepInEx/
    plugins/
      CraftopiaArchipelago/
        CraftopiaArchipelago.dll
        bridge/
          client/
          python/
          worlds/
          licenses/
          PROVENANCE.json
```

Keep only one active copy of `CraftopiaArchipelago.dll`. When updating an existing installation, use its existing plugin folder and preserve configuration and AP state files. Do not move between plugin folders casually: the bundled bridge stores session state alongside its client.

A complete bridge package includes Python; players do not need to install Python or launch a bridge terminal separately. BepInEx and game DLLs are not included in the AP package.

## 2. Prepare an Archipelago room

If someone else is hosting, give them your Craftopia player YAML and use the server address, exact slot name and optional password they provide.

If generating yourself:

1. Install the matching `craftopia.apworld` in your Archipelago `custom_worlds` folder and restart the launcher.
2. Copy the repository's [regional player YAML](../worlds/craftopia/test/regional_player.yaml) into your generation player-files folder.
3. Change `name` to your slot name. Set `ending_region` to `geezah` or `millewind`; keep `logic_mode: regional` and the template's other settings for this test profile.
4. Generate the multiworld and host its resulting server data. For a source-based solo example, see the [developer commands](DEVELOPMENT.md#generate-a-solo-example).

The APWorld is needed by the generator. It is not a file to put in BepInEx. Keep the generator and player components from the same source snapshot.

## 3. Connect and start

1. Launch Craftopia. At the main menu press **F8**, then select **Connection**.
2. Enter your server address and port, exact slot name, and password if required.
3. Select **Connect**. The included bridge starts in the background.
4. Create and load a **fresh character and fresh world** for this seed.
5. Open **F8 → Connection**, select **Start this seed with this character**, and confirm.
6. Close the Field Guide and play. Checks send to the server and received items apply to your enrolled character.

No F9 capture, manual GUID editing or enrollment restart is needed. If setup reports a recovery hold, stop and preserve the files for diagnosis rather than clearing the hold or resetting a ledger.

## Returning to your run

Connect to the same slot at the main menu, then load the same character and world. Enrollment is remembered; you do not need to enroll again. Change seed/slot only at the main menu. A new seed needs its own fresh character/world.

F8 expands or closes the guide. The collapsed badge keeps gameplay running; the expanded guide pauses single-player gameplay in v1.15.0. Native map/pause acceptance is still pending.

## Troubleshooting

| Symptom | First check |
| --- | --- |
| F8 does nothing | Confirm BepInEx loaded and there is exactly one AP DLL. Check `BepInEx/LogOutput.log`. |
| Bridge cannot start | Confirm the complete sibling `bridge` folder is installed, including `python/python.exe`. |
| Connection refused | Confirm server/port, slot spelling and room password with the host. |
| Enrollment unavailable | Connect first, load a fresh character/world and wait for it to finish loading. |
| Action or recipe blocked | Read the AP unlock notice and Field Guide; sufficient materials alone do not grant AP permission. |
| Recovery hold | Keep saves and AP state intact and [report the problem](TESTING.md). |

More details: [connection and password handling](IN_GAME_SETUP.md).
