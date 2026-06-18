using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>moveWishlistItem</c> GraphQL mutation
/// (<see cref="MoveWishListItemCommandHandler.Handle"/>). Measured compute = TWO cart loads
/// (source and destination), add the item to the destination (<c>AddItemsAsync</c> + recalc),
/// remove from source (linear scan + recalc), then save both.
///
/// This is the most expensive wishlist mutation: it recalculates two aggregates per invocation.
/// The item-count axis grows BOTH the source and destination carts simultaneously (the harness
/// returns the same fixture cart for both ids), so the recalculate cost doubles relative to a
/// single-cart mutation.
/// </summary>
[MemoryDiagnoser]
public class MoveWishlistItemBenchmarks
{
    private MoveWishListItemCommandHandler _handler = null!;
    private readonly MoveWishlistItemCommand _command = WishlistBenchmarkFixtures.CreateMoveWishlistItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateMoveWishlistItemHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> MoveWishlistItem() => _handler.Handle(_command, CancellationToken.None);
}
