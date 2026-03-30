param(
    [string]$RepoOwner = "ShiftyKiwi",
    [string]$RepoName = "Avarice",
    [string]$Branch = "main",
    [string]$ManifestPath = "Avarice/Avarice.json",
    [string]$ProjectPath = "Avarice/Avarice.csproj",
    [string]$OutputPath = "pluginmaster.json",
    [string]$Changelog = "See GitHub release notes."
)

$ErrorActionPreference = "Stop"

$projectFullPath = Join-Path $PSScriptRoot "..\$ProjectPath"
$manifestFullPath = Join-Path $PSScriptRoot "..\$ManifestPath"
$outputFullPath = Join-Path $PSScriptRoot "..\$OutputPath"

[xml]$projectXml = Get-Content -LiteralPath $projectFullPath -Raw
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not determine plugin version from $ProjectPath"
}

$manifest = Get-Content -LiteralPath $manifestFullPath -Raw | ConvertFrom-Json

$repoUrl = "https://github.com/$RepoOwner/$RepoName"
$iconUrl = "https://raw.githubusercontent.com/$RepoOwner/$RepoName/$Branch/Assets/avarice_icon.png"
$downloadUrl = "$repoUrl/releases/download/v$version/latest.zip"

$entry = [ordered]@{
    Author = $manifest.Author
    Name = $manifest.Name
    InternalName = $manifest.InternalName
    AssemblyVersion = $version
    Description = $manifest.Description
    ApplicableVersion = $manifest.ApplicableVersion
    RepoUrl = $repoUrl
    Tags = @($manifest.Tags)
    DalamudApiLevel = $manifest.DalamudApiLevel
    LoadRequiredState = 0
    LoadSync = $false
    CanUnloadAsync = $false
    LoadPriority = 0
    IconUrl = $iconUrl
    Punchline = $manifest.Punchline
    AcceptsFeedback = $true
    DownloadLinkInstall = $downloadUrl
    DownloadLinkUpdate = $downloadUrl
    Changelog = $Changelog
}

$entryJson = $entry | ConvertTo-Json -Depth 10
$json = "[`n$entryJson`n]"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outputFullPath, $json + [Environment]::NewLine, $utf8NoBom)

Write-Host "Updated $OutputPath for version $version"
