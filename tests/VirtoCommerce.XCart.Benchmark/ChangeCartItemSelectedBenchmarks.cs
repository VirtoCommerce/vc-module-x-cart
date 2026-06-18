using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>changeCartItemSelected</c> GraphQL mutation
/// (<see cref="ChangeCartItemSelectedCommandHandler.Handle"/>): the mutate-existing-cart path — load
/// (real build + recalc), toggle the first line item's checkout selection, save (recalc again). Only
/// the I/O leaves are mocked; the totals calculator is real. The selection flag feeds the recalc's
/// selected-items totals, so this also exercises the totals path on a changed selection set.
/// Idempotent without [IterationSetup] (fresh cart per call). Flat vs Configured; count axis.
/// </summary>
[MemoryDiagnoser]
public class ChangeCartItemSelectedBenchmarks
{
    private ChangeCartItemSelectedCommandHandler _handler = null!;
    private readonly ChangeCartItemSelectedCommand _command = CartBenchmarkFixtures.CreateChangeCartItemSelectedCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CartBenchmarkFixtures.CreateChangeCartItemSelectedHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> ChangeCartItemSelected() => _handler.Handle(_command, CancellationToken.None);
}
