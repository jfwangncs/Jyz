#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Syncs to the release branch, resolves api's and/or web's own version, confirms with you, then tags+pushes, triggering
  .github/workflows/docker-push.yml (which builds on push of a tag matching `api-*.*.*` / `web-*.*.*`).

.DESCRIPTION
  Reads the api version from `src/api/Jyz/Jyz.csproj`'s `<Version>` and the web
  version from `src/web/apps/web-antd/package.json`'s `appVersion` — they are independent, so bump
  whichever side(s) changed before running this. Prompts for api / web / both (default both) unless
  `-Target api|web|both` is passed, to release just one side (e.g. a frontend-only style fix shouldn't force-republish an
  unchanged backend image). Before resolving versions, checks out `-Branch` (default `master`) and
  pulls the latest from `-Remote`, so the tag always points at the real tip of the release branch —
  not whatever happened to be checked out locally. Shows the resolved tag(s) and asks for confirmation
  before tagging or pushing anything.

.EXAMPLE
  ./release.ps1              # releases both api and web
.EXAMPLE
  ./release.ps1 -Target web  # releases only web
#>
param(
  [ValidateSet('api', 'web', 'both')]
  [string]$Target,
  [string]$Remote = "origin",
  [string]$Branch = "master"
)
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot

function Sync-ReleaseBranch {
  $dirty = git -C $RepoRoot status --porcelain
  if ($dirty) {
    throw "Working tree has uncommitted changes — commit or stash them before releasing."
  }
  Write-Host "Checking out $Branch..." -ForegroundColor Cyan
  git -C $RepoRoot checkout $Branch
  if ($LASTEXITCODE -ne 0) { throw "git checkout $Branch failed." }
  Write-Host "Pulling latest $Branch from $Remote..." -ForegroundColor Cyan
  git -C $RepoRoot pull $Remote $Branch
  if ($LASTEXITCODE -ne 0) { throw "git pull failed." }
}

function Assert-TagAvailable {
  param([string]$Tag)
  $existingTag = git -C $RepoRoot tag -l $Tag
  if ($existingTag) {
    throw "Tag '$Tag' already exists locally. Bump the version first, or delete the tag if this was a mistake (git tag -d $Tag)."
  }
  $existingRemoteTag = git -C $RepoRoot ls-remote --tags $Remote "refs/tags/$Tag"
  if ($existingRemoteTag) {
    throw "Tag '$Tag' already exists on remote '$Remote'. Bump the version first."
  }
}

function Publish-Tag {
  param([string]$Tag)
  Write-Host "Tagging $Tag and pushing to $Remote..." -ForegroundColor Cyan
  git -C $RepoRoot tag $Tag
  git -C $RepoRoot push $Remote $Tag
  if ($LASTEXITCODE -ne 0) {
    throw "git push failed."
  }
  Write-Host "Pushed tag $Tag — docker-push.yml will build and push the image." -ForegroundColor Green
}

function Get-ApiVersion {
  $csprojPath = Join-Path $RepoRoot "src/api/Jyz/Jyz.csproj"
  if (-not (Test-Path $csprojPath)) { throw "Backend project file not found: $csprojPath" }
  $versionNode = Select-Xml -Path $csprojPath -XPath '//Version' | Select-Object -First 1
  if (-not $versionNode) { throw "<Version> not found in $csprojPath" }
  return $versionNode.Node.InnerText.Trim()
}

function Get-WebVersion {
  $packageJsonPath = Join-Path $RepoRoot "src/web/apps/web-antd/package.json"
  if (-not (Test-Path $packageJsonPath)) { throw "Frontend package.json not found: $packageJsonPath" }
  $version = (Get-Content $packageJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json).appVersion
  if ([string]::IsNullOrWhiteSpace($version)) { throw "appVersion not found in $packageJsonPath — this is our own field, distinct from the vendored Vben 'version'" }
  return $version
}

Sync-ReleaseBranch

if (-not $Target) {
  Write-Host "What do you want to release?" -ForegroundColor Cyan
  Write-Host "  1) api"
  Write-Host "  2) web"
  Write-Host "  3) both (default)"
  $choice = Read-Host "Select [1-3]"
  $Target = switch ($choice) {
    '1' { 'api' }
    '2' { 'web' }
    default { 'both' }
  }
}
$releases = [System.Collections.Generic.List[object]]::new()
if ($Target -eq 'api' -or $Target -eq 'both') { $releases.Add([PSCustomObject]@{ Prefix = 'api'; Version = (Get-ApiVersion) }) }
if ($Target -eq 'web' -or $Target -eq 'both') { $releases.Add([PSCustomObject]@{ Prefix = 'web'; Version = (Get-WebVersion) }) }
foreach ($r in $releases) {
  $r | Add-Member -NotePropertyName Tag -NotePropertyValue "$($r.Prefix)-$($r.Version)"
  Assert-TagAvailable -Tag $r.Tag
}

Write-Host ""
Write-Host "About to tag and push:" -ForegroundColor Yellow
foreach ($r in $releases) { Write-Host "  $($r.Tag)" -ForegroundColor Yellow }
$confirmation = Read-Host "Proceed? [y/N]"
if ($confirmation -notin @("y", "Y", "yes", "Yes")) {
  Write-Host "Aborted — no tags were created." -ForegroundColor Red
  exit 1
}

foreach ($r in $releases) {
  Publish-Tag -Tag $r.Tag
}
