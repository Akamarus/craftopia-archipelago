param(
    [Parameter(Mandatory=$true)][string]$CecilPath,
    [string]$GameAssembly = (Join-Path $PSScriptRoot '../refs/AD__Overcraft.dll'),
    [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
Add-Type -Path $CecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $GameAssembly).Path)
$source = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ObservationHooks.cs') -Raw
$specs = [regex]::Matches($source, 'new Spec\("([^"]+)", "([^"]+)", (new string\[0\]|new\[\]\{([^}]*)\})')
if ($specs.Count -ne 22) { throw "Unexpected observer count: $($specs.Count)" }
$rows = foreach ($spec in $specs) {
    $typeName = $spec.Groups[1].Value
    $methodName = $spec.Groups[2].Value
    $parameterTypes = @([regex]::Matches($spec.Groups[4].Value, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
    $type = $assembly.MainModule.Types | Where-Object FullName -eq $typeName
    $matches = @($type.Methods | Where-Object {
        $_.Name -eq $methodName -and
        ($_.Parameters.ParameterType.FullName -join ',') -eq ($parameterTypes -join ',')
    })
    if ($matches.Count -ne 1 -or $matches[0].IsStatic -or -not $matches[0].HasBody) {
        throw "Unverified observer target: $typeName::$methodName"
    }
    [pscustomobject]@{type=$typeName; method=$methodName; parameters=@($matches[0].Parameters | ForEach-Object {
        [pscustomobject]@{name=$_.Name;type=$_.ParameterType.FullName}
    }); returns=$matches[0].ReturnType.FullName; status='signature_verified_not_runtime_tested'}
}
$report = [pscustomobject]@{assembly_sha256=(Get-FileHash -LiteralPath $GameAssembly -Algorithm SHA256).Hash; hooks=@($rows)}
if ($OutputPath) { $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding utf8 }
$assembly.Dispose()
Write-Output "PASS: all 22 observer signatures match the supplied game assembly. Runtime patch execution still needs a live test."
