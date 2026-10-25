# Steam Workshop installation

Subscribe to [Craftopia Archipelago](https://steamcommunity.com/sharedfiles/filedetails/?id=3823110766) and its official loader dependency. Download the Windows ZIP from [GitHub releases](https://github.com/Akamarus/craftopia-archipelago/releases), extract it, close Craftopia and run **Setup.cmd once**. No Python or compiler is needed. Steam supplies subsequent mod updates. Subscription alone is not sufficient.

## Maintainer procedure

1. Build the same clean payload as the GitHub alpha with `tools/package_release.py`.
2. Upload the generated `workshop` folder with **Private** visibility. Add the official Craftopia Mod Loader dependency, item `2519948110`. Never upload the working plugin directory, which may contain personal records.
3. Write `modInfo.txt` with the assigned numeric Workshop ID, title, and `mod` on three lines. This is required by Craftopia's mod selection screen. Preserve the assigned ID when updating.
   The plugin must be at `plugins/CraftopiaArchipelago/CraftopiaArchipelago.dll`, with its `bridge` directory beside it. The official MultiFolderLoader only indexes `plugins` and `patchers` inside each Workshop item; a DLL in the item root is not discoverable.
4. Subscribe using the owner account, let Steam download, and confirm the native mod selector and loader discover the plugin and sibling bridge. Avoid running a manual and subscribed copy together.
5. Confirm F8 connection, existing enrollment and delivery recovery. Exit, update the private item, and repeat. Unsubscribe/re-subscribe must preserve `%LOCALAPPDATA%\CraftopiaArchipelago`.
6. Verify on a clean loader setup before changing visibility to Public. The development machine's custom/manual BepInEx install is not proof of subscription-only installation.

Pocketpair's [official uploader](https://github.com/pocketpairjp/Craftopia_OfficialSteamModTools) adds the official loader dependency but its source does not explicitly set visibility. Use an upload path that explicitly selects Private for this test.

## Proposed listing

**Craftopia Archipelago — Regional Alpha**

Connect Craftopia Seamless World to an Archipelago multiworld. Complete missions, collect Soul Orbs, repair towers and clear dungeons to send items across games. Receive equipment permissions, production and skill unlocks, Skill Points and Soul Orb Currency.

Choose an ending region from Geezah through Brigandine Valley. Victory requires that region's completed tower and main dungeon. Regions beyond Brigandine and native multiplayer remain in development.

Press F8 for connection settings, character enrollment, unlocks and item history. The bridge runtime is included; Python and a compiler are not required. Your room host still needs the matching Craftopia APWorld and player YAML from GitHub.

Experimental alpha. Use a dedicated AP character/world and back up saves. Do not install a manual copy alongside the Workshop copy.

Source, downloads and setup: https://github.com/Akamarus/craftopia-archipelago

Developed with AI assistance under the project owner's direction and gameplay testing. Unofficial; not affiliated with Pocketpair or the Archipelago team.

## Private upload helper

`tools/WorkshopUpload` is a maintainer-only console helper. It uses the Steamworks bindings and native library from Pocketpair's official uploader, placed in `artifacts/steam-sdk` as `Assembly-CSharp.dll` and `steam_api64.dll`. These are not included in the player release.

Verified official Version1.02 SHA-256 values:

- `Assembly-CSharp.dll`: `da690578f01efc85e079a770b29c5cc79edbc2d2840d9a6ad1e67f6c31281fdf`
- `steam_api64.dll`: `ca1fa9a7609ab10b7926400559cf073e5888423cc156af72c6027d72a89eea73`

Build with `dotnet build tools/WorkshopUpload -o artifacts/private-uploader`. While signed into Steam, run:

```text
WorkshopUpload.exe upload CONTENT_FOLDER ITEM_ID_FILE PREVIEW_PNG DESCRIPTION_TXT
WorkshopUpload.exe subscribe ITEM_ID
WorkshopUpload.exe inspect ITEM_ID
WorkshopUpload.exe unsubscribe ITEM_ID
```

The upload command always explicitly sets Private, adds the official loader dependency and saves the assigned ID immediately. Keep that ID file outside the uploaded content and reuse it for updates. A timeout is an unknown outcome: inspect the same item before retrying, never erase the ID file to try again. Legal agreement acceptance is left to the account owner.

## Acceptance status — October 19, 2026

- Private upload and update accepted; downloaded files match the payload manifest.
- Unsubscribe/re-subscribe preserved all 48 existing AP storage files byte-for-byte.
- Manual release installation loaded v1.16.0 and the Field Guide successfully.
- The subscribed AP copy loaded twice with BepInEx 5.4.23.5 plus the official MultiFolderLoader, with the manual AP DLL disabled. Tamer loaded alongside it.
- The new Setup.cmd automatically detected the real Steam installation, downloaded the pinned loader, installed Workshop support and passed a live startup. Its restore command restored the previous installation; all 48 AP records remained unchanged.
- The downloaded bridge passed isolation, SSL, handshake, auth-refusal, credential-privacy and shutdown tests in a disposable copy.
- The old official loader bootstrap produced no fresh AP startup evidence. Workshop users must run the modern setup once; subscription alone is not enough.
- At that time, visual Field Guide/real-session connection acceptance remained pending; the October 25 test below resolves that limitation.

## Publication smoke test — October 25, 2026

The v1.18.0 subscribed DLL matched the verified package. Setup installed the modern loader, the Field Guide opened, and its Connect button recovered the existing Brigandine session at 330/330 with Goal complete. Craftopia was closed normally afterward. This verifies startup and existing-session connection, not a new full playthrough or multiplayer. Earlier unsubscribe/re-subscribe and portable-runtime tests remain documented above.

Maintainers can inspect remote metadata with `WorkshopUpload.exe details ITEM_ID`. The separate `publish ITEM_ID` command verifies Craftopia identity and account ownership before changing only visibility to Public. It does not create an item or replace content. Uploads remain private by default.
