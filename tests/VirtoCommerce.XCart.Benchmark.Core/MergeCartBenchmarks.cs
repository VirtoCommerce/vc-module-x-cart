using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>mergeCart</c> GraphQL mutation
/// (<see cref="MergeCartCommandHandler.Handle"/>): the merge path. Measured compute = load primary
/// cart (real build + recalc), load second cart, call <c>MergeWithCartAsync</c> (copies second
/// cart's items into the primary aggregate), save (recalc again over the merged item set).
/// Only I/O leaves are mocked; the totals calculator is real.
///
/// <b>Second-cart wiring</b>: The handler calls <c>GetCartById(request.SecondCartId, ...)</c> via
/// the same repository. A local harness (in <see cref="CartStateBenchmarkFixtures"/>) dispatches
/// <c>GetAsync</c> by cart ID — returning a distinct <c>"second-cart"</c> instance so the handler's
/// <c>secondCart.Id != cartAggr.Id</c> guard passes and the actual merge runs. The shared
/// <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> would return Id="benchmark-cart" for
/// every call, skipping the merge body and measuring only two loads — that was fixed in the local
/// harness. <c>DeleteAfterMerge=false</c> keeps the benchmark focused on merge compute (no delete
/// path).
///
/// Two axes: <b>Shape</b> (Flat vs Configured — the post-merge recalc walks the doubled item set,
/// and configured items carry configuration-item graphs) and <b>LineItemCount</b> (the merged cart
/// doubles the items the recalc sees; 100+100 surfaces super-linear growth).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns fresh carts per call.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.CartState)]
public abstract class MergeCartBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private MergeCartCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CartStateBenchmarkFixtures.CreateMergeCartCommand();
    }

    [Benchmark]
    public Task<CartAggregate> MergeCart() => _mediator.Send(_command);
}
