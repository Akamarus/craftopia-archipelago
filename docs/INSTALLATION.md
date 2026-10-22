# Installation and setup

## What you need

- Craftopia's **Seamless World** version on Windows, with a separate character/world for AP.
- An internet connection for the one-time loader download. Setup installs the tested BepInEx 5.4.23.5 Windows x64 Mono loader automatically and verifies its SHA-256 digest.
- A matching Craftopia mod + bridge build and `craftopia.apworld`.
- An Archipelago room with a Craftopia slot. The baseline used here is AP **0.6.8**.

**Download the Windows ZIP from [GitHub Releases](https://github.com/Akamarus/craftopia-archipelago/releases).** Extract the complete ZIP. It includes the mod, its Python runtime, `craftopia.apworld` and `Craftopia.yaml`. You do not need a compiler or Python installation. Do not use the repository source ZIP.

## 1. Install the game files

With Craftopia closed, **double-click `Setup.cmd`**, then press Enter to install. Setup finds the game in your Steam libraries; if it cannot choose a single installation, select the folder containing `Craftopia.exe`.

- If the Craftopia AP Workshop item is already downloaded, Setup enables that copy. Steam handles subsequent mod updates. Wait for its official loader dependency to finish downloading too; Setup uses its Workshop discovery component with the tested modern loader.
- Otherwise, Setup installs the mod included in this ZIP. Run Setup from a newer release to update it.
- Existing manual AP installations are updated in place or disabled when switching to Workshop. Old records and other mods are retained.

Launch Craftopia normally after Setup finishes. You do not need to install Python, compile anything, or launch a separate bridge.

**Undo setup:** close Craftopia and run `Restore Previous Setup.cmd`. This restores the files from before the first setup run, including any previous AP version. Saves and `%LOCALAPPDATA%\CraftopiaArchipelago` are left alone. Backups live in the game's `CraftopiaAP-Setup` folder. Restore stops if a managed file has been changed afterward, so it does not silently overwrite a newer modification.

Advanced: `Install-Mod.ps1 -Mode Manual` selects the bundled copy; `-Mode Workshop` selects the subscribed copy. The command wrappers set execution policy only for their own PowerShell process; they do not change the machine's policy. A folder permission error is reported without changing permissions.

For a manual install, first install [BepInEx 5 Windows x64 Mono](https://docs.bepinex.dev/articles/user_guide/installation/index.html), then copy the contents of the release’s `mod` folder into the following folder (or your existing AP plugin folder):

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

A complete bridge package includes Python; players do not need to install Python or launch a bridge terminal separately. The loader is downloaded from its official release during setup. Game DLLs are not distributed.

## 2. Prepare an Archipelago room

If someone else is hosting, give them your Craftopia player YAML and use the server address, exact slot name and optional password they provide.

If generating yourself:

1. Install the matching `craftopia.apworld` in your Archipelago `custom_worlds` folder and restart the launcher.
2. Copy the release's `Craftopia.yaml` into your generation player-files folder.
3. Change `name` to your slot name. Set `ending_region` to `geezah`, `millewind`, or experimental `yarden_mountain` / `yarden_ruin` (see YARDEN_RUNS.md); keep `logic_mode: regional` and the template's other settings for this test profile.
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
