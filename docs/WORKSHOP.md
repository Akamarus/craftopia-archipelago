# Steam Workshop candidate

Workshop distribution is **private and pending acceptance**. Players should use the GitHub release until the README supplies a public Workshop link.

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

Choose a Geezah or Millewind run. Victory requires the ending region's completed tower and main dungeon. Later regions and native multiplayer are still in development.

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

The helper always explicitly sets Private, adds the official loader dependency and saves the assigned ID immediately. Keep that ID file outside the uploaded content and reuse it for updates. A timeout is an unknown outcome: inspect the same item before retrying, never erase the ID file to try again. Legal agreement acceptance is left to the account owner.

## Acceptance status — October 19, 2026

- Private upload and update accepted; downloaded files match the payload manifest.
- Unsubscribe/re-subscribe preserved all 48 existing AP storage files byte-for-byte.
- Manual release installation loaded v1.16.0 and the Field Guide successfully.
- Workshop-only startup remains unverified. A test pointing at the official loader produced no new loader log, and window capture failed. The working manual loader configuration was restored. This does not establish the cause of the startup problem or prove a clean Workshop install works.
- Keep the Workshop listing private until the official loader startup, mod selector, and bundled bridge have passed a real-game test without the manual AP copy.
