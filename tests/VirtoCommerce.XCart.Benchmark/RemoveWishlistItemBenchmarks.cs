using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeWishlistItem</c> GraphQL mutation
/// (<see cref="RemoveWishlistItemCommandHandler.Handle"/>). Measured compute = load wishlist via
/// <c>GetCartByIdAsync</c>, remove <c>li-0</c> by line item id (O(n) linear scan over
/// <c>Items</c>), recalculate, then save.
///
/// The item-count axis grows the wishlist and exercises the linear-scan cost for the remove lookup
/// and the recalculate walk. The harness returns a fresh cart per call so the item is always
/// present.
/// </summary>
[MemoryDiagnoser]
public class RemoveWishlistItemBenchmarks
{
    private RemoveWishlistItemCommandHandler _handler = null!;
    private readonly RemoveWishlistItemCommand _command = WishlistBenchmarkFixtures.CreateRemoveWishlistItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateRemoveWishlistItemHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> RemoveWishlistItem() => _handler.Handle(_command, CancellationToken.None);
}
