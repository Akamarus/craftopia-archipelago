param([string]$GamePath)
$ErrorActionPreference = 'Stop'
if (-not $GamePath) { $GamePath = Read-Host 'Craftopia folder (Steam > Manage > Browse local files)' }
$game = (Resolve-Path -LiteralPath $GamePath).Path
if (-not (Test-Path -LiteralPath (Join-Path $game 'Craftopia.exe'))) { throw 'This is not the Craftopia game folder.' }
if (Get-Process -Name Craftopia -ErrorAction SilentlyContinue) { throw 'Save and close Craftopia before installing.' }
$plugins = Join-Path $game 'BepInEx/plugins'
if (-not (Test-Path -LiteralPath (Join-Path $game 'BepInEx/core/BepInEx.dll'))) { throw 'Install BepInEx 5 Windows x64 Mono and run the game once first.' }
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
$existing = @(Get-ChildItem -LiteralPath $plugins -Filter 'CraftopiaArchipelago.dll' -Recurse -File)
if ($existing.Count -gt 1) { throw 'Multiple AP copies found. Keep only the intended installation before updating.' }
$destination = if ($existing.Count -eq 1) { $existing[0].DirectoryName } else { Join-Path $plugins 'CraftopiaArchipelago' }
$source = Join-Path $PSScriptRoot 'mod'
if (-not (Test-Path -LiteralPath (Join-Path $source 'bridge/python/python.exe'))) { throw 'Extract the complete release ZIP before installing.' }
# Preserve old client state for first-launch migration. Do not delete or mirror folders.
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force
Write-Host 'Installed. Launch Craftopia, then press F8 > Connection.'
Write-Host 'AP progress and preferences migrate to %LOCALAPPDATA%\CraftopiaArchipelago on first launch.'
