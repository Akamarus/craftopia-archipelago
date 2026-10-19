# Developer guide

Player flow: [installation](INSTALLATION.md). This page is for producing the files that guide requires. Commands below run in PowerShell from the repository root.

## Build from source

Install Git, the .NET 8 SDK, and official Windows x64 CPython. The controlled bridge baseline is Python 3.13.5 with `websockets==13.1`. You also need your own game installation and BepInEx 5 x64 Mono. Game assemblies are compile-only and must not be redistributed.

### 1. Prepare references

Set your actual game path, then copy the references required by the project:

```powershell
$gamePath = 'D:\SteamLibrary\steamapps\common\Craftopia'
New-Item -ItemType Directory -Force mod/refs | Out-Null
$managed = Join-Path $gamePath 'Craftopia_Data/Managed'
$gameAssemblies = @('AD__Overcraft.dll', 'Mirror.dll', 'UnityEngine.dll',
  'UnityEngine.CoreModule.dll', 'UnityEngine.IMGUIModule.dll',
  'UnityEngine.TextRenderingModule.dll', 'UnityEngine.InputLegacyModule.dll',
  'UnityEngine.UI.dll')
foreach ($assembly in $gameAssemblies) {
  Copy-Item -LiteralPath (Join-Path $managed $assembly) -Destination mod/refs
}
foreach ($assembly in @('BepInEx.dll', '0Harmony.dll')) {
  Copy-Item -LiteralPath (Join-Path $gamePath "BepInEx/core/$assembly") -Destination mod/refs
}
dotnet build mod/CraftopiaArchipelago/CraftopiaArchipelago.csproj -c Release
```

Output: `mod/CraftopiaArchipelago/bin/Release/netstandard2.1/CraftopiaArchipelago.dll`.

### 2. Build the bridge and APWorld

Use an official CPython installation; the bridge packager copies its runtime and license files. The Microsoft Store alias or embeddable ZIP is not the tested packaging environment.

```powershell
py -3.13 -m venv .venv
./.venv/Scripts/python.exe -m pip install websockets==13.1
./.venv/Scripts/python.exe tools/package_managed_bridge.py --output artifacts/player-package --verify
./.venv/Scripts/python.exe tools/package_apworld.py artifacts/craftopia.apworld
```

The bridge output is `artifacts/player-package/bridge/`. Install that entire folder beside the compiled DLL as described in [installation](INSTALLATION.md). The packager includes runtime provenance and redistribution licenses. It does not include BepInEx, game files, private ledgers or generated seeds. If rebuilding into the same output directory, use `--overwrite`; it replaces that build output only.

Install `artifacts/craftopia.apworld` into the generator's Archipelago `custom_worlds` folder. Use the same source revision for all components.

## Generate a solo example

For these developer helpers, obtain a separate Archipelago **0.6.8 source checkout** and install its dependencies using that project's instructions. Run with that checkout's prepared Python environment. Set the path to your own checkout:

```powershell
$env:CRAFTOPIA_AP_SOURCE = 'D:\Development\Archipelago-0.6.8'
# Use the Python environment containing AP's dependencies:
python tools/generate_regional_example.py artifacts/geezah-example --ending geezah --seed 2026101605
python tools/generate_regional_example.py artifacts/millewind-example --ending millewind --seed 2026101701
```

These generate a separate example and spoiler; they do not start a server, install the mod, or enroll a character. Host the generated server archive with Archipelago, then follow the in-game setup. Do not use an existing character for a newly generated seed.

## Validation

With the AP-dependent Python environment and `CRAFTOPIA_AP_SOURCE` configured:

```powershell
python -m unittest discover -s worlds/craftopia/test -p test_regional_runs.py
python -m unittest discover -s worlds/craftopia/test -p test_millewind.py
python -m unittest discover -s client -p test_orb_map_snapshot.py
dotnet run --project mod/GrantPipelineTests -c Release
dotnet run --project mod/FieldGuidePauseTests -c Release
dotnet run --project mod/OrbMapViewTests -c Release
```

Other C# regressions are executable `mod/*Tests` projects. `SkillCatalogTests` needs `SKILL_CATALOG_PATH` set to the absolute path of `worlds/craftopia/skill_catalog_20261008_r1.json`. Some historical Python test runners stall; an all-tests pass is not claimed. Use named focused suites and record their results.

## State and compatibility rules

- Preserve item/location IDs and old catalog versions for existing seeds.
- Keep generator reachability consistent with in-game enforcement.
- Never commit game/BepInEx DLLs, saves, credentials, runtime ledgers or generated seed archives.
- Do not reset reward history or bypass a hold to force enrollment or delivery.
- Manual bridge mode is a legacy development path. Normal setup uses F8 Connection and explicit in-game enrollment; old GUID/epoch configuration runbooks are not current player instructions.
- Connection to an AP multiworld does not imply native Craftopia co-op support.

Record live observations separately from automated results. Historical research and runbooks remain in `docs/` for traceability; the root README and linked player guide are the current entry points.

## Build a downloadable release

Build the mod in Release, then run with the approved Windows AP Python environment:

```text
python tools/package_release.py --output artifacts/release-v1.16.0
```

Use a fresh output directory. The command verifies the bundled bridge and produces a player ZIP, matching APWorld, checksums and separate Workshop content. The runtime inputs are explicitly selected; the validator rejects personal state and game assemblies. Review the manifest before uploading. The manifest records the source commit; build from a clean committed tree.

Managed records now live in `%LOCALAPPDATA%\CraftopiaArchipelago`. `UserStorage` migrates prior configuration and bundle state atomically on first launch. The standalone developer bridge keeps its old directory unless `CRAFTOPIA_AP_DATA_DIR` is explicitly set. Never point test processes at live user data.
