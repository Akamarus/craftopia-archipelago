$ErrorActionPreference = 'Stop'
$script:LoaderUrl = 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip'
$script:LoaderHash = '82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4'
$script:PatcherHash = 'be1e461d5a7bb98a09c9e49ac1e0064e98f66a2ff32ba304ae85426976765273'

function Get-SafeSetupPath([string]$Root, [string]$Relative) {
    $base = [IO.Path]::GetFullPath($Root).TrimEnd('\','/')
    $path = [IO.Path]::GetFullPath((Join-Path $base $Relative))
    if (-not $path.StartsWith($base + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Setup path escapes the selected game folder.' }
    # Do not follow junctions/symlinks when modifying an installation.
    $probe = $path
    while ($probe.Length -ge $base.Length) {
        if ((Test-Path -LiteralPath $probe) -and ((Get-Item -LiteralPath $probe -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Setup cannot modify a linked folder: $probe" }
        $probe = Split-Path -Parent $probe
        if (-not $probe) { break }
    }
    return $path
}

function Find-Craftopia {
    $roots = @()
    foreach ($key in @('HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        $value = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
        if ($value.SteamPath) { $roots += $value.SteamPath }
        if ($value.InstallPath) { $roots += $value.InstallPath }
    }
    foreach ($root in @($roots)) {
        $vdf = Join-Path $root 'steamapps/libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            foreach ($match in [regex]::Matches([IO.File]::ReadAllText($vdf), '"path"\s+"([^"]+)"')) { $roots += $match.Groups[1].Value.Replace('\\','\') }
        }
    }
    @($roots | Select-Object -Unique | ForEach-Object { Join-Path $_ 'steamapps/common/Craftopia' } | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'Craftopia.exe') })
}

function Get-WorkshopRoot([string]$Game) {
    Join-Path (Split-Path (Split-Path $Game -Parent) -Parent) 'workshop/content/1307550'
}

function Save-SetupState($State, [string]$Path) {
    $temp = $Path + '.tmp'
    [IO.File]::WriteAllText($temp, ($State | ConvertTo-Json -Depth 8))
    if (Test-Path -LiteralPath $Path) { [IO.File]::Replace($temp,$Path,$Path+'.previous') } else { [IO.File]::Move($temp,$Path) }
}

function Get-SetupFileHash([string]$Path) {
    if(Test-Path -LiteralPath $Path){return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}
    return 'absent'
}

function Get-SetupTextHash([string]$Text) {
    $sha=[Security.Cryptography.SHA256]::Create()
    try{return [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text))).Replace('-','')}
    finally{$sha.Dispose()}
}

