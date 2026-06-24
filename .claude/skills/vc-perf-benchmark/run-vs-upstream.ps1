#!/usr/bin/env pwsh
# run-vs-upstream.ps1 — "client override vs stock": run the SAME benchmark on THIS module's stock runner
# and on a CLIENT PROJECT's override runner, and emit the overhead verdict via compare-reports.cs.
# PowerShell mirror of run-vs-upstream.sh. Answers "how much overhead does a client project's override add
# over this module's stock path?"
#
# Uses --match method: the two runners' namespaces + class names differ by design, so only the operation +
# workload params coincide. compare-reports.cs reports THIS module's stock side as baseline and the client
# side as current, so an alloc/time ratio > 1 is the client override's overhead.
#
# Validity: compare FULL operations, not isolated overridden methods. Filter to full mutations / commands.
#
# SCOPE: prefer -Filter (one operation) or -Categories (one area). Do NOT run the full suite ('*') in the
# optimization loop — it is ~13h measured. Only `-Job default` lets the TIME axis gate.
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)][ValidateSet('cart', 'order')][string]$Domain,
    [Parameter(Mandatory)][string]$ClientDir,
    [string]$Filter = '*',
    [string]$Categories = '',
    [ValidateSet('dry', 'short', 'default')][string]$Job = 'dry',
    [int]$AllocThreshold = -1,
    [int]$TimeThreshold = -1
)

$ErrorActionPreference = 'Stop'
# A non-zero exit from compare-reports.cs (1 = overhead exceeds threshold) is a valid verdict — never throw.
$PSNativeCommandUseErrorActionPreference = $false
$ScriptDir = $PSScriptRoot

$Repo = (git -C $ScriptDir rev-parse --show-toplevel).Trim()
$StockDir = switch ($Domain) {
    'cart'  { Join-Path $Repo 'benchmarks/VirtoCommerce.XCart.Benchmark' }
    'order' { Join-Path $Repo 'benchmarks/VirtoCommerce.XOrder.Benchmark' }
}

if (-not (Test-Path -PathType Container $ClientDir)) {
    Write-Error "Client runner dir not found: $ClientDir (the override side)."
    exit 2
}
if (-not (Test-Path -PathType Container $StockDir)) {
    Write-Error "This module's stock runner not found: $StockDir"
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

# Each side is the run's results DIRECTORY, not a single file: BenchmarkDotNet writes one
# *-report-full-compressed.json per benchmark class, so a multi-class scope (-Categories, or a broad
# -Filter) emits several. compare-reports.cs reads the whole directory and merges them. The stock and
# client runners are distinct dirs, so their results never collide.
$StockResults = Join-Path $StockDir 'BenchmarkDotNet.Artifacts/results'
$ClientResults = Join-Path $ClientDir 'BenchmarkDotNet.Artifacts/results'

function Invoke-Runner($Dir, $Label) {
    Write-Host "[vs-stock] running $Label ($Dir)..." -ForegroundColor Cyan
    Push-Location $Dir
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

Write-Host "[vs-stock] domain=$Domain job=$Job filter='$Filter' categories='$Categories'" -ForegroundColor Cyan
Invoke-Runner $StockDir 'stock (baseline)'
Invoke-Runner $ClientDir 'client override (current)'

dotnet run "$ScriptDir/compare-reports.cs" -- $StockResults $ClientResults --match method --job-kind $JobKind @CompareExtra
exit $LASTEXITCODE
