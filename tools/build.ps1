param(
    [string]$GameDllRoot = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($GameDllRoot)) {
    $GameDllRoot = Join-Path $PSScriptRoot "..\external\Managed"
}

$proj = Join-Path $PSScriptRoot "..\src\CaleHenituseAbnormality.csproj"

$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    & $msbuild.Source $proj /p:Configuration=$Configuration /p:GameDllRoot="$GameDllRoot"
}
else {
    dotnet build $proj `
        -c $Configuration `
        -p:GameDllRoot="$GameDllRoot"
}

Write-Host ""
Write-Host "Build finished."
Write-Host "Output: $PSScriptRoot\..\dist\CaleHenituseAbnormality\"
