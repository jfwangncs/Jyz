#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Cleanly removes everything deploy.ps1 set up for this project: the ArgoCD Application, the
  ImageUpdater, and the Secret you created by hand. Reverses deploy.ps1 — run this before
  re-deploying from scratch, or when retiring the project from the cluster.

.DESCRIPTION
  Reads the namespace back out of k8s/<slug>-application.yaml (the same file deploy.ps1 wrote),
  so this always targets the same namespace deploy.ps1 actually deployed to — you don't retype it
  and risk it drifting from what's really running.

  Deletes in this order, which matters:
    1. The ArgoCD Application (k8s/<slug>-application.yaml) and the ImageUpdater
       (k8s/<slug>-image-updater.yaml) first. The Application has `syncPolicy.automated` with
       `prune: true` and `selfHeal: true` — if you deleted the Rollout/Service/Ingress
       directly first, while the Application still exists, ArgoCD would just sync them straight
       back. Deleting the Application itself removes ArgoCD's management of this app, and (since
       the Application resource's own deletion cascades via ArgoCD's finalizer) it also cleans
       up every resource it created from the Kustomize output.
    2. Waits for the Application resource to actually disappear from the `argocd` namespace
       before continuing, so step 3 isn't racing ArgoCD's own cascade delete.
    3. The Secret you created by hand when you first ran deploy.ps1. This was never managed by
       ArgoCD/Kustomize (deliberately, so secrets never touch Git), so it has to be deleted
       separately — ArgoCD pruning in step 1 never touches it.

  This script never touches your ArgoCD Git repo. If you already copied k8s/<slug>/ there,
  delete that directory from the ArgoCD repo yourself and commit — otherwise the Kustomize
  source is still sitting there for the next person (or ArgoCD UI "New App") to pick back up.

.PARAMETER KubeContext
  kubectl context to operate against. Defaults to whatever is currently active
  (`kubectl config current-context`) — pass this explicitly if you manage multiple clusters and
  want to be certain which one this runs against.

.EXAMPLE
  ./undeploy.ps1
#>
param(
  [string]$KubeContext = $null
)
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$DockerImageSlug = "jyz"

$k8sDir = Join-Path $RepoRoot "k8s"
$applicationPath = Join-Path $k8sDir "$DockerImageSlug-application.yaml"
$imageUpdaterPath = Join-Path $k8sDir "$DockerImageSlug-image-updater.yaml"

if (-not (Test-Path $applicationPath)) {
  throw "k8s/$DockerImageSlug-application.yaml not found — run deploy.ps1 first (this script undoes what it set up)."
}

if ($KubeContext) {
  Write-Host "Using kubectl context: $KubeContext" -ForegroundColor Cyan
  kubectl config use-context $KubeContext
  if ($LASTEXITCODE -ne 0) { throw "kubectl config use-context failed." }
}
else {
  $currentContext = (kubectl config current-context 2>$null)
  Write-Host "Using current kubectl context: $currentContext" -ForegroundColor Cyan
}

# Read the namespace back out of the Application manifest itself, rather than asking again —
# this is the one deploy.ps1 actually used, so there's no chance of targeting the wrong one.
$applicationYaml = Get-Content -Path $applicationPath -Raw
$namespaceMatch = [regex]::Match($applicationYaml, 'destination:\s*\r?\n\s*server:.*\r?\n\s*namespace:\s*(\S+)')
if (-not $namespaceMatch.Success) {
  throw "Could not find spec.destination.namespace in $applicationPath — was this file hand-edited?"
}
$Namespace = $namespaceMatch.Groups[1].Value
$SecretName = "$DockerImageSlug-secrets"

Write-Host ""
Write-Host "About to delete:" -ForegroundColor Yellow
Write-Host "  ArgoCD Application '$DockerImageSlug' (namespace: argocd)"
if (Test-Path $imageUpdaterPath) {
  Write-Host "  ArgoCD ImageUpdater '$DockerImageSlug' (namespace: argocd)"
}
Write-Host "  -> which will cascade-delete every Rollout/Service/Ingress this app owns in namespace '$Namespace'"
Write-Host "  Secret '$SecretName' (namespace: $Namespace)"
Write-Host ""
$confirmation = Read-Host "Proceed? [y/N]"
if ($confirmation -notin @("y", "Y", "yes", "Yes")) {
  Write-Host "Aborted — nothing was deleted." -ForegroundColor Red
  exit 1
}

Write-Host ""
Write-Host "Deleting ArgoCD Application + ImageUpdater..." -ForegroundColor Cyan
$deleteArgs = @("delete", "-f", $applicationPath, "--ignore-not-found")
if (Test-Path $imageUpdaterPath) { $deleteArgs += @("-f", $imageUpdaterPath) }
& kubectl @deleteArgs
if ($LASTEXITCODE -ne 0) {
  throw "kubectl delete failed for the Application/ImageUpdater."
}

Write-Host ""
Write-Host "Waiting for ArgoCD to finish cascading the delete (Application resource removed from 'argocd' namespace)..." -ForegroundColor Cyan
$deadline = (Get-Date).AddMinutes(3)
$stillExists = $true
while ((Get-Date) -lt $deadline) {
  kubectl get application $DockerImageSlug -n argocd 2>$null 1>$null
  if ($LASTEXITCODE -ne 0) {
    $stillExists = $false
    break
  }
  Start-Sleep -Seconds 3
}
if ($stillExists) {
  Write-Host "Application '$DockerImageSlug' is still terminating after 3 minutes — it has a finalizer" -ForegroundColor Yellow
  Write-Host "that waits for ArgoCD to finish deleting its managed resources. Check manually with:" -ForegroundColor Yellow
  Write-Host "  kubectl get application $DockerImageSlug -n argocd" -ForegroundColor Yellow
  Write-Host "and re-run this script (or just the secret delete below) once it's gone." -ForegroundColor Yellow
}
else {
  Write-Host "Application removed." -ForegroundColor Green
}

Write-Host ""
Write-Host "Deleting Secret '$SecretName' in namespace '$Namespace'..." -ForegroundColor Cyan
kubectl delete secret $SecretName -n $Namespace --ignore-not-found
if ($LASTEXITCODE -ne 0) {
  throw "kubectl delete secret failed."
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "Cluster-side resources for '$DockerImageSlug' have been removed."
Write-Host ""
Write-Host "This did not touch your ArgoCD Git repo. If you already copied k8s/$DockerImageSlug/ there," -ForegroundColor Yellow
Write-Host "delete that directory from the ArgoCD repo and commit — otherwise the Kustomize source is" -ForegroundColor Yellow
Write-Host "still sitting there for ArgoCD (or someone in the UI) to pick back up." -ForegroundColor Yellow

