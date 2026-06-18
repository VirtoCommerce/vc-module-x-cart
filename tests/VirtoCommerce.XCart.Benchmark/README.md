# VirtoCommerce.XCart Benchmarks

Microbenchmarks for the XCart **XAPI command handlers** and the cart recalculate hot path — the
operations a developer ships (GraphQL mutations) and their real internal compute, with all I/O
mocked at the leaves. They are a tool for **local development and code analysis**: run them while
changing cart code to see the allocation and throughput effect of a change.

The metric to trust is **allocations** (`[MemoryDiagnoser]`): it is deterministic across machines
and runs, so it is meaningful even when comparing numbers taken at different times. Wall-clock
`Mean` is a useful complementary signal but only within a single controlled run on an idle machine
(it varies with CPU, turbo, and load).

## Subjects

The suite covers the cart module's distinct code paths — one benchmark per distinct handler/query
logic (single/bulk/all twins are deduped and varied via the count axis instead). Most run both a
**flat** and a **configured** cart shape, and each over cart sizes `[1, 5, 20, 100]`. Subjects are
grouped into functional **categories** for selective runs (see [Running](#running)):

| Category | Covers |
|---|---|
| `items` | add / change-quantity / change-price / change-comment / change-selected / remove line items |
| `configuration` | add / update / remove configuration items, change configured line item, config-item selected |
| `checkout` | add-or-update shipment / payment / address, initialize payment, remove shipment, clear shipments / payments |
| `cart-state` | change currency, merge, clear, refresh, change PO number, change cart comment, create cart |
| `coupon` | add / remove coupon, validate coupon |
| `queries` | `getCart`, `getPricesSum` |
| `recalculate` | `CartAggregate.RecalculateAsync()` — fires on every cart read and inside every mutation's save |
| `validation` | cart `validateAsync` (the `validationErrors` field) + `validateCoupon` (cross-tagged) |
| `gifts` | add gift items, reject gift items |
| `saved-for-later` | move items to / from the saved-for-later list (real `SavedForLaterListService`) |
| `dynamic-properties` | update cart / cart-item dynamic properties |
| `wishlist` | add / create / clone / rename / change / remove / move / update wishlist + items, create cart from wishlist, get / search |

Harness: command/query-level with the real handler, real `CartAggregateRepository`, real
`DefaultShoppingCartTotalsCalculator`, and only the I/O leaves mocked (`recalculate` is
aggregate-direct). A benchmark may carry more than one category (e.g. `validateCoupon` is both
`coupon` and `validation`).

### What is real vs mocked

Everything that does I/O is mocked at the leaf; everything that is pure compute runs for real.
In particular `IShoppingCartTotalsCalculator` is the **real** `DefaultShoppingCartTotalsCalculator`
— a totals-math regression is exactly what these benchmarks should reveal. The DB write
(`IShoppingCartService.SaveChangesAsync`) is a no-op mock, so `CartAggregateRepository.SaveAsync`
still runs the real `RecalculateAsync` while dropping only the persistence round-trip.

### What these do NOT measure

XAPI/GraphQL request duration, real CPU%, EF query time, and the concurrency/caching behaviour of
the process-cached shared aggregate. These are single-threaded, in-memory measurements.

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
dotnet run -c Release -- --filter "*.AddCartItems"

# a whole functional area, by category (space-separated = OR)
dotnet run -c Release -- --anyCategories checkout
dotnet run -c Release -- --anyCategories coupon wishlist
dotnet run -c Release -- --anyCategories validation   # cart + coupon validation

# discover what exists
dotnet run -c Release -- --list flat                  # all benchmark names
dotnet run -c Release -- --list flat --anyCategories checkout
```

`--filter` matches globs on the full `Namespace.Class.Method`; `--anyCategories` / `--allCategories`
match the `[BenchmarkCategory]` tags (defined in `Categories.cs`). Results are written to
`BenchmarkDotNet.Artifacts/`; read the `*-report-github.md` summary table — the `Categories` column
shows each row's category.

## Comparing before/after a change

A single run's numbers are not a verdict — compare. Two ways:

1. **Two runs, git-switched (simplest).** Run on the current code into one artifacts dir, make the
   change (or `git checkout` the branch), run again into another, and diff the `Allocated` columns:
   ```bash
   dotnet run -c Release -- --filter "*RecalculateAsync*" --artifacts ./before
   #   ...apply the cart-code change...
   dotnet run -c Release -- --filter "*RecalculateAsync*" --artifacts ./after
   diff before/results/*-report-github.md after/results/*-report-github.md
   ```
   Allocations are deterministic, so this is reliable for them across separate runs.

2. **Single-process side-by-side.** To get a `Ratio` column that controls for machine variance
   (and a trustworthy `Mean` delta), build the baseline `XCart.Data` to a folder and reference it
   as a second job — see BenchmarkDotNet's saved-build comparison. This is only valid when the
   change keeps the public API these benchmarks call stable.

Allocations catch garbage/GC regressions; the time `Ratio` (in a controlled run) catches pure-CPU
regressions that allocate nothing — read both.
