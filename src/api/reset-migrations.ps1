#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Regenerates a single clean migration (default name "Init") from the current model and applies it to the database.

.DESCRIPTION
  Deletes all existing files under Migrations/ (including the model snapshot), then runs
  `dotnet ef migrations add <Name>` followed by `dotnet ef database update`.

  Use this after resetting migration history during template development (e.g. after changing
  AppDbContext.OnModelCreating seed data), not against a database with real user data — deleting
  migration history and regenerating from scratch is a squash, not an incremental change.

  NOTE: run this against a project that already has a single, resolved DatabaseProvider — either a
  `dotnet new` generated project, or this template's own dev-instance (this file's parent folder)
  ONLY if its appsettings.json currently has exactly one active ConnectionStrings block. The raw,
  un-instantiated template source intentionally carries all three provider blocks side by side (each
  gated by a template conditional block) so `dotnet new` can pick the right one — that raw file will not parse on
  its own via `dotnet ef` because of the resulting duplicate keys; that's expected, not a bug.

.PARAMETER Name
  Migration name to generate. Defaults to "Init".

.PARAMETER Force
  Skip the confirmation prompt before deleting existing migration files.

.PARAMETER SkipDatabaseUpdate
  Only (re)generate the migration; do not run `dotnet ef database update`.

.EXAMPLE
  ./reset-migrations.ps1
  ./reset-migrations.ps1 -Force
  ./reset-migrations.ps1 -Name Init -SkipDatabaseUpdate
#>
param(
  [string]$Name = "Init",
  [switch]$Force,
  [switch]$SkipDatabaseUpdate
)

$ErrorActionPreference = "Stop"

$ContentRoot = $PSScriptRoot
$ProjectDir = Join-Path $ContentRoot "Jyz"
$Csproj = Join-Path $ProjectDir "Jyz.csproj"
$MigrationsDir = Join-Path $ProjectDir "Migrations"

if (-not (Test-Path $Csproj)) {
  throw "Project not found: $Csproj"
}

$existingMigrations = @()
if (Test-Path $MigrationsDir) {
  $existingMigrations = Get-ChildItem -Path $MigrationsDir -Filter "*.cs" -File
}

if ($existingMigrations.Count -gt 0) {
  Write-Host "[1/3] Found $($existingMigrations.Count) existing migration file(s) that will be deleted:" -ForegroundColor Yellow
  $existingMigrations | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }

  if (-not $Force) {
    $answer = Read-Host "This regenerates a single '$Name' migration from the current model (squash, not incremental). Continue? [y/N]"
    if ($answer -notin @("y", "Y", "yes", "Yes")) {
      Write-Host "Aborted." -ForegroundColor Red
      exit 1
    }
  }

  Remove-Item -Path (Join-Path $MigrationsDir "*.cs") -Force
  Write-Host "Deleted existing migration files." -ForegroundColor Green
}
else {
  Write-Host "[1/3] No existing migration files found; generating from a clean slate." -ForegroundColor Cyan
}

Write-Host "[2/3] Generating migration '$Name'..." -ForegroundColor Cyan
dotnet ef migrations add $Name --project $Csproj --startup-project $Csproj
if ($LASTEXITCODE -ne 0) {
  throw "dotnet ef migrations add failed."
}

if ($SkipDatabaseUpdate) {
  Write-Host "Skipped database update (-SkipDatabaseUpdate). Migration '$Name' generated successfully." -ForegroundColor Green
  exit 0
}

$activeEnvironment = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Production" }
Write-Host "[3/3] Applying migration to the database (dotnet ef database update)..." -ForegroundColor Cyan
Write-Host "Target connection: whatever 'ConnectionStrings' resolves to for the active ASPNETCORE_ENVIRONMENT ($activeEnvironment)." -ForegroundColor DarkYellow
dotnet ef database update --project $Csproj --startup-project $Csproj
if ($LASTEXITCODE -ne 0) {
  throw "dotnet ef database update failed. Check that the target database in your appsettings/user-secrets is reachable."
}

Write-Host "Done. Migration '$Name' generated and applied." -ForegroundColor Green