function Invoke-CraftopiaInstall([string]$Game, [string]$Loader, [string]$Payload, [ValidateSet('Manual','Workshop')][string]$Mode, [string]$Patcher) {
    $Game = [IO.Path]::GetFullPath($Game).TrimEnd('\','/')
    if (-not (Test-Path -LiteralPath (Join-Path $Game 'Craftopia.exe'))) { throw 'Select the folder containing Craftopia.exe.' }
    if (Get-Process Craftopia -ErrorAction SilentlyContinue) { throw 'Save and close Craftopia, then run Setup again.' }
    $plugins = Get-SafeSetupPath $Game 'BepInEx/plugins'
    $existing = @(Get-ChildItem -LiteralPath $plugins -Recurse -Filter CraftopiaArchipelago.dll -File -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 1) { throw 'Multiple manual AP copies found. Resolve the duplicate installations before continuing.' }
    if (-not (Test-Path -LiteralPath "$Loader/BepInEx/core/BepInEx.dll")) { throw 'Loader package is incomplete.' }
    $writes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $Loader -File -Recurse) {
        $rel = $file.FullName.Substring(([IO.Path]::GetFullPath($Loader).TrimEnd('\','/')).Length+1)
        # Loader defaults must not replace user configuration.
        if ($rel -eq 'doorstop_config.ini' -or $rel -like 'BepInEx\config\*') { continue }
        $writes[$rel] = $file.FullName
    }
    if ($Mode -eq 'Manual') {
        if (-not (Test-Path -LiteralPath "$Payload/CraftopiaArchipelago.dll") -or -not (Test-Path -LiteralPath "$Payload/bridge/python/python.exe")) { throw 'Extract the complete AP release ZIP before running Setup.' }
        $dest = if ($existing.Count) { $existing[0].DirectoryName } else { Join-Path $plugins 'CraftopiaArchipelago' }
        $prefix = $dest.Substring($Game.Length+1)
        foreach ($file in Get-ChildItem -LiteralPath $Payload -Recurse -File) {
            $rel = $file.FullName.Substring(([IO.Path]::GetFullPath($Payload).TrimEnd('\','/')).Length+1)
            $writes[(Join-Path $prefix $rel)] = $file.FullName
        }
    } else {
        if (-not (Test-Path -LiteralPath $Patcher) -or (Get-FileHash -LiteralPath $Patcher -Algorithm SHA256).Hash -ne $script:PatcherHash) { throw 'Subscribe to the official Craftopia Mod Loader and let Steam finish downloading it. Its discovery patcher is missing or differs from the tested version.' }
        $workshop = Get-WorkshopRoot $Game
        if (-not (Test-Path -LiteralPath "$workshop/3823110766/plugins/CraftopiaArchipelago/bridge/python/python.exe")) { throw 'Subscribe to Craftopia Archipelago and let Steam finish downloading before choosing Workshop setup.' }
        $writes['BepInEx/patchers/BepInEx.MultiFolderLoader.dll'] = $Patcher
        if ($existing.Count) { $writes[$existing[0].FullName.Substring($Game.Length+1)] = $null }
    }
    $iniPath = Get-SafeSetupPath $Game 'doorstop_config.ini'
    $ini = if (Test-Path -LiteralPath $iniPath) { [IO.File]::ReadAllText($iniPath) } else { [IO.File]::ReadAllText("$Loader/doorstop_config.ini") }
    if ($ini -notmatch '(?m)^\s*\[General\]\s*$') { throw 'Unrecognized Doorstop configuration; no files changed.' }
    # Replace only the General section keys, retaining unrelated sections/options.
    $ini = [regex]::Replace($ini, '(?ms)(^\s*\[General\]\s*\r?\n)(.*?)(?=^\s*\[|\z)', {
        param($m)
        $body = [regex]::Replace($m.Groups[2].Value,'(?m)^\s*(enabled|target_assembly)\s*=.*\r?\n?','')
        $m.Groups[1].Value + "enabled = true`r`ntarget_assembly=BepInEx\core\BepInEx.Preloader.dll`r`n" + $body
    })
    $oldListMatch=[regex]::Match($ini,'(?m)^\s*disabledModsListPath\s*=\s*(.+?)\s*$')
    $extraDisabled=@()
    if($oldListMatch.Success){
        $oldList=$oldListMatch.Groups[1].Value
        if(-not [IO.Path]::IsPathRooted($oldList)){$oldList=Join-Path $Game $oldList}
        if(Test-Path -LiteralPath $oldList){$extraDisabled=@(Get-Content -LiteralPath $oldList)}
    }
    if ($Mode -eq 'Workshop') {
        $ini = [regex]::Replace($ini,'(?ms)^\s*\[MultiFolderLoader\].*?(?=^\s*\[|\z)','')
        $ini += "`r`n[MultiFolderLoader]`r`nbaseDir=$(Get-WorkshopRoot $Game)`r`ndisabledModsListPath=$(Join-Path $Game 'Mods/disabledMods.txt')`r`n"
    } elseif($ini -match '(?m)^\s*\[MultiFolderLoader\]') {
        $newList=Join-Path $Game 'Mods/disabledMods.txt'
        $ini=[regex]::Replace($ini,'(?ms)(^\s*\[MultiFolderLoader\]\s*\r?\n)(.*?)(?=^\s*\[|\z)',{
            param($m)
            $body=[regex]::Replace($m.Groups[2].Value,'(?m)^\s*disabledModsListPath\s*=.*\r?\n?','')
            $m.Groups[1].Value+"disabledModsListPath=$newList`r`n"+$body
        })
    }
    $writes['doorstop_config.ini'] = ':ini'
    $disabledPath=Get-SafeSetupPath $Game 'Mods/disabledMods.txt'
    $disabled=@(if(Test-Path -LiteralPath $disabledPath){Get-Content -LiteralPath $disabledPath})
    $disabled=@(($disabled+$extraDisabled) | Where-Object { $_.Trim() -ne '3823110766' } | Select-Object -Unique)
    if($Mode -eq 'Manual'){$disabled += '3823110766'}
    $writes['Mods/disabledMods.txt']=':disabled'
    foreach ($rel in $writes.Keys) { $null = Get-SafeSetupPath $Game $rel }
    $statePath = Get-SafeSetupPath $Game 'CraftopiaAP-Setup/state.json'
    $state = if (Test-Path -LiteralPath $statePath) { Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json } else { [pscustomobject]@{Version=1;Game=$Game;Files=@();Mode=$Mode} }
    if ($state.Version -ne 1 -or $state.Game -ne $Game) { throw 'Existing setup backup does not match this installation.' }
    New-Item -ItemType Directory -Path (Split-Path $statePath) -Force | Out-Null
    # Save all originals and rollback metadata before the first modification.
    foreach ($rel in $writes.Keys) {
        if (@($state.Files | Where-Object Path -eq $rel).Count) { continue }
        $target = Get-SafeSetupPath $Game $rel
        $present = Test-Path -LiteralPath $target
        $backup = 'CraftopiaAP-Setup/backup/' + [guid]::NewGuid().ToString('N')
        if ($present) {
            $backupPath=Get-SafeSetupPath $Game $backup
            New-Item -ItemType Directory -Path (Split-Path $backupPath) -Force | Out-Null
            Copy-Item -LiteralPath $target -Destination $backupPath
        }
        $state.Files = @($state.Files) + [pscustomobject]@{Path=$rel;Existed=$present;Backup=$backup;OriginalHash=(Get-SetupFileHash $target);AppliedHash=$null;BeforeHash=$null}
    }
    # Journal both the before and intended content, so an interrupted write is
    # recoverable without accepting arbitrary files written after setup.
    foreach($rel in $writes.Keys){
        $entry=@($state.Files | Where-Object Path -eq $rel)[0]
        $entry.BeforeHash=Get-SetupFileHash (Get-SafeSetupPath $Game $rel)
        $entry.AppliedHash=if($null -eq $writes[$rel]){'absent'}
            elseif($writes[$rel] -eq ':ini'){Get-SetupTextHash $ini}
            elseif($writes[$rel] -eq ':disabled'){Get-SetupTextHash $(if($disabled.Count){($disabled -join "`r`n")+"`r`n"}else{''})}
            else{Get-SetupFileHash $writes[$rel]}
    }
    $state.Mode=$Mode
    Save-SetupState $state $statePath
    foreach ($rel in $writes.Keys) {
        $target=Get-SafeSetupPath $Game $rel
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        if ($null -eq $writes[$rel]) { Remove-Item -LiteralPath $target }
        elseif ($writes[$rel] -eq ':ini') { [IO.File]::WriteAllText($target,$ini) }
        elseif ($writes[$rel] -eq ':disabled') { [IO.File]::WriteAllLines($target,[string[]]$disabled) }
        else { Copy-Item -LiteralPath $writes[$rel] -Destination $target -Force }
        $entry=@($state.Files | Where-Object Path -eq $rel)[0]
        if((Get-SetupFileHash $target) -ne $entry.AppliedHash){throw "Installed file verification failed: $rel"}
        $entry.BeforeHash=$null
        Save-SetupState $state $statePath
    }
    Write-Host "Installed ($Mode). Launch Craftopia and press F8 > Connection."
    Write-Host 'Saves and AP records are preserved. Setup backups: CraftopiaAP-Setup.'
}

function Restore-CraftopiaInstall([string]$Game) {
    if (Get-Process Craftopia -ErrorAction SilentlyContinue) { throw 'Save and close Craftopia first.' }
    $statePath=Get-SafeSetupPath $Game 'CraftopiaAP-Setup/state.json'
    $state=Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.Version -ne 1 -or $state.Game -ne [IO.Path]::GetFullPath($Game).TrimEnd('\','/')) { throw 'Setup backup does not match this folder.' }
    foreach ($f in $state.Files) {
        $target=Get-SafeSetupPath $Game $f.Path
        $current=Get-SetupFileHash $target
        if(@($f.AppliedHash,$f.BeforeHash,$f.OriginalHash) -notcontains $current){throw "File changed since setup: $($f.Path). Restore stopped to preserve the newer file."}
        if ($f.Existed -and -not (Test-Path -LiteralPath (Get-SafeSetupPath $Game $f.Backup))) { throw 'Backup incomplete; restore stopped before changing files.' }
        if($f.Existed -and (Get-SetupFileHash (Get-SafeSetupPath $Game $f.Backup)) -ne $f.OriginalHash){throw 'Backup integrity check failed; no files restored.'}
    }
    foreach ($f in $state.Files) {
        $target=Get-SafeSetupPath $Game $f.Path
        if ($f.Existed) { Copy-Item -LiteralPath (Get-SafeSetupPath $Game $f.Backup) -Destination $target -Force }
        elseif (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
    }
    Move-Item -LiteralPath $statePath -Destination ($statePath + '.restored-' + [guid]::NewGuid().ToString('N'))
    Write-Host 'Previous installation restored. Saves and AP records were not removed.'
}
