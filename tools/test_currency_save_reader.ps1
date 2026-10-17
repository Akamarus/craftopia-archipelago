param(
    [Parameter(Mandatory=$true)][string]$FixturePath,
    [Parameter(Mandatory=$true)][string]$GamePath
)
$ErrorActionPreference='Stop'
# Fixture must contain Entity rows PlayerSave=player-fixture, InGame=inventory-fixture.
# Never creates or writes a database; use a disposable fixture made by Python SQLite.
$fixture=(Resolve-Path -LiteralPath $FixturePath).Path
$sqlAssembly=[Reflection.Assembly]::LoadFrom((Join-Path $GamePath 'Craftopia_Data/Managed/SQLiteNet.dll'))
$script:currencySqliteHandle=[Runtime.InteropServices.NativeLibrary]::Load((Join-Path $GamePath 'Craftopia_Data/Plugins/x86_64/sqlite3.dll'))
[Runtime.InteropServices.NativeLibrary]::SetDllImportResolver($sqlAssembly,[Runtime.InteropServices.DllImportResolver]{
    param($name,$assembly,$search)
    if($name -eq 'sqlite3'){return $script:currencySqliteHandle}
    return [IntPtr]::Zero
})
$modPath=Join-Path $PSScriptRoot '../mod/CraftopiaArchipelago/bin/Release/netstandard2.1/CraftopiaArchipelago.dll'
$modAssembly=[Reflection.Assembly]::LoadFrom((Resolve-Path $modPath).Path)
$method=$modAssembly.GetType('CraftopiaArchipelago.CurrencySaveReader').GetMethod('Matches',[Reflection.BindingFlags]'Static,NonPublic')
$before=(Get-FileHash -LiteralPath $fixture).Hash
if(-not $method.Invoke($null,@($fixture,'player-fixture','inventory-fixture'))){throw 'Exact records did not verify'}
if($method.Invoke($null,@($fixture,'player-fixture','other'))){throw 'Wrong inventory verified'}
if($method.Invoke($null,@($fixture,'other','inventory-fixture'))){throw 'Wrong character verified'}
$absent=$fixture+'.absent-'+[Guid]::NewGuid().ToString('N')
if($method.Invoke($null,@($absent,'player-fixture','inventory-fixture'))){throw 'Missing file verified'}
if(Test-Path -LiteralPath $absent){throw 'Reader created missing file'}
if($before -ne (Get-FileHash -LiteralPath $fixture).Hash){throw 'Reader changed database'}
Write-Output 'CurrencySaveReader: 6 assertions passed (actual game SQLite library, disposable database).'
