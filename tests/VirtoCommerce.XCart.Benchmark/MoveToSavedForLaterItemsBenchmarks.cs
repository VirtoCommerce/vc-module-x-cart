using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>moveToSavedForLaterItems</c> GraphQL mutation
/// (<see cref="MoveToSavedForLaterItemsCommandHandler.Handle"/>) over the <b>real</b>
/// <see cref="VirtoCommerce.XCart.Data.Services.SavedForLaterListService"/>: load the source cart
/// (real build + recalc), create a fresh saved-for-later list, copy li-0 into it, remove it from the
/// source, then save both (two real recalculates). Only the DB read/write leaves are mocked.
///
/// Idempotent without [IterationSetup]: a fresh source cart and a fresh saved-for-later list are
/// built per call. Flat vs Configured surfaces the configured-copy path; the cart-size count drives
/// the two recalculates.
/// </summary>
[MemoryDiagnoser]
public class MoveToSavedForLaterItemsBenchmarks
{
    private MoveToSavedForLaterItemsCommandHandler _handler = null!;
    private readonly MoveToSavedForLaterItemsCommand _command = GiftsSavedDynamicBenchmarkFixtures.CreateMoveToSavedForLaterCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = GiftsSavedDynamicBenchmarkFixtures.CreateMoveToSavedForLaterHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregateWithList> MoveToSavedForLaterItems() => _handler.Handle(_command, CancellationToken.None);
}
