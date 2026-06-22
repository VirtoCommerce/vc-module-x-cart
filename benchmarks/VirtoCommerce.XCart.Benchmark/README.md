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
cd benchmarks/VirtoCommerce.XCart.Benchmark

# validate first — compiles + executes each case once, no measurement (Job.Dry)
dotnet run -c Release -- --filter "*" --job Dry

# fast-but-real job — bounded warmup + measurement iterations (Job.ShortRun). Prefer over the
# default job for quick reads
dotnet run -c Release -- --filter "*RecalculateAsync*" --job Short

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

The benchmark **logic** ([Benchmark] methods, [Params], fixtures, the DI host, the module-agnostic
seam, and entry-point plumbing) lives in the `VirtoCommerce.XCart.Benchmark.Core` **library** as
abstract `*BenchmarksBase` classes (so other modules can reference and run the same benchmarks under
their own setup). This project is a thin runner exe over it. The concrete subclasses BenchmarkDotNet
discovers are **source-generated** into this runner: a single `[assembly: BenchmarkSetup(typeof(...))]`
in `Program.cs` tells the generator (shipped in the Core package's `analyzers/` folder) which
`ICartBenchmarkSetup` to bake, and it emits one concrete `*Benchmarks` subclass per Core base.

Because the concrete subclasses live in *this* exe whose `.csproj` filename matches its assembly name,
the run uses BenchmarkDotNet's **stock out-of-process toolchain** — no custom toolchain, no in-process
mode, no process-global state. BDN rebuilds this runner's own project for each child process, which
re-runs the generator, so the baked setup is active there too.

Job selection is BenchmarkDotNet's native `--job Dry|Short|Default` (there are no custom
`--smoke`/`--short` aliases): `--job Dry` to validate, `--job Short` for a fast real measurement,
`--job Default` (or omitting `--job`) for the full measured job. Everything is the stock toolchain.

## Comparing a consuming module against upstream (module-agnostic engine)

Because the benchmark logic lives in the Core library, a consuming module (e.g. XOrder) references
the Core package, implements `ICartBenchmarkSetup.ConfigureServices` to contribute its own
registrations (subclassed models via `AbstractTypeFactory`, a heavier aggregate, overridden command
handlers, extra recalculate middleware), and declares it once via `[assembly: BenchmarkSetup]`. The
generator (shipped in the Core package) emits the same benchmark definitions into the consumer's runner,
so the **same** operations run against the consumer's graph. Both sides run out-of-process on the stock
toolchain — run each into separate `--artifacts` and diff the `Allocated` column (deterministic; `Mean`
from a short run is noise):

A consumer whose real workload is a richer cart (a parent→child line-item hierarchy, not flat SKUs) can
also override `ICartBenchmarkSetup.CreateCart(lineItemCount, shape)` to feed that graph into the
loaded-cart benchmark paths (mutation / recalculate / validate / checklist), so its recalc pipeline and
validators do real per-item work instead of early-returning on Core's generic shape. The default returns
`null` (Core's `CartBenchmarkFixtures.CreateCart` shape). **Id contract**: a consumer cart MUST still
expose selected line items at ids `li-0..li-{lineItemCount-1}` / products `product-0..`, because the
shared mutation fixtures target `li-0` by id — a graph that omits them makes those benchmarks silently
early-return. Only the loaded-cart path is affected; the add path's products still come from the host mock.

```bash
# upstream baseline (this runner)
dotnet run -c Release -- --filter "*ChangeCartItemQuantity*" --job Short --artifacts ./upstream
# consumer (from its own runner, which declares [assembly: BenchmarkSetup(typeof(ConsumerSetup))])
dotnet run -c Release -- --filter "*ChangeCartItemQuantity*" --job Short --artifacts ./consumer
```

**Validity rule — compare full operations, not isolated reimplemented methods.** The seam is
DI-resolved: a consumer's `ConfigureServices` can override the aggregate, the model types, **and the
command handlers** (`OverrideCommandType` / `UseCommandType().WithCommandHandler()`), so a full
operation measured here genuinely routes through the consumer's overrides — the overhead signal is real.
The remaining caveat is *semantic*: isolating a method the consumer fully reimplements with different
behavior (e.g. `RecalculateAsync`, where an override may drop promotions or change the totals passes)
compares two different operations and yields apples-to-oranges deltas. Prefer full mutations
(`ChangeCartItemQuantity`, `RemoveCartItem`, …), whose sign and magnitude track the cart shape and the
recalc share of the operation.

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
   via `/p:BaselineSrc=<path>` (a `ProjectReference` swap, so the full transitive package graph still
   restores — a bare DLL reference would not). The `before` job is the baseline, so an `Alloc Ratio`
   of `0.85` on an `after` row means the change allocates ~15% less. Valid only when the change keeps
   the public API these benchmarks call stable (same namespaces and signatures).

   Do **not** add `--job <preset>` here — `--baseline-src` pins `Job.Default` for both the before and
   after jobs (only the source differs between them), so a CLI `--job` would just append a third,
   unpaired job. `Allocated` / `Alloc Ratio` are the deterministic signal; for a stricter `Mean`
   comparison (symmetric invocation counts) add `--apples --iterationCount N`.

Allocations catch garbage/GC regressions; the time `Ratio` (in a controlled run) catches pure-CPU
regressions that allocate nothing — read both.

## TODO — extract the engine to a module-agnostic `VirtoCommerce.Xapi.Benchmark.Core`

The reusable plumbing — `BenchmarkProgram`, `BenchmarkSetupAttribute`, the source generator
(`BenchmarkSubclassGenerator`), and the host/base scaffolding — is **generic** but currently lives in
`VirtoCommerce.XCart.Benchmark.Core` under the `VirtoCommerce.XCart.Benchmark` namespace, and the
generator is hardcoded to `CartBenchmarkBase` / `ICartBenchmarkSetup`. Consequences:

- A non-cart consumer (XOrder, a future catalog/customer suite) has to reference **XCart**'s benchmark
  package just to get the generator and entry-point plumbing — semantically wrong (its benchmarks don't
  depend on cart).
- XOrder therefore cannot source-generate its concrete subclasses (the cart generator doesn't recognize
  `CreateOrderFromCartBenchmarksBase`), so it hand-writes the subclass; and XOrder still keeps its own
  inline copy of the `--baseline-src` entry-point logic instead of sharing `BenchmarkProgram`.

Extract the generic pieces into a new module-agnostic package `VirtoCommerce.Xapi.Benchmark.Core`
(+ its `.SourceGen`), generalize the generator to discover any `*BenchmarksBase` with a `CreateSetup()`
seam (any `I*BenchmarkSetup`), and have each module's `*.Benchmark.Core` reference it. That
unblocks, **as one publish-gated batch** (it requires republishing the Benchmark.Core packages and
bumping every consumer's package ref in lockstep — the CLI/plumbing change can't land piecemeal):

- XOrder migrating onto the shared `BenchmarkProgram` (dropping its inline `--baseline-src` copy);
- XOrder source-generating its subclass (dropping the hand-written one);
- a clean home for the now-shared native-`--job` `BenchmarkProgram` so no module's package name leaks
  into another's benchmark suite.
