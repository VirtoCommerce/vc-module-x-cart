using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>moveFromSavedForLaterItems</c> GraphQL mutation
/// (<see cref="MoveFromSavedForLaterItemsCommandHandler.Handle"/>) over the <b>real</b>
/// <see cref="VirtoCommerce.XCart.Data.Services.SavedForLaterListService"/>: load the destination
/// cart (real build + recalc), find the seeded saved-for-later list, copy li-0 from it into the cart,
/// remove it from the list, then save both (two real recalculates). Only the DB read/write leaves are
/// mocked.
///
/// Idempotent without [IterationSetup]: a fresh cart and a fresh seeded saved-for-later list are built
/// per call. Flat vs Configured surfaces the configured-copy path; the cart-size count drives the two
/// recalculates.
/// </summary>
[MemoryDiagnoser]
public class MoveFromSavedForLaterItemsBenchmarks
{
    private MoveFromSavedForLaterItemsCommandHandler _handler = null!;
    private readonly MoveFromSavedForLaterItemsCommand _command = GiftsSavedDynamicBenchmarkFixtures.CreateMoveFromSavedForLaterCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = GiftsSavedDynamicBenchmarkFixtures.CreateMoveFromSavedForLaterHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregateWithList> MoveFromSavedForLaterItems() => _handler.Handle(_command, CancellationToken.None);
}
