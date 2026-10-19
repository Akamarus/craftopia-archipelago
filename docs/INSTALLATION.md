# Installation and setup

## What you need

- Craftopia's **Seamless World** version on Windows, with a separate character/world for AP.
- **BepInEx 5, Windows x64 Mono**, installed in the Craftopia folder. Follow the [official BepInEx installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html). Launch the game once to create the BepInEx folders, then close it.
- A matching Craftopia mod + bridge build and `craftopia.apworld`.
- An Archipelago room with a Craftopia slot. The baseline used here is AP **0.6.8**.

**Download the Windows ZIP from [GitHub Releases](https://github.com/Akamarus/craftopia-archipelago/releases).** Extract the complete ZIP. It includes the mod, its Python runtime, `craftopia.apworld` and `Craftopia.yaml`. You do not need a compiler or Python installation. Do not use the repository source ZIP.

## 1. Install the game files

With Craftopia closed, right-click `Install-Mod.ps1` in the extracted release and choose **Run with PowerShell**. Enter the game folder shown by Steam → Craftopia → Manage → Browse local files. The installer updates an existing AP folder if present and preserves old state for migration.

If Windows blocks the script, use the manual method: copy the contents of the release’s `mod` folder into the following folder (or your existing AP plugin folder):

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

Keep only one active copy of `CraftopiaArchipelago.dll`. For the first upgrade from v1.15 or earlier, update the existing plugin folder and keep all its old files until you have launched the new build successfully.

Starting with v1.16, preferences, enrollment, delivery records and observation queues live in **`%LOCALAPPDATA%\CraftopiaArchipelago`**. First launch copies the old records there without deleting the originals. This folder survives mod updates and uninstalls. Back it up together with your Craftopia saves. Do not delete it to resolve a connection problem.

Migration refuses conflicting records or an incomplete destination rather than starting fresh. Close old bridge processes and preserve both sets of files for diagnosis if startup reports a migration error. Once migration completes, the new folder is authoritative; old copies are backups, not live data. The advanced, manually launched developer bridge retains its legacy location unless `CRAFTOPIA_AP_DATA_DIR` is set to this folder.

A complete bridge package includes Python; players do not need to install Python or launch a bridge terminal separately. BepInEx and game DLLs are not included in the AP package.

## 2. Prepare an Archipelago room

If someone else is hosting, give them your Craftopia player YAML and use the server address, exact slot name and optional password they provide.

If generating yourself:

1. Install the matching `craftopia.apworld` in your Archipelago `custom_worlds` folder and restart the launcher.
2. Copy the release's `Craftopia.yaml` into your generation player-files folder.
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
