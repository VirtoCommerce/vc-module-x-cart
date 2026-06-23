#!/usr/bin/env pwsh
# run-own-before-after.ps1 — "own before/after": compare two revisions of THIS module's own source on the
# same benchmark runner, and emit the two-axis verdict via compare-reports.cs. PowerShell mirror of
# run-own-before-after.sh — same behaviour, same exit codes. Answers "did my change regress this module's
# cart/order paths?"
#
# WHY A WORKTREE: comparing two revisions means building the runner from each. NEVER `git checkout`/`stash`
# the working tree — the operator works concurrently in this repo. A detached worktree at the baseline ref
# is the only safe mechanism. Packages restore from the normal feeds, so the worktree needs no setup.
#
# Propagates compare-reports.cs's exit code: 0 = no regression, 1 = regression, 2 = usage/parse error.
# Exit 1 is a VALID verdict, not a script failure.
#
# SCOPE: prefer -Filter (one operation) or -Categories (one area). Do NOT run the full suite ('*') in the
# optimization loop — it is ~13h measured. Only `-Job default` lets the TIME axis gate; dry/short keep it
# advisory (alloc always gates).
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)][string]$BaselineRef,
    [Parameter(Mandatory, Position = 1)][ValidateSet('cart', 'order')][string]$Runner,
    [string]$Filter = '*',
    [string]$Categories = '',
    [ValidateSet('dry', 'short', 'default')][string]$Job = 'dry',
    [int]$AllocThreshold = -1,
    [int]$TimeThreshold = -1
)

$ErrorActionPreference = 'Stop'
# A non-zero exit from compare-reports.cs (1 = regression) is a valid verdict, not a failure — never throw.
$PSNativeCommandUseErrorActionPreference = $false
$ScriptDir = $PSScriptRoot

$RunnerDir = switch ($Runner) {
    'cart'  { 'benchmarks/VirtoCommerce.XCart.Benchmark' }
    'order' { 'benchmarks/VirtoCommerce.XOrder.Benchmark' }
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

$Repo = (git -C $ScriptDir rev-parse --show-toplevel).Trim()

git -C $Repo cat-file -e "$BaselineRef^{commit}" 2>$null
if (-not $?) {
    Write-Error "Baseline ref '$BaselineRef' is not a valid commit in $Repo."
    exit 2
}

$Worktree = Join-Path ([System.IO.Path]::GetTempPath()) ("module-baseline-" + [System.IO.Path]::GetRandomFileName())
$BaseJson = [System.IO.Path]::GetTempFileName()
$CurJson = [System.IO.Path]::GetTempFileName()

function Invoke-Runner($Root, $OutJson, $Label) {
    $dir = Join-Path $Root $RunnerDir
    Write-Host "[own-before-after] running $Label ($dir)..." -ForegroundColor Cyan
    Push-Location $dir
    try {
        if (Test-Path BenchmarkDotNet.Artifacts) { Remove-Item -Recurse -Force BenchmarkDotNet.Artifacts }
        dotnet run -c Release -- @JobFlags --filter $Filter @CatFlags --exporters json
        $report = Get-ChildItem 'BenchmarkDotNet.Artifacts/results/*-report-full-compressed.json' | Select-Object -First 1
        if (-not $report) { throw "No BenchmarkDotNet report produced in $dir" }
        Copy-Item $report.FullName $OutJson -Force
    } finally {
        Pop-Location
    }
}

$rc = 2
try {
    Write-Host "[own-before-after] baseline=$BaselineRef runner=$Runner job=$Job filter='$Filter' categories='$Categories'" -ForegroundColor Cyan
    git -C $Repo worktree add --detach $Worktree $BaselineRef | Out-Host
    Invoke-Runner $Worktree $BaseJson "baseline ($BaselineRef)"
    Invoke-Runner $Repo $CurJson 'current (working tree)'
    dotnet run "$ScriptDir/compare-reports.cs" -- $BaseJson $CurJson --job-kind $JobKind @CompareExtra
    $rc = $LASTEXITCODE
} finally {
    git -C $Repo worktree remove --force $Worktree 2>$null | Out-Null
}
exit $rc
