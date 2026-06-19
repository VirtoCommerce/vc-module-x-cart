using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changePurchaseOrderNumber</c> GraphQL mutation
/// (<see cref="ChangePurchaseOrderNumberCommandHandler.Handle"/>): the scalar-field mutation path.
/// Measured compute = load cart (real build + recalc), call <c>ChangePurchaseOrderNumber</c>
/// (sets a single string field on the cart), save (recalc again). Only I/O leaves are mocked;
/// the totals calculator is real.
///
/// The PO number change is the cheapest possible mutation — no item graph modification. The
/// measured time is dominated by the two recalc passes (load + save). This makes it a useful
/// reference point for the minimum cost of a cart write round-trip at a given shape and size.
///
/// Two axes: <b>Shape</b> (Flat vs Configured — diverges on the recalc cost, not the scalar
/// mutation) and <b>LineItemCount</b> (100 surfaces recalc cost, not mutation cost).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart per call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.CartState)]
public class ChangePurchaseOrderNumberBenchmarks
{
    private ChangePurchaseOrderNumberCommandHandler _handler = null!;
    private readonly ChangePurchaseOrderNumberCommand _command = CartStateBenchmarkFixtures.CreateChangePurchaseOrderNumberCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartStateBenchmarkFixtures.CreateChangePurchaseOrderNumberHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ChangePurchaseOrderNumber() => _handler.Handle(_command, CancellationToken.None);
}
