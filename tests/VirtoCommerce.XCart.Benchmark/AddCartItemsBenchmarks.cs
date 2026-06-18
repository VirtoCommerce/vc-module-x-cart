using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCartItems</c> GraphQL mutation
/// (<see cref="AddCartItemsCommandHandler.Handle"/>) — the operation a developer ships. The whole
/// real graph runs (handler → real <c>CartAggregateRepository</c> → add dispatch → real
/// <c>RecalculateAsync</c> with the real totals calculator); only the I/O leaves are mocked, and
/// the DB write is a no-op so the save still recalculates.
///
/// Flat-SKU only here (Tier 1). The item count is the <b>bulk</b> dimension: 1 = single add,
/// 5/20/100 = bulk — it drives the per-item dispatch loop, the product/config batch dedup, and the
/// recalculate over the growing cart. 100 is the superlinearity canary. Configured / mixed shapes
/// are Tier 2 (separate change).
///
/// No method-level baseline: this is a single operation, not an A/B. Compare across the count rows
/// (scale) and before/after a change (see README).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(BenchmarkCategories.Tier1)]
public class AddCartItemsBenchmarks
{
    private AddCartItemsCommandHandler _handler = null!;
    private AddCartItemsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _handler = CartBenchmarkFixtures.CreateAddCartItemsHandler();
        _command = CartBenchmarkFixtures.CreateAddCartItemsCommand(ItemCount);
    }

    // Each invocation creates its own fresh cart (the search returns none → create-new path), so
    // adds never accumulate across invocations — the benchmark is idempotent without [IterationSetup].
    [Benchmark]
    public Task<CartAggregate> AddCartItems() => _handler.Handle(_command, CancellationToken.None);
}
