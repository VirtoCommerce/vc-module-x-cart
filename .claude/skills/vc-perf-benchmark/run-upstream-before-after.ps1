#!/usr/bin/env pwsh
# run-upstream-before-after.ps1 — "dependency before/after": did an UPSTREAM change regress? Runs the
# upstream module's own benchmark runner at two revisions of the upstream source and emits the verdict via
# compare-reports.cs. PowerShell mirror of run-upstream-before-after.sh. This is a property of the upstream
# module, measured on the upstream runner — the client/consuming module is not involved.
#
# Same runner both sides (same namespace) -> --match fullname.
#
# Note: the upstream runner's built-in `--baseline-src` flag does before/after in ONE BenchmarkDotNet run
# (Ratio / Alloc-Ratio columns) and is lighter when you just want to eyeball the table — see the runner's
# README. This helper instead produces TWO clean single-job JSON reports so the structured, thresholded
# compare-reports.cs verdict applies.
#
# SCOPE: prefer -Filter (one operation) or -Categories (one area). Do NOT run the full suite ('*') in the
# loop — it is ~13h measured. Only `-Job default` lets the TIME axis gate.
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)][ValidateSet('cart', 'order')][string]$Domain,
    [Parameter(Mandatory, Position = 1)][string]$BaselineRef,
    [string]$Filter = '*',
    [string]$Categories = '',
    [ValidateSet('dry', 'short', 'default')][string]$Job = 'dry',
    [string]$UpstreamRoot = '',
    [int]$AllocThreshold = -1,
    [int]$TimeThreshold = -1
)

$ErrorActionPreference = 'Stop'
# A non-zero exit from compare-reports.cs (1 = regression) is a valid verdict, not a failure — never throw.
$PSNativeCommandUseErrorActionPreference = $false
$ScriptDir = $PSScriptRoot

$Repo = (git -C $ScriptDir rev-parse --show-toplevel).Trim()
if (-not $UpstreamRoot) { $UpstreamRoot = (Resolve-Path (Join-Path $Repo '../../..')).Path }

switch ($Domain) {
    'cart'  { $UpRepo = Join-Path $UpstreamRoot 'vc-module-x-cart';  $RunnerDir = 'benchmarks/VirtoCommerce.XCart.Benchmark' }
    'order' { $UpRepo = Join-Path $UpstreamRoot 'vc-module-x-order'; $RunnerDir = 'benchmarks/VirtoCommerce.XOrder.Benchmark' }
}

if (-not (Test-Path (Join-Path $UpRepo '.git'))) {
    Write-Error "Upstream repo not found: $UpRepo (set -UpstreamRoot)."
    exit 2
}
git -C $UpRepo cat-file -e "$BaselineRef^{commit}" 2>$null
if (-not $?) {
    Write-Error "Baseline ref '$BaselineRef' is not a valid commit in $UpRepo."
    exit 2
}

$JobFlags = @(); $JobKind = 'measured'
switch ($Job) {
    'dry'     { $JobFlags = @('--job', 'Dry');   $JobKind = 'dry' }
    'short'   { $JobFlags = @('--job', 'Short'); $JobKind = 'short' }
    'default' { $JobFlags = @();                 $JobKind = 'measured' }
}

$CatFlags = @()
if ($Categories) { $CatFlags = @('--anyCategories') + ($Categories -split ',') }

$CompareExtra = @()
if ($AllocThreshold -ge 0) { $CompareExtra += @('--alloc-threshold', "$AllocThreshold") }
if ($TimeThreshold -ge 0)  { $CompareExtra += @('--time-threshold', "$TimeThreshold") }

$Worktree = Join-Path ([System.IO.Path]::GetTempPath()) ("upstream-baseline-" + [System.IO.Path]::GetRandomFileName())
# Each side is the run's results DIRECTORY, not a single file: BenchmarkDotNet writes one
# *-report-full-compressed.json per benchmark class, so a multi-class scope (-Categories, or a broad
# -Filter) emits several. compare-reports.cs reads the whole directory and merges them. The two runs use
# distinct tree roots, so their results dirs never collide; compare runs before the worktree is removed.
$BaseResults = Join-Path (Join-Path $Worktree $RunnerDir) 'BenchmarkDotNet.Artifacts/results'
$CurResults = Join-Path (Join-Path $UpRepo $RunnerDir) 'BenchmarkDotNet.Artifacts/results'

function Invoke-Runner($Root, $Label) {
    $dir = Join-Path $Root $RunnerDir
    Write-Host "[upstream-before-after] running $Label ($dir)..." -ForegroundColor Cyan
    Push-Location $dir
    try {
        if (Test-Path BenchmarkDotNet.Artifacts) { Remove-Item -Recurse -Force BenchmarkDotNet.Artifacts }
        dotnet run -c Release -- @JobFlags --filter $Filter @CatFlags --exporters json
        # $PSNativeCommandUseErrorActionPreference is off (so compare-reports.cs can exit 1 without
        # throwing), so a failed/partial benchmark run won't throw on its own — check it explicitly,
        # else compare-reports.cs would run on missing results and emit a misleading verdict.
        if ($LASTEXITCODE -ne 0) { throw "Benchmark run failed ($Label): dotnet run exited $LASTEXITCODE" }
    } finally {
        Pop-Location
    }
}

$rc = 2
try {
    Write-Host "[upstream-before-after] upstream=$Domain baseline=$BaselineRef job=$Job filter='$Filter' categories='$Categories'" -ForegroundColor Cyan
    git -C $UpRepo worktree add --detach $Worktree $BaselineRef | Out-Host
    Invoke-Runner $Worktree "upstream baseline ($BaselineRef)"
    Invoke-Runner $UpRepo 'upstream current'
    dotnet run "$ScriptDir/compare-reports.cs" -- $BaseResults $CurResults --match fullname --job-kind $JobKind @CompareExtra
    $rc = $LASTEXITCODE
} finally {
    git -C $UpRepo worktree remove --force $Worktree 2>$null | Out-Null
}
exit $rc
