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
dotnet run -c Release -- --filter "*" --smoke

# fast-but-real job — bounded 3 warmup + 3 measurement iterations (Job.ShortRun). Prefer over the
# default job for quick reads and for in-process runs, where the default job's adaptive iteration
# count may not converge on heavier cases
dotnet run -c Release -- --filter "*RecalculateAsync*" --short

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

### Layout and toolchain

The benchmark classes, fixtures, seam, and entry-point plumbing live in the
`VirtoCommerce.XCart.Benchmark.Core` **library** (so other modules can reference and run the same
benchmarks under their own setup); this project is a thin runner exe over it. Because the benchmarks
live in a referenced library, the run uses a custom toolchain (`BenchmarkCoreToolchain`) that resolves
the library's `.csproj` deterministically — BenchmarkDotNet's default current-directory search can't
find it from the runner's folder.

Two consequences for the command line:

- **Use `--smoke`, not `--job Dry`, to validate; use `--short`, not `--job Short`, for a fast real
  measurement.** Both run on the custom toolchain. A BenchmarkDotNet `--job <preset>` CLI argument
  *adds* a job that uses the **default** toolchain, which cannot locate the library project and fails
  to generate.
- The default run (no flag), `--smoke`, `--short`, and `--baseline-src` use the custom out-of-process
  toolchain. **`--in-process`** switches to `InProcessEmitToolchain` (runs the benchmarks in this
  process, no per-case build) — required when a consuming module runs the benchmarks from a NuGet
  package (no `.csproj` on disk) and for producing a baseline on the *same* toolchain a consumer is
  locked into. `--in-process` cannot be combined with `--baseline-src`.

## Comparing a consuming module against upstream (module-agnostic toolchain)

Because the benchmark classes live in the Core library, a consuming module (XOrder, LEO, …) can
reference Core, install its own `ICartModuleBenchmarkSetup` via `BenchmarkEnvironment.Current`, and run
the **same** benchmark definitions against its overridden aggregate/types. Run both sides **in-process,
on the same toolchain**, into separate `--artifacts`, and diff the `Allocated` column (deterministic;
`Mean` from a short run is noise):

```bash
# upstream baseline
dotnet run -c Release -- --filter "*ChangeCartItemQuantity*" --short --in-process --artifacts ./upstream
# consumer (from its own runner, which sets BenchmarkEnvironment.Current)
dotnet run -c Release -- --filter "*ChangeCartItemQuantity*" --short --artifacts ./consumer
```

**Validity rule — compare full operations, not isolated overridden methods.** The seam swaps the
**aggregate and model types**, not the command handlers. A head-to-head is meaningful only when the
consumer's override is an alternative implementation of the **same** operation. Isolating a method the
consumer fully reimplements (e.g. `RecalculateAsync`, where an override may drop promotions or change
the totals passes) compares two different operations and yields apples-to-oranges deltas. Full
mutations (`ChangeCartItemQuantity`, `RemoveCartItem`, …) give a realistic overhead signal whose sign
and magnitude track the cart shape and the recalc share of the operation.

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

2. **Single-process side-by-side (`--baseline-src`).** To get `Ratio` / `Alloc Ratio` columns that
   control for machine variance in **one** run, point the benchmark at a baseline checkout of the
   source. BenchmarkDotNet then builds a `before` job (the baseline) from that source and an `after`
   job from the current source, side-by-side:
   ```bash
   # 1. materialize the "before" source (a worktree on the baseline revision — no working-tree switch)
   git worktree add /tmp/xcart-before <baseline-ref>

   # 2. compare — runs from the benchmark's own directory (BDN resolves its exe relative to CWD)
   dotnet run -c Release -- --filter "*RecalculateAsync*" --baseline-src /tmp/xcart-before/src

   # 3. clean up
   git worktree remove /tmp/xcart-before
   ```
   `--baseline-src <path>` is opt-in and additive — without it the run is unchanged. The path is the
   `src` root of the baseline checkout; the `before` job rebuilds `XCart.Core`/`XCart.Data` from it
   via `/p:XCartSrc=<path>` (a `ProjectReference` swap, so the full transitive package graph still
   restores — a bare DLL reference would not). The `before` job is the baseline, so an `Alloc Ratio`
   of `0.85` on an `after` row means the change allocates ~15% less. Valid only when the change keeps
   the public API these benchmarks call stable (same namespaces and signatures).

   Do **not** add `--job <preset>` here — besides appending an extra job rather than reconfiguring the
   before/after pair, a CLI `--job` uses the default toolchain that can't resolve the library project
   (see "Layout and toolchain"). `Allocated` / `Alloc Ratio` are the deterministic signal; for a
   stricter `Mean` comparison (symmetric invocation counts) add `--apples --iterationCount N`, or
   `--smoke` for a fast before/after correctness pass.

Allocations catch garbage/GC regressions; the time `Ratio` (in a controlled run) catches pure-CPU
regressions that allocate nothing — read both.
