param(
    [string]$GamePath,
    [ValidateSet('Auto','Manual','Workshop')][string]$Mode='Auto',
    [ValidateSet('Install','Restore')][string]$Action='Install',
    [switch]$NonInteractive
)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/Setup-Core.ps1"
try {
    Write-Host "`nCraftopia Archipelago Setup`n"
    if (-not $GamePath) {
        $found=@(Find-Craftopia)
        if($found.Count -eq 1){$GamePath=$found[0]}
        elseif($NonInteractive){throw 'Could not uniquely detect Craftopia. Supply -GamePath.'}
        else {
            Add-Type -AssemblyName System.Windows.Forms
            $picker=New-Object System.Windows.Forms.FolderBrowserDialog
            $picker.Description='Select the Steam folder containing Craftopia.exe'
            if($picker.ShowDialog() -ne 'OK'){throw 'Setup cancelled.'}
            $GamePath=$picker.SelectedPath
        }
    }
    $GamePath=(Resolve-Path -LiteralPath $GamePath).Path
    if(-not(Test-Path -LiteralPath "$GamePath/Craftopia.exe")){throw 'This folder does not contain Craftopia.exe.'}
    if(Get-Process Craftopia -ErrorAction SilentlyContinue){throw 'Save and close Craftopia, then run Setup again.'}
    Write-Host "Game: $GamePath"
    if($Action -eq 'Restore'){
        if(-not $NonInteractive -and (Read-Host 'Restore the installation from before Setup? Type YES') -ne 'YES'){throw 'Restore cancelled.'}
        Restore-CraftopiaInstall -Game $GamePath
        exit 0
    }
    $workshop=Get-WorkshopRoot $GamePath
    if($Mode -eq 'Auto'){
        $Mode=if(Test-Path -LiteralPath "$workshop/3823110766/plugins/CraftopiaArchipelago/CraftopiaArchipelago.dll"){'Workshop'}else{'Manual'}
    }
    Write-Host "Install: $Mode (your saves and AP progress will be preserved)"
    $patcher=Join-Path $workshop '2519948110/BepInEx/patchers/BepInEx.MultiFolderLoader.dll'
    if($Mode -eq 'Workshop' -and -not(Test-Path -LiteralPath $patcher)){throw 'Let Steam finish downloading the official Craftopia Mod Loader dependency, then run Setup again.'}
    if(-not $NonInteractive){$null=Read-Host 'Press Enter to install, or close this window to cancel'}
    $core=Join-Path $GamePath 'BepInEx/core/BepInEx.dll'
    if(Test-Path -LiteralPath $core){
        $version=[Reflection.AssemblyName]::GetAssemblyName($core).Version
        if($version.Major -ne 5 -or $version -gt [version]'5.4.23.5'){throw "Installed loader $version differs from the tested version. No files changed."}
    }
    $stage=Join-Path ([IO.Path]::GetTempPath()) ('CraftopiaAP-Setup-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage|Out-Null
    try {
        [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
        $zip=Join-Path $stage 'loader.zip'
        Write-Host 'Downloading the verified BepInEx loader from its official GitHub release...'
        Invoke-WebRequest -Uri $script:LoaderUrl -OutFile $zip -UseBasicParsing
        if((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $script:LoaderHash){throw 'Loader download failed its integrity check. No game files changed.'}
        $loader=Join-Path $stage 'loader'
        Expand-Archive -LiteralPath $zip -DestinationPath $loader
        Invoke-CraftopiaInstall -Game $GamePath -Loader $loader -Payload "$PSScriptRoot/mod" -Mode $Mode -Patcher $patcher
    } finally {
        $tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/')
        $resolved=[IO.Path]::GetFullPath($stage)
        if($resolved.StartsWith($tempRoot+'\',[StringComparison]::OrdinalIgnoreCase) -and (Split-Path $resolved -Leaf) -match '^CraftopiaAP-Setup-[a-f0-9]{32}$'){
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
} catch {
    Write-Host "`nSetup stopped: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'If files were already installed, Restore Previous Setup.cmd uses the saved backup. AP progress is never deleted.'
    exit 1
}
