using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

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
public class AddGiftItemsBenchmarks
{
    private AddGiftItemsCommandHandler _handler = null!;
    private readonly AddGiftItemsCommand _command = GiftsSavedDynamicBenchmarkFixtures.CreateAddGiftItemsCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = GiftsSavedDynamicBenchmarkFixtures.CreateAddGiftItemsHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> AddGiftItems() => _handler.Handle(_command, CancellationToken.None);
}
