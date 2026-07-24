using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>clearCart</c> GraphQL mutation
/// (<see cref="ClearCartCommandHandler.Handle"/>): the clear-all-items path. Measured compute =
/// load cart (real build + recalc), call <c>ClearAsync</c> (removes all line items from the
/// aggregate), save (recalc again over the now-empty cart). Only I/O leaves are mocked; the totals
/// calculator is real.
///
/// The clear path is interesting in the configured shape: the initial load recalculates over the
/// full item graph (N items × M configuration sections), after which <c>ClearAsync</c> removes
/// everything and the post-save recalc runs over an empty cart. The measured time thus includes
/// the configured-shape load overhead even though the mutation itself is O(1).
///
/// Two axes: <b>Shape</b> (Flat vs Configured — diverges on the load-side recalc cost, not the
/// clear itself) and <b>LineItemCount</b> (100 surfaces the load-side cost of the pre-clear recalc).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh populated cart per call,
/// so the clear never accumulates (each invocation starts with a full cart).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.CartState)]
public abstract class ClearCartBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ClearCartCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartStateBenchmarkFixtures.CreateClearCartCommand();
    }

    [Benchmark]
    public Task<CartAggregate> ClearCart() => _mediator.Send(_command);
}
