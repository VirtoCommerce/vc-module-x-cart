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
/// aggregate), save (recalc again over the now-empty cart).
///
/// The clear path is interesting in the configured shape: the initial load recalculates over the
/// full item graph (N items × M configuration sections), after which <c>ClearAsync</c> removes
/// everything and the post-save recalc runs over an empty cart. The measured time thus includes
/// the configured-shape load overhead even though the mutation itself is O(1).
///
/// Every invocation starts from a FULL cart, so the clear always has the whole item graph to remove.
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
