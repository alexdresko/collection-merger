#!/usr/bin/env pwsh
<##
.SYNOPSIS
    Bumps the package version, commits, tags, and pushes to trigger the NuGet release workflow.

.DESCRIPTION
    This script:
    - Reads <Version> from src/CollectionMerger/CollectionMerger.csproj
    - Prompts for a new SemVer (Major.Minor.Patch)
    - Updates the csproj version
    - Creates a conventional-commit release commit
    - Creates and pushes tag vX.Y.Z (triggers GitHub Actions release)

.PARAMETER Version
    Optional. Specify the version directly without prompting.

.EXAMPLE
    ./scripts/Release-Version.ps1

.EXAMPLE
    ./scripts/Release-Version.ps1 -Version 1.2.3
##>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$csprojPath = Join-Path $repoRoot 'src\CollectionMerger\CollectionMerger.csproj'

if (-not (Test-Path (Join-Path $repoRoot '.git'))) {
    throw "This script must be run from the repository root."
}

if (-not (Test-Path $csprojPath)) {
    throw "Could not find CollectionMerger.csproj at: $csprojPath"
}

function Get-CurrentVersion {
    param([string]$Path)

    $content = Get-Content -Raw -LiteralPath $Path
    if ($content -match '<Version>(\d+\.\d+\.\d+)</Version>') {
        return $matches[1]
    }

    throw "Could not find <Version> in $Path"
}

function Get-NextPatchVersion {
    param([string]$CurrentVersion)

    $parts = $CurrentVersion.Split('.')
    if ($parts.Length -ne 3) {
        throw "Version must be in format Major.Minor.Patch"
    }

    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]

    return "$major.$minor.$($patch + 1)"
}

function Set-CsprojVersion {
    param(
        [string]$Path,
        [string]$NewVersion
    )

    $content = Get-Content -Raw -LiteralPath $Path

    if ($content -notmatch '<Version>') {
        throw "Expected <Version> element to exist in $Path"
    }

    $updated = $content -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$NewVersion</Version>"
    Set-Content -LiteralPath $Path -Value $updated -NoNewline
}

Write-Host "`n=== CollectionMerger Release Manager ===" -ForegroundColor Cyan

$currentVersion = Get-CurrentVersion -Path $csprojPath
Write-Host "Current version: " -NoNewline
Write-Host $currentVersion -ForegroundColor Yellow

$suggested = Get-NextPatchVersion -CurrentVersion $currentVersion
Write-Host "Suggested next version: " -NoNewline
Write-Host $suggested -ForegroundColor Green

if (-not $Version) {
    Write-Host "`nEnter new version (press Enter for suggested): " -NoNewline -ForegroundColor Cyan
    $inputVersion = Read-Host

    if ([string]::IsNullOrWhiteSpace($inputVersion)) {
        $Version = $suggested
    } else {
        $Version = $inputVersion.Trim()
    }
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Invalid version format. Must be Major.Minor.Patch (e.g. 1.2.3)"
}

# Safety: require clean working tree
$dirty = git status --porcelain
if ($dirty) {
    throw "Working tree is not clean. Commit/stash changes before releasing."
}

if ($Version -eq $currentVersion) {
    $existingTag = git tag --list "v$Version"

    if ($existingTag) {
        Write-Host "Version unchanged and tag v$Version already exists; nothing to do." -ForegroundColor Yellow
        exit 0
    }

    Write-Host "`nVersion matches current csproj; will create and push tag only." -ForegroundColor Cyan
    Write-Host "This will:" -ForegroundColor Cyan
    Write-Host "  1. Create tag v$Version"
    Write-Host "  2. Push tag to origin (triggers NuGet publish)"
    Write-Host "`nContinue? [Y/n]: " -NoNewline -ForegroundColor Yellow
    $confirmTagOnly = Read-Host
    if ($confirmTagOnly -and $confirmTagOnly -notin @('Y','y')) {
        Write-Host "Release cancelled." -ForegroundColor Red
        exit 0
    }

    Write-Host "Creating tag v$Version..." -ForegroundColor Cyan
    git tag "v$Version"

    Write-Host "Pushing tag v$Version..." -ForegroundColor Cyan
    git push origin "v$Version"

    Write-Host "`nRelease initiated. Monitor GitHub Actions runs." -ForegroundColor Green
    exit 0
}

Write-Host "`nThis will:" -ForegroundColor Cyan
Write-Host "  1. Update version in CollectionMerger.csproj"
Write-Host "  2. Commit (chore(release): v$Version)"
Write-Host "  3. Create tag v$Version"
Write-Host "  4. Push commit and tag (triggers NuGet publish)"
Write-Host "`nContinue? [Y/n]: " -NoNewline -ForegroundColor Yellow
$confirm = Read-Host
if ($confirm -and $confirm -notin @('Y','y')) {
    Write-Host "Release cancelled." -ForegroundColor Red
    exit 0
}

Write-Host "`nUpdating csproj version..." -ForegroundColor Cyan
Set-CsprojVersion -Path $csprojPath -NewVersion $Version

Write-Host "Committing version bump..." -ForegroundColor Cyan
git add -- $csprojPath

git commit -m "chore(release): v$Version"

Write-Host "Creating tag v$Version..." -ForegroundColor Cyan
git tag "v$Version"

Write-Host "Pushing commit..." -ForegroundColor Cyan
$branch = git branch --show-current
git push origin $branch

Write-Host "Pushing tag v$Version..." -ForegroundColor Cyan
git push origin "v$Version"

Write-Host "`nRelease initiated. Monitor GitHub Actions runs." -ForegroundColor Green
