using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addWishlistItem</c> GraphQL mutation
/// (<see cref="AddWishlistItemCommandHandler.Handle"/>). Measured compute = load existing wishlist
/// via <c>GetCartByIdAsync</c>, check for an active product configuration (none — flat branch),
/// call <c>AddItemsAsync</c> (real add-validation + recalc), then save.
///
/// The item-count axis grows the wishlist the item is added INTO, surfacing how the recalculate
/// cost scales with cart size. The added item is always the same product (
/// <see cref="WishlistBenchmarkFixtures.WishlistProductId"/>); the harness returns a fresh cart
/// per call so items never accumulate.
/// </summary>
[MemoryDiagnoser]
public class AddWishlistItemBenchmarks
{
    private AddWishlistItemCommandHandler _handler = null!;
    private readonly AddWishlistItemCommand _command = WishlistBenchmarkFixtures.CreateAddWishlistItemCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateAddWishlistItemHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> AddWishlistItem() => _handler.Handle(_command, CancellationToken.None);
}
