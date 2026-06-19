using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>refreshCart</c> GraphQL mutation
/// (<see cref="RefreshCartCommandHandler.Handle"/>): the reload+recalc path. Measured compute =
/// load cart (real <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/> build +
/// recalc), then save immediately (recalc again). No aggregate-level mutation occurs — refresh is
/// a forced double-recalc. Only I/O leaves are mocked; the totals calculator is real.
///
/// This is the canonical baseline for the load+recalc cost of a given cart shape and size: any
/// handler that loads a cart pays at least this much. The Configured shape is heavier because each
/// load triggers variation-product re-pricing via <c>UpdateConfiguredLineItemPrice</c>.
///
/// Two axes: <b>Shape</b> (Flat vs Configured — configured has heavier per-load recalc) and
/// <b>LineItemCount</b> (100 shows whether recalc is O(N) or super-linear).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart per call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.CartState)]
public class RefreshCartBenchmarks
{
    private RefreshCartCommandHandler _handler = null!;
    private readonly RefreshCartCommand _command = CartStateBenchmarkFixtures.CreateRefreshCartCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartStateBenchmarkFixtures.CreateRefreshCartHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> RefreshCart() => _handler.Handle(_command, CancellationToken.None);
}
