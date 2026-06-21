using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>rejectGiftCartItems</c> GraphQL mutation
/// (<see cref="RejectGiftCartItemsCommandHandler.Handle"/>): the mutate-existing-cart path — load
/// (real build + recalc), call <see cref="CartAggregate.RejectCartItems"/> over a cart with no
/// gift items (empty <c>GiftItems</c> sequence), save (recalc again).
///
/// <b>Gift path note</b>: the fixture cart carries plain line items only (<c>IsGift == false</c>).
/// <c>RejectCartItems</c> with an empty Ids list short-circuits before the scan. The measured cost
/// is the load+recalc overhead common to all mutation handlers. Idempotent without [IterationSetup];
/// flat vs configured and cart count axes are the same as other mutation benchmarks.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Gifts)]
public abstract class RejectGiftCartItemsBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RejectGiftCartItemsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = GiftsSavedDynamicBenchmarkFixtures.CreateRejectGiftCartItemsCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RejectGiftCartItems() => _mediator.Send(_command);
}
