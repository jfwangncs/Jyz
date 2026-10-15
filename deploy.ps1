#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Interactive: generates an ArgoCD/Kustomize/Argo-Rollouts manifest set for this project (api
  and/or web, whichever were generated), in the same shape as a hand-maintained ArgoCD GitOps
  repo (one `kustomization.yaml` + `Rollout`/`Service`/`Ingress` per component, plus a standalone
  `Application` and `ImageUpdater` resource). You copy the output into your ArgoCD repo yourself —
  this script never pushes or touches any repo but this project's own. Also prints the kubectl
  commands you run once, by hand, to create the Secrets it references. Does not touch any
  application code.

.DESCRIPTION
  Auto-detects which optional backend capabilities are present (RBAC, JWT, JWT+Redis, WeChat,
  OpenTelemetry) by reading this project's own generated files, and reads the image tag to deploy
  straight from the same version fields release.ps1 publishes from — you don't re-answer what
  quick-start.ps1 already asked, or retype a version release.ps1 already recorded. Then prompts
  for the handful of deploy-time choices that aren't derivable (Docker Hub username, namespace,
  internal vs external network, hostnames, cert-manager issuer, replica counts, canary rollout
  strategy, and — if web is present — the backend API URL the frontend should call) and writes:

    k8s/<slug>/kustomization.yaml       — Kustomize entrypoint (namespace, resources, images)
    k8s/<slug>/rollout-api.yaml          — Argo Rollout for api
    k8s/<slug>/rollout-web.yaml          — Argo Rollout for web (same canary strategy as api)
    k8s/<slug>/service-api.yaml, service-web.yaml
    k8s/<slug>/ingress-api.yaml, ingress-web.yaml
    k8s/<slug>-application.yaml          — standalone ArgoCD Application (not inside k8s/<slug>/,
                                            so it never gets picked up as a Kustomize resource)
    k8s/<slug>-image-updater.yaml        — standalone ImageUpdater (current/recommended
                                            configuration style — see the note above the
                                            Application generation code for why this replaces
                                            the legacy argocd-image-updater.argoproj.io/*
                                            annotations some older Applications use)

  Copy k8s/<slug>/ into your ArgoCD repo (e.g. next to its existing `nga`/`argotest` apps) and
  k8s/<slug>-application.yaml + k8s/<slug>-image-updater.yaml wherever you keep those, then
  `kubectl apply` both.

  Secrets never go into the generated YAML or into Git: this script only prints
  `kubectl create secret generic ...` command templates with placeholder values. Fill in the real
  values yourself and run them once directly against the target cluster.

.EXAMPLE
  ./deploy.ps1
#>
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$ProjectName = "Jyz"
$DockerImageSlug = "jyz"

function Read-MenuChoice {
  param(
    [string]$Prompt,
    [string[]]$Options,
    [int]$DefaultIndex
  )
  Write-Host ""
  Write-Host $Prompt -ForegroundColor Cyan
  for ($i = 0; $i -lt $Options.Count; $i++) {
    $marker = if ($i -eq $DefaultIndex) { " (default)" } else { "" }
    Write-Host "  [$($i + 1)] $($Options[$i])$marker"
  }
  while ($true) {
    $raw = Read-Host "Enter a number (1-$($Options.Count)), or press Enter for default"
    if ([string]::IsNullOrWhiteSpace($raw)) { return $DefaultIndex }
    if ($raw -match '^\d+$') {
      $idx = [int]$raw - 1
      if ($idx -ge 0 -and $idx -lt $Options.Count) { return $idx }
    }
    Write-Host "Invalid choice. Try again." -ForegroundColor Red
  }
}

function Read-Default {
  param([string]$Prompt, [string]$Default)
  $raw = Read-Host "$Prompt [$Default]"
  if ([string]::IsNullOrWhiteSpace($raw)) { return $Default }
  return $raw
}

# ── Auto-detect what this project actually has, by reading its own generated files ──────────
# No manifest of quick-start.ps1's choices is persisted anywhere, so this introspects the
# generated output directly rather than re-asking the same questions quick-start.ps1 already asked.
$apiDir = Join-Path $RepoRoot "src/api/$ProjectName"
$webDir = Join-Path $RepoRoot "src/web"

$hasBackend = Test-Path $apiDir
$hasFrontend = Test-Path (Join-Path $RepoRoot "src/web/apps/web-antd")

if (-not $hasBackend -and -not $hasFrontend) {
  throw "Neither src/api/$ProjectName nor src/web/apps/web-antd was found under $RepoRoot — is this script in the right place?"
}

$hasRBAC = $false
$hasJWT = $false
$hasJWTRedis = $false
$hasWeChat = $false
$hasTelemetry = $false
$databaseType = $null

if ($hasBackend) {
  $appsettingsPath = Join-Path $apiDir "appsettings.json"
  if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content -Path $appsettingsPath -Raw | ConvertFrom-Json
    $hasJWT = $null -ne $appsettings.JwtSettings
    $hasJWTRedis = $null -ne $appsettings.Redis
    $hasWeChat = $null -ne $appsettings.MiniProgram
    $hasRBAC = $null -ne $appsettings.ConnectionStrings
    if ($hasRBAC) { $databaseType = $appsettings.ConnectionStrings.DatabaseType }
  }
  $csprojPath = Join-Path $apiDir "$ProjectName.csproj"
  if (Test-Path $csprojPath) {
    $hasTelemetry = (Get-Content -Path $csprojPath -Raw) -match "OpenTelemetry\.Instrumentation\.AspNetCore"
  }
}

Write-Host "=== Detected capabilities ===" -ForegroundColor Green
Write-Host "  Backend:        $hasBackend"
if ($hasBackend) {
  Write-Host "    JWT:          $hasJWT"
  Write-Host "    JWT+Redis:    $hasJWTRedis"
  Write-Host "    RBAC:         $hasRBAC$(if ($hasRBAC) { " ($databaseType)" })"
  Write-Host "    WeChat:       $hasWeChat"
  Write-Host "    Telemetry:    $hasTelemetry"
}
Write-Host "  Frontend:       $hasFrontend"

if ($hasBackend) {
  Write-Host ""
  Write-Host "NOTE: the backend's CORS policy ('AllowAll') currently only accepts requests whose" -ForegroundColor Yellow
  Write-Host "Origin host is literally 'localhost' (see AddCustomCors() in ServicesExtension.cs)." -ForegroundColor Yellow
  Write-Host "Deployed behind a real hostname, every browser request from the frontend will be" -ForegroundColor Yellow
  Write-Host "rejected by CORS until that policy is changed to allow your real domain(s). This" -ForegroundColor Yellow
  Write-Host "script does not modify any .cs file — fix it in code before relying on this deploy." -ForegroundColor Yellow
}

# ── What to deploy: whatever this project has. quick-start.ps1 already decided which side(s)
# exist — no need to ask again here.
$deployBackend = $hasBackend
$deployFrontend = $hasFrontend

# ── Common settings ───────────────────────────────────────────────────────────
Write-Host ""
$DockerHubUsername = Read-Host "Docker Hub username"
if ([string]::IsNullOrWhiteSpace($DockerHubUsername)) {
  throw "Docker Hub username is required."
}

# Image tags are read straight from the same version fields release.ps1 tags+pushes from — not
# asked for — so this always matches whatever release.ps1 last published, with no chance of
# typing the wrong version by hand. This assumes that version has actually been released (i.e.
# release.ps1 has run for it) — deploying a version that was never pushed will just fail the
# image pull.
$apiTag = $null
$webTag = $null
if ($deployBackend) {
  $csprojPath = Join-Path $apiDir "$ProjectName.csproj"
  if (-not (Test-Path $csprojPath)) { throw "Backend project file not found: $csprojPath" }
  $versionNode = Select-Xml -Path $csprojPath -XPath '//Version' | Select-Object -First 1
  if (-not $versionNode) { throw "<Version> not found in $csprojPath" }
  $apiTag = $versionNode.Node.InnerText.Trim()
  Write-Host "Image tag for api (from $ProjectName.csproj's <Version>): $apiTag" -ForegroundColor Cyan
}
if ($deployFrontend) {
  $webPackageJsonPath = Join-Path $RepoRoot "src/web/apps/web-antd/package.json"
  if (-not (Test-Path $webPackageJsonPath)) { throw "Frontend package.json not found: $webPackageJsonPath" }
  $webTag = (Get-Content -Path $webPackageJsonPath -Raw | ConvertFrom-Json).appVersion
  if ([string]::IsNullOrWhiteSpace($webTag)) { throw "appVersion not found in $webPackageJsonPath" }
  Write-Host "Image tag for web (from package.json's appVersion): $webTag" -ForegroundColor Cyan
}

$Namespace = Read-Default -Prompt "Kubernetes namespace" -Default $DockerImageSlug

# Network mode is asked per side — api and web often sit on different sides of the network
# (e.g. api stays internal-only while web is exposed externally), so one shared answer would
# be wrong for a common deployment shape. Internal means plain HTTP, no TLS, no ClusterIssuer —
# cert-manager has nothing to issue a browser-trusted cert against for a private hostname
# without a self-signed/internal CA, which is extra infra most teams don't want to stand up
# just for this. External gets TLS via the real cert-manager ClusterIssuer already configured
# in the cluster — run `kubectl get clusterissuer` to see what's actually there; the default
# below is only a guess.
$isInternalApi = $false
$isInternalWeb = $false
$apiClusterIssuer = $null
$webClusterIssuer = $null
$apiHost = $null
$webHost = $null

if ($deployBackend) {
  $apiNetIdx = Read-MenuChoice -Prompt "Network mode for api:" -Options @(
    "Internal (private domain, plain HTTP — no TLS)",
    "External (public domain, HTTPS via cert-manager ClusterIssuer, e.g. Let's Encrypt)"
  ) -DefaultIndex 1
  $isInternalApi = ($apiNetIdx -eq 0)
  if (-not $isInternalApi) {
    $apiClusterIssuer = Read-Default -Prompt "cert-manager ClusterIssuer name for api" -Default "letsencrypt-http"
  }
  $apiHost = Read-Default -Prompt "Hostname for the api Ingress" -Default $(if ($isInternalApi) { "$DockerImageSlug-api.internal.xiaofengyu.com" } else { "$DockerImageSlug-api.xiaofengyu.com" })
}

if ($deployFrontend) {
  $webNetIdx = Read-MenuChoice -Prompt "Network mode for web:" -Options @(
    "Internal (private domain, plain HTTP — no TLS)",
    "External (public domain, HTTPS via cert-manager ClusterIssuer, e.g. Let's Encrypt)"
  ) -DefaultIndex 1
  $isInternalWeb = ($webNetIdx -eq 0)
  if (-not $isInternalWeb) {
    $webClusterIssuer = Read-Default -Prompt "cert-manager ClusterIssuer name for web" -Default "letsencrypt-http"
  }
  $webHost = Read-Default -Prompt "Hostname for the web Ingress" -Default $(if ($isInternalWeb) { "$DockerImageSlug.internal.xiaofengyu.com" } else { "$DockerImageSlug.xiaofengyu.com" })
}

# ── Replica count + canary rollout strategy ───────────────────────────────────
$apiReplicas = $null
$webReplicas = $null
if ($deployBackend) { $apiReplicas = Read-Default -Prompt "Number of api replicas" -Default "1" }
if ($deployFrontend) { $webReplicas = Read-Default -Prompt "Number of web replicas" -Default "1" }

# Canary steps are shared by api and web (one strategy per deploy run, not asked per side) since
# in practice you want the same rollout cadence across a release rather than tuning each
# component separately — matches how nga itself uses one steps list for all its Rollouts.
$canaryIdx = Read-MenuChoice -Prompt "Canary rollout strategy:" -Options @(
  "100% immediately (no canary — pause-free single step)",
  "100% but wait for manual approval first (kubectl argo rollouts promote / ArgoCD UI)",
  "20% -> wait for manual approval -> 50% -> auto-continue after 5m -> 100%",
  "Custom — choose your own number of steps, weight, and pause per step"
) -DefaultIndex 0
$canarySteps = switch ($canaryIdx) {
  0 { "        - setWeight: 100`n" }
  1 { @"
        - pause: {}
        - setWeight: 100
"@ }
  2 { @"
        - setWeight: 20
        - pause: {}
        - setWeight: 50
        - pause: { duration: 5m }
        - setWeight: 100
"@ }
  3 {
    # Each step is: a traffic weight, then how the Rollout waits before the next weight —
    # auto-continue after a fixed duration ("pause: { duration: ... }"), wait indefinitely for a
    # human to run `kubectl argo rollouts promote` / click Promote in the ArgoCD UI
    # ("pause: {}" — Argo Rollouts' own convention for "manual approval", confirmed in the
    # argotest chart's own rollout.yaml template), or move on immediately with no pause at all.
    $stepCountRaw = Read-Default -Prompt "How many canary steps (not counting the final 100%)?" -Default "3"
    $stepCount = [int]$stepCountRaw

    $customSteps = [System.Collections.Generic.List[string]]::new()
    for ($i = 1; $i -le $stepCount; $i++) {
      $weight = Read-Host "Step ${i}: traffic weight (percent, e.g. 20)"
      $customSteps.Add("        - setWeight: $weight")

      $pauseIdx = Read-MenuChoice -Prompt "Step ${i}: after reaching $weight%, how should it proceed?" -Options @(
        "Auto-continue after a fixed wait",
        "Wait for manual approval (kubectl argo rollouts promote / ArgoCD UI)",
        "No pause — go straight to the next step"
      ) -DefaultIndex 0
      switch ($pauseIdx) {
        0 {
          $pauseDuration = Read-Default -Prompt "Step ${i}: how long to wait (e.g. 30s, 5m, 1h)" -Default "30s"
          $customSteps.Add("        - pause: { duration: $pauseDuration }")
        }
        1 { $customSteps.Add("        - pause: {}") }
        2 { }
      }
    }
    $customSteps.Add("        - setWeight: 100")
    ($customSteps -join "`n") + "`n"
  }
}

# ── Secrets this project needs, derived from the detected capabilities ───────
# Matches the nga convention of one envFrom.secretRef Secret per app (see nga-secrets) rather
# than per-key secretKeyRef entries — simpler to review and matches what's already in the
# existing ArgoCD repo.
$secretName = "$DockerImageSlug-secrets"
$secretCommandLines = [System.Collections.Generic.List[string]]::new()
if ($hasJWT) { $secretCommandLines.Add("  --from-literal=JwtSettings__SecretKey='REPLACE_ME_32_CHARS_MIN'") }
if ($hasJWTRedis) { $secretCommandLines.Add("  --from-literal=Redis__Password='REPLACE_ME'") }
if ($hasRBAC) { $secretCommandLines.Add("  --from-literal=ConnectionStrings__ConnectionString='REPLACE_ME'") }
if ($hasWeChat) {
  $secretCommandLines.Add("  --from-literal=MiniProgram__AppId='REPLACE_ME'")
  $secretCommandLines.Add("  --from-literal=MiniProgram__Secret='REPLACE_ME'")
}

# ── Write manifests, in the same shape as an existing nga-style ArgoCD app ───────────────────
$k8sDir = Join-Path $RepoRoot "k8s"
$appDir = Join-Path $k8sDir $DockerImageSlug
New-Item -ItemType Directory -Path $appDir -Force | Out-Null

$kustomizeResources = [System.Collections.Generic.List[string]]::new()
$kustomizeImages = [System.Collections.Generic.List[string]]::new()

if ($deployBackend) {
  $apiImage = "$DockerHubUsername/$DockerImageSlug-api"

  $apiTelemetryAnnotations = if ($hasTelemetry) { "`n  annotations:`n    prometheus.io/scrape: `"true`"`n    prometheus.io/port: `"8080`"`n    prometheus.io/path: `"/metrics`"" } else { "" }
  $apiTelemetryPodAnnotations = if ($hasTelemetry) { "`n      annotations:`n        prometheus.io/scrape: `"true`"`n        prometheus.io/port: `"8080`"`n        prometheus.io/path: `"/metrics`"" } else { "" }
  $apiOtelEnv = if ($hasTelemetry) { @"

            - name: OTEL_SERVICE_NAME
              value: "$DockerImageSlug-api"
            - name: OTEL_EXPORTER_OTLP_ENDPOINT
              value: "http://tempo.observability.svc.cluster.local:4317"
            - name: OTEL_EXPORTER_OTLP_PROTOCOL
              value: "grpc"
            - name: OTEL_TRACES_SAMPLER
              value: "always_on"
"@ } else { "" }
  $apiEnvFrom = if ($secretCommandLines.Count -gt 0) { @"

          envFrom:
            - secretRef:
                name: $secretName
"@ } else { "" }

  $apiRolloutContent = @"
apiVersion: argoproj.io/v1alpha1
kind: Rollout
metadata:
  name: $DockerImageSlug-api$apiTelemetryAnnotations
spec:
  replicas: $apiReplicas
  selector:
    matchLabels:
      app: $DockerImageSlug-api
  template:
    metadata:
      labels:
        app: $DockerImageSlug-api$apiTelemetryPodAnnotations
    spec:
      containers:
        - name: $DockerImageSlug-api
          image: $apiImage`:$apiTag
          ports:
            - name: http
              containerPort: 8080
              protocol: TCP
          readinessProbe:
            httpGet:
              path: /api/health
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
            timeoutSeconds: 3
            successThreshold: 1
            failureThreshold: 3
          livenessProbe:
            httpGet:
              path: /api/health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 3
          env:
            - name: TZ
              value: "Asia/Shanghai"
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"$apiOtelEnv$apiEnvFrom
          resources:
            requests:
              memory: "128Mi"
              cpu: "20m"
            limits:
              memory: "256Mi"
              cpu: "500m"
  strategy:
    canary:
      maxSurge: 1
      maxUnavailable: 0
      steps:
$canarySteps
"@
  Set-Content -Path (Join-Path $appDir "rollout-api.yaml") -Value $apiRolloutContent -Encoding utf8

  $apiServiceContent = @"
apiVersion: v1
kind: Service
metadata:
  name: $DockerImageSlug-api
spec:
  type: ClusterIP
  ports:
    - port: 80
      targetPort: 8080
      protocol: TCP
      name: http
  selector:
    app: $DockerImageSlug-api
"@
  Set-Content -Path (Join-Path $appDir "service-api.yaml") -Value $apiServiceContent -Encoding utf8

  $apiIngressAnnotations = if ($isInternalApi) { "" } else { "`n    cert-manager.io/cluster-issuer: $apiClusterIssuer" }
  $apiIngressTls = if ($isInternalApi) { "" } else { "`n  tls:`n    - hosts:`n        - $apiHost`n      secretName: $DockerImageSlug-api-tls" }

  $apiIngressContent = @"
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: $DockerImageSlug-api-ingress
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "$(if ($isInternalApi) { "false" } else { "true" })"
    nginx.ingress.kubernetes.io/backend-protocol: "HTTP"$apiIngressAnnotations
spec:
  ingressClassName: nginx$apiIngressTls
  rules:
    - host: $apiHost
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: $DockerImageSlug-api
                port:
                  number: 80
"@
  Set-Content -Path (Join-Path $appDir "ingress-api.yaml") -Value $apiIngressContent -Encoding utf8

  $kustomizeResources.Add("rollout-api.yaml")
  $kustomizeResources.Add("service-api.yaml")
  $kustomizeResources.Add("ingress-api.yaml")
  $kustomizeImages.Add("  - name: $apiImage`n    newTag: `"$apiTag`"")

  Write-Host "Wrote: k8s/$DockerImageSlug/rollout-api.yaml, service-api.yaml, ingress-api.yaml" -ForegroundColor Green
}

if ($deployFrontend) {
  $webImage = "$DockerHubUsername/$DockerImageSlug-web"

  $webRolloutContent = @"
apiVersion: argoproj.io/v1alpha1
kind: Rollout
metadata:
  name: $DockerImageSlug-web
spec:
  replicas: $webReplicas
  selector:
    matchLabels:
      app: $DockerImageSlug-web
  template:
    metadata:
      labels:
        app: $DockerImageSlug-web
    spec:
      containers:
        - name: $DockerImageSlug-web
          image: $webImage`:$webTag
          ports:
            - name: http
              containerPort: 8080
              protocol: TCP
          env:
            - name: TZ
              value: "Asia/Shanghai"
          readinessProbe:
            httpGet:
              path: /
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
            timeoutSeconds: 3
            successThreshold: 1
            failureThreshold: 3
          livenessProbe:
            httpGet:
              path: /
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
            timeoutSeconds: 3
            failureThreshold: 3
          resources:
            requests:
              memory: "64Mi"
              cpu: "10m"
            limits:
              memory: "128Mi"
              cpu: "200m"
  strategy:
    canary:
      maxSurge: 1
      maxUnavailable: 0
      steps:
$canarySteps
"@
  Set-Content -Path (Join-Path $appDir "rollout-web.yaml") -Value $webRolloutContent -Encoding utf8

  $webServiceContent = @"
apiVersion: v1
kind: Service
metadata:
  name: $DockerImageSlug-web
spec:
  type: ClusterIP
  ports:
    - port: 80
      targetPort: 8080
      protocol: TCP
      name: http
  selector:
    app: $DockerImageSlug-web
"@
  Set-Content -Path (Join-Path $appDir "service-web.yaml") -Value $webServiceContent -Encoding utf8

  $webIngressAnnotations = if ($isInternalWeb) { "" } else { "`n    cert-manager.io/cluster-issuer: $webClusterIssuer" }
  $webIngressTls = if ($isInternalWeb) { "" } else { "`n  tls:`n    - hosts:`n        - $webHost`n      secretName: $DockerImageSlug-web-tls" }

  $webIngressContent = @"
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: $DockerImageSlug-web-ingress
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "$(if ($isInternalWeb) { "false" } else { "true" })"
    nginx.ingress.kubernetes.io/backend-protocol: "HTTP"$webIngressAnnotations
spec:
  ingressClassName: nginx$webIngressTls
  rules:
    - host: $webHost
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: $DockerImageSlug-web
                port:
                  number: 80
"@
  Set-Content -Path (Join-Path $appDir "ingress-web.yaml") -Value $webIngressContent -Encoding utf8

  $kustomizeResources.Add("rollout-web.yaml")
  $kustomizeResources.Add("service-web.yaml")
  $kustomizeResources.Add("ingress-web.yaml")
  $kustomizeImages.Add("  - name: $webImage`n    newTag: `"$webTag`"")

  Write-Host "Wrote: k8s/$DockerImageSlug/rollout-web.yaml, service-web.yaml, ingress-web.yaml" -ForegroundColor Green
}

$kustomizationContent = @"
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

namespace: $Namespace

resources:
$(($kustomizeResources | ForEach-Object { "  - $_" }) -join "`n")

images:
$($kustomizeImages -join "`n")
"@
Set-Content -Path (Join-Path $appDir "kustomization.yaml") -Value $kustomizationContent -Encoding utf8

Write-Host "Wrote: k8s/$DockerImageSlug/kustomization.yaml" -ForegroundColor Green

# ── ArgoCD Application (standalone — not inside k8s/<slug>/, so it's never picked up as a
# Kustomize resource of its own app) ──────────────────────────────────────────────────────────
$gitRemoteUrl = Read-Default -Prompt "Git repo URL this k8s/$DockerImageSlug directory will be copied into (your ArgoCD repo)" -Default "https://github.com/jfwangncs/ArgoCD.git"
$gitBranch = Read-Default -Prompt "Git branch ArgoCD should track" -Default "master"

# No argocd-image-updater.argoproj.io/* annotations here — those are the legacy configuration
# style. The current/recommended way is a standalone ImageUpdater CRD (generated below), and
# the two mechanisms conflict if both target the same Application: per the image updater's own
# docs, annotations only take effect when an ImageUpdater CR explicitly opts in with
# `useAnnotations: true`, and doing so makes the CR ignore its own `images`/`writeBackConfig` —
# mixing both approaches causes exactly the "thrashing" (fields overwritten every reconcile)
# the docs warn about.
$argocdAppContent = @"
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: $DockerImageSlug
  namespace: argocd
spec:
  project: default
  source:
    repoURL: $gitRemoteUrl
    targetRevision: $gitBranch
    path: $DockerImageSlug
  destination:
    server: https://kubernetes.default.svc
    namespace: $Namespace
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
  revisionHistoryLimit: 10
"@
Set-Content -Path (Join-Path $k8sDir "$DockerImageSlug-application.yaml") -Value $argocdAppContent -Encoding utf8

Write-Host "Wrote: k8s/$DockerImageSlug-application.yaml" -ForegroundColor Green

# ── ArgoCD Image Updater (standalone ImageUpdater CRD — current/recommended configuration
# style; see the note above on why this replaces annotation-based config) ────────────────────
$imageUpdaterEntries = [System.Collections.Generic.List[string]]::new()
if ($deployBackend) {
  $imageUpdaterEntries.Add(@"
        - alias: $DockerImageSlug-api
          imageName: $DockerHubUsername/$DockerImageSlug-api
          commonUpdateSettings:
            updateStrategy: "semver"
"@)
}
if ($deployFrontend) {
  $imageUpdaterEntries.Add(@"
        - alias: $DockerImageSlug-web
          imageName: $DockerHubUsername/$DockerImageSlug-web
          commonUpdateSettings:
            updateStrategy: "semver"
"@)
}

$imageUpdaterContent = @"
apiVersion: argocd-image-updater.argoproj.io/v1alpha1
kind: ImageUpdater
metadata:
  name: $DockerImageSlug
  namespace: argocd
spec:
  namespace: argocd
  writeBackConfig:
    method: git
    gitConfig:
      branch: $gitBranch
  applicationRefs:
    - namePattern: $DockerImageSlug
      images:
$($imageUpdaterEntries -join "`n")
"@
Set-Content -Path (Join-Path $k8sDir "$DockerImageSlug-image-updater.yaml") -Value $imageUpdaterContent -Encoding utf8

Write-Host "Wrote: k8s/$DockerImageSlug-image-updater.yaml" -ForegroundColor Green

# ── Secret creation commands (never applied automatically, never committed) ───
if ($secretCommandLines.Count -gt 0) {
  Write-Host ""
  Write-Host "=== Secrets this deployment needs ===" -ForegroundColor Yellow
  Write-Host "These never go into k8s/ or Git. Fill in real values and run this once against the" -ForegroundColor Yellow
  Write-Host "target cluster (re-run with --dry-run=client -o yaml | kubectl apply -f - to update):" -ForegroundColor Yellow
  Write-Host ""
  Write-Host "kubectl create secret generic $secretName \" -ForegroundColor Cyan
  Write-Host "  --namespace=$Namespace \" -ForegroundColor Cyan
  for ($i = 0; $i -lt $secretCommandLines.Count; $i++) {
    $suffix = if ($i -lt $secretCommandLines.Count - 1) { " \" } else { "" }
    Write-Host "$($secretCommandLines[$i])$suffix" -ForegroundColor Cyan
  }
}

# ── Summary ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host ""
Write-Host "Generated (this project's own repo, not pushed anywhere):" -ForegroundColor Green
Write-Host "  k8s/$DockerImageSlug/                      — copy this whole directory into your ArgoCD repo"
Write-Host "  k8s/$DockerImageSlug-application.yaml       — the Application resource"
Write-Host "  k8s/$DockerImageSlug-image-updater.yaml     — the ImageUpdater resource (auto image bump via semver)"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Copy k8s/$DockerImageSlug/ into your ArgoCD repo (e.g. alongside its existing apps), commit, and push to $gitBranch."
if ($secretCommandLines.Count -gt 0) {
  Write-Host "  2. Fill in and run the kubectl create secret command printed above, against the target cluster."
  Write-Host "  3. kubectl apply -f k8s/$DockerImageSlug-application.yaml -f k8s/$DockerImageSlug-image-updater.yaml"
}
else {
  Write-Host "  2. kubectl apply -f k8s/$DockerImageSlug-application.yaml -f k8s/$DockerImageSlug-image-updater.yaml"
}
Write-Host "  ArgoCD + Argo Rollouts will then sync and canary-roll this out: 20% -> 50% -> 80% -> 100%."
Write-Host "  Make sure Argo Rollouts Controller, NGINX Ingress Controller, and (for External hosts) cert-manager are installed in the cluster."

