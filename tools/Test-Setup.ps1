$ErrorActionPreference='Stop'
. "$PSScriptRoot/Setup-Core.ps1"
function Assert($condition,$message){if(-not $condition){throw $message}}
$root=Join-Path $PSScriptRoot '../artifacts/setup-tests'
$root=Join-Path $root ([guid]::NewGuid().ToString('N'))
$game=Join-Path $root 'steam/steamapps/common/Craftopia'
$payload=Join-Path $root 'payload'
$loader=Join-Path $root 'loader'
foreach($dir in @($game,"$payload/bridge/python","$loader/BepInEx/core")){New-Item -ItemType Directory -Path $dir -Force|Out-Null}
Set-Content "$game/Craftopia.exe" 'fixture'
Set-Content "$game/winhttp.dll" 'old loader'
Set-Content "$game/doorstop_config.ini" "[General]`ntarget_assembly=old.dll`n[Custom]`nkeep=yes"
Set-Content "$loader/winhttp.dll" 'new loader'
Set-Content "$loader/BepInEx/core/BepInEx.dll" 'new core'
Set-Content "$loader/doorstop_config.ini" "[General]`nenabled=true`ntarget_assembly=BepInEx\core\BepInEx.Preloader.dll"
Set-Content "$payload/CraftopiaArchipelago.dll" 'AP'
Set-Content "$payload/bridge/python/python.exe" 'runtime'
New-Item -ItemType Directory -Path "$game/BepInEx/plugins/ExistingAP" -Force|Out-Null
Set-Content "$game/BepInEx/plugins/ExistingAP/CraftopiaArchipelago.dll" 'old AP'
Set-Content "$game/BepInEx/plugins/ExistingAP/ledger.json" 'preserve'
Set-Content "$game/BepInEx/plugins/Tamer.dll" 'tamer'
Invoke-CraftopiaInstall -Game $game -Loader $loader -Payload $payload -Mode Manual
Assert ((Get-Content "$game/winhttp.dll" -Raw).Trim() -eq 'new loader') 'loader not installed'
Assert ((Get-Content "$game/BepInEx/plugins/ExistingAP/ledger.json" -Raw).Trim() -eq 'preserve') 'legacy record changed'
Assert (@(Get-ChildItem "$game/BepInEx/plugins" -Recurse -Filter CraftopiaArchipelago.dll).Count -eq 1) 'duplicate AP'
Invoke-CraftopiaInstall -Game $game -Loader $loader -Payload $payload -Mode Manual
Restore-CraftopiaInstall -Game $game
Assert ((Get-Content "$game/winhttp.dll" -Raw).Trim() -eq 'old loader') 'repeat install lost original backup'
Assert ((Get-Content "$game/BepInEx/plugins/ExistingAP/CraftopiaArchipelago.dll" -Raw).Trim() -eq 'old AP') 'old AP not restored'
Assert ((Get-Content "$game/BepInEx/plugins/Tamer.dll" -Raw).Trim() -eq 'tamer') 'tamer changed'
Assert (-not (Test-Path "$game/BepInEx/plugins/ExistingAP/bridge/python/python.exe")) 'new runtime not removed'
# Ambiguous duplicate installations must fail before any loader writes.
Set-Content "$game/BepInEx/plugins/CraftopiaArchipelago.dll" 'duplicate'
$rejected=$false
try { Invoke-CraftopiaInstall -Game $game -Loader $loader -Payload $payload -Mode Manual } catch {$rejected=$true}
Assert $rejected 'duplicates accepted'
Assert ((Get-Content "$game/winhttp.dll" -Raw).Trim() -eq 'old loader') 'preflight failure mutated loader'
# Refuse paths that traverse outside the selected game.
$rejected=$false
try { Get-SafeSetupPath $game '../outside' } catch {$rejected=$true}
Assert $rejected 'path traversal accepted'
Write-Host 'Setup lifecycle, repeat install, preservation, duplicate and containment tests passed.'
# A Workshop install switches off the manual DLL without deleting legacy records.
Remove-Item -LiteralPath "$game/BepInEx/plugins/CraftopiaArchipelago.dll"
$workshop=Get-WorkshopRoot $game
New-Item -ItemType Directory -Path "$workshop/3823110766/plugins/CraftopiaArchipelago/bridge/python" -Force|Out-Null
Set-Content "$workshop/3823110766/plugins/CraftopiaArchipelago/bridge/python/python.exe" 'runtime'
$patcher=Join-Path $root 'patcher.dll'
Set-Content $patcher 'test patcher'
$script:PatcherHash=(Get-FileHash $patcher).Hash
Invoke-CraftopiaInstall -Game $game -Loader $loader -Payload $payload -Mode Workshop -Patcher $patcher
Assert (-not(Test-Path "$game/BepInEx/plugins/ExistingAP/CraftopiaArchipelago.dll")) 'manual AP remains active in Workshop mode'
Assert ((Get-Content "$game/BepInEx/plugins/ExistingAP/ledger.json" -Raw).Trim() -eq 'preserve') 'Workshop removed legacy record'
Assert ((Get-Content "$game/doorstop_config.ini" -Raw).Contains('[MultiFolderLoader]')) 'Workshop loader not configured'
Assert ((Get-Content "$game/doorstop_config.ini" -Raw).Contains('keep=yes')) 'custom configuration lost'
# Restore must refuse to overwrite a later user's edit, before restoring any file.
Add-Content "$game/winhttp.dll" 'later update'
$rejected=$false
try{Restore-CraftopiaInstall -Game $game}catch{$rejected=$true}
Assert $rejected 'restore clobbered a later change'
Assert (-not(Test-Path "$game/BepInEx/plugins/ExistingAP/CraftopiaArchipelago.dll")) 'restore partially ran after conflict'
Copy-Item "$loader/winhttp.dll" "$game/winhttp.dll" -Force
Restore-CraftopiaInstall -Game $game
Assert (Test-Path "$game/BepInEx/plugins/ExistingAP/CraftopiaArchipelago.dll") 'restore failed to reactivate previous AP'
Write-Host 'Workshop transition, custom configuration and conflict-safe restore tests passed.'
# Simulate a crash after journaling a pending write, followed by a user edit.
Invoke-CraftopiaInstall -Game $game -Loader $loader -Payload $payload -Mode Manual
$statePath="$game/CraftopiaAP-Setup/state.json"
$state=Get-Content $statePath -Raw|ConvertFrom-Json
$entry=@($state.Files|Where-Object Path -eq 'winhttp.dll')[0]
$entry.AppliedHash=$null
$entry|Add-Member BeforeHash ((Get-FileHash "$game/winhttp.dll").Hash) -Force
$state|ConvertTo-Json -Depth 8|Set-Content $statePath
Add-Content "$game/winhttp.dll" 'user edit after interruption'
$rejected=$false
try{Restore-CraftopiaInstall -Game $game}catch{$rejected=$true}
Assert $rejected 'pending install record overwrote a later user edit'
Copy-Item "$loader/winhttp.dll" "$game/winhttp.dll" -Force
# Simulate restore interrupted after one original file was already restored.
$entry.AppliedHash=(Get-FileHash "$loader/winhttp.dll").Hash
$state|ConvertTo-Json -Depth 8|Set-Content $statePath
Copy-Item (Join-Path $game $entry.Backup) "$game/winhttp.dll" -Force
Restore-CraftopiaInstall -Game $game
Assert ((Get-Content "$game/winhttp.dll" -Raw).Trim() -eq 'old loader') 'partial restore cannot resume'
Write-Host 'Interrupted install and resumable restore tests passed.'
# Keep other disabled mods while correcting a custom loader list path.
$custom=Join-Path $game 'custom-disabled.txt'
Set-Content $custom '123456'
Add-Content "$game/doorstop_config.ini" "`n[MultiFolderLoader]`nbaseDir=$workshop`ndisabledModsListPath=$custom"
Invoke-CraftopiaInstall -Game $game -Loader $loader -Payload $payload -Mode Manual
$ini=Get-Content "$game/doorstop_config.ini" -Raw
Assert ($ini.Contains('disabledModsListPath='+([IO.Path]::GetFullPath((Join-Path $game 'Mods/disabledMods.txt'))))) 'custom exclusion path not corrected'
$list=@(Get-Content "$game/Mods/disabledMods.txt")
Assert ($list -contains '123456' -and $list -contains '3823110766') 'Workshop duplicate prevention lost exclusions'
Restore-CraftopiaInstall -Game $game
Write-Host 'Custom Workshop configuration regression passed.'

