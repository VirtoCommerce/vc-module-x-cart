using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addGiftItems</c> GraphQL mutation
/// (<see cref="AddGiftItemsCommandHandler.Handle"/>): the mutate-existing-cart path — load (real
/// build + recalc), call <see cref="CartAggregate.AddGiftItemsAsync"/> with one requested gift id over
/// an empty available-gift list, save (recalc again).
///
/// <b>Gift path note</b>: the handler sources the available gifts from
/// <c>ICartAvailMethodsService.GetAvailableGiftsAsync</c> alone, never from the promotion evaluator,
/// and that service is a <c>DefaultValue.Mock</c> mock — so the available-gift list is empty. The
/// command requests one gift id, so <c>AddGiftItemsAsync</c> runs its id loop, finds the requested gift
/// absent from the empty list, and skips it — measuring the <b>gift-requested-but-unavailable path</b>
/// (two full recalculates plus the gift-avail resolution, with no line item actually added).
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Gifts)]
public abstract class AddGiftItemsBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddGiftItemsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = GiftsSavedDynamicBenchmarkFixtures.CreateAddGiftItemsCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddGiftItems() => _mediator.Send(_command);
}
