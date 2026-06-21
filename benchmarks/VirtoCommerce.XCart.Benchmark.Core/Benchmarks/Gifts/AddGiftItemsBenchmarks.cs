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
/// build + recalc), call <see cref="CartAggregate.AddGiftItemsAsync"/> with an empty available-gift
/// list, save (recalc again).
///
/// <b>Gift path note</b>: the shared marketing evaluator returns an empty
/// <see cref="VirtoCommerce.MarketingModule.Core.Model.Promotions.PromotionResult"/>. The
/// <see cref="ICartAvailMethodsService"/> mock also returns an empty list. The command carries no
/// Ids, so <c>AddGiftItemsAsync</c> short-circuits immediately after the null-check — measuring
/// the <b>empty-gift success path</b> (cost of a promotion miss: two full recalculates plus the
/// gift-avail service call). Idempotent without [IterationSetup]; flat vs configured surfaces
/// configured-product recalc regressions; count surfaces super-linear growth.
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
