using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCartItems</c> GraphQL mutation
/// (<see cref="AddCartItemsCommandHandler.Handle"/>). The whole real graph runs (handler → real
/// <c>CartAggregateRepository</c> → add dispatch → real <c>RecalculateAsync</c> with the real
/// totals calculator); only the I/O leaves are mocked, and the DB write is a no-op so the save
/// still recalculates.
///
/// Two axes:
/// <list type="bullet">
/// <item><b>Shape</b> — <c>Flat</c> exercises the plain per-item add; <c>Configured</c> routes
/// every item through the configured-product dispatch (a distinct, heavier handler branch).</item>
/// <item><b>Item count</b> — the bulk dimension: 1 = single add, 5/20/100 = bulk; drives the
/// per-item dispatch loop, the product/config batch dedup, and the recalculate over the growing
/// cart. 100 surfaces super-linear growth.</item>
/// </list>
/// Read the <c>Allocated</c> column across the rows (and before/after a change); the operations are
/// not alternatives, so there is no in-run baseline.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Items)]
public class AddCartItemsBenchmarks
{
    private AddCartItemsCommandHandler _handler = null!;
    private AddCartItemsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int ItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _handler = CartBenchmarkFixtures.CreateAddCartItemsHandler(Shape);
        _command = CartBenchmarkFixtures.CreateAddCartItemsCommand(ItemCount);
    }

    // Each invocation creates its own fresh cart (the search returns none → create-new path), so
    // adds never accumulate across invocations — no [IterationSetup] reset needed.
    [Benchmark]
    public Task<CartAggregate> AddCartItems() => _handler.Handle(_command, CancellationToken.None);
}
