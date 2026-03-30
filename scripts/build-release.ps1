param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "sync-pluginmaster.ps1")
dotnet build (Join-Path $PSScriptRoot "..\Avarice.sln") -c $Configuration

$zipPath = Join-Path $PSScriptRoot "..\Avarice\bin\x64\$Configuration\Avarice\latest.zip"
if (-not (Test-Path -LiteralPath $zipPath)) {
    throw "Expected release package was not generated at $zipPath"
}

Write-Host "Release package ready at $zipPath"
