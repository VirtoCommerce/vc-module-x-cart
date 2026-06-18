# VirtoCommerce.XCart Benchmarks

L1 microbenchmarks for the XCart **XAPI command handlers** and the cart hot path — the operations
a developer ships (GraphQL mutations) and their real internal compute, with all I/O mocked at the
leaves.

These are a **regression coverage suite**: the metric that gates is **allocations**
(`[MemoryDiagnoser]`, deterministic across machines). Wall-clock `Mean` is informative but not
portable — do not fail CI on absolute nanoseconds.

## Subjects

| Benchmark | Subject | Harness |
|---|---|---|
| `RecalculateAsyncBenchmarks` | `CartAggregate.RecalculateAsync()` — fires on every cart read and inside every mutation's save | aggregate-direct, **real** `DefaultShoppingCartTotalsCalculator` |
| `AddCartItemsBenchmarks` | `addCartItems` → `AddCartItemsCommandHandler.Handle` (single + bulk via the count axis) | command-level, real handler + real `CartAggregateRepository`, mocked I/O leaves |

### What is real vs mocked

Everything that does I/O is mocked at the leaf; everything that is pure compute runs for real.
In particular `IShoppingCartTotalsCalculator` is the **real** `DefaultShoppingCartTotalsCalculator`
— a totals-math regression is exactly what this suite must catch. The DB write
(`IShoppingCartService.SaveChangesAsync`) is a no-op mock, so `CartAggregateRepository.SaveAsync`
still runs the real `RecalculateAsync` while dropping only the persistence round-trip.

### What L1 does NOT measure

XAPI/GraphQL request duration, real CPU%, EF query time, and the concurrency/caching behaviour of
the process-cached shared aggregate. Those are L2 (local load harness) / L3 (profiling) concerns.

## Prerequisites

- .NET 10 SDK

## Running

```bash
cd tests/VirtoCommerce.XCart.Benchmark

# validate first — compiles + executes each case once, no measurement
dotnet run -c Release -- --filter "*" --job Dry

# all benchmarks
dotnet run -c Release -- --filter "*" --noOverwrite > benchmark.log 2>&1

# one class / one method
dotnet run -c Release -- --filter "*RecalculateAsyncBenchmarks*"
dotnet run -c Release -- --filter "*AddCartItems*"
```

### Tiered gating (cost guard)

The full count × shape matrix is several minutes per run — too slow per-commit. Benchmarks are
tagged by tier so CI can gate the minimal set per-PR and run the full matrix nightly:

```bash
# per-PR gate — hottest paths only
dotnet run -c Release -- --anyCategories Tier1

# nightly — everything
dotnet run -c Release -- --filter "*"
```

## Detecting a regression (before/after)

A single run's numbers are not a verdict — compare. The coverage-suite axis is **before/after a
change**, run side-by-side so environmental variance cancels (BenchmarkDotNet, comparison
strategy 4 — saved-DLL reference):

1. Build the baseline (pre-change) XCart assemblies to a folder:
   `dotnet build ../../src/VirtoCommerce.XCart.Data -c Release -o ./saved-baseline`
2. Make your change.
3. Run with both the saved baseline and current source as two jobs (see BenchmarkDotNet docs on
   referencing a saved DLL), then read the `Ratio` / `Allocated` columns.

Results are written to `BenchmarkDotNet.Artifacts/`. Read the `*-report-github.md` summary.
