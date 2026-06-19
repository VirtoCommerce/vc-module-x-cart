using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>createCartFromWishlist</c> GraphQL mutation
/// (<see cref="CreateCartFromWishlistCommandHandler.Handle"/>). Measured compute = load source
/// wishlist via <c>GetCartByIdAsync</c> (real recalc), create a new empty
/// <see cref="VirtoCommerce.CartModule.Core.Model.ShoppingCart"/> via
/// <c>GetOrCreateCartFromCommandAsync</c> (real recalc of empty cart), copy all non-configured
/// items with <c>AddItemsAsync</c> (add-validation + recalc over the growing copy), then save.
///
/// This is the heaviest create-path mutation: it recalculates the source on load, then the
/// destination once empty and again after each batch of items is added. The item-count axis
/// drives both the source-cart size and the number of items copied.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public class CreateCartFromWishlistBenchmarks
{
    private CreateCartFromWishlistCommandHandler _handler = null!;
    private CreateCartFromWishlistCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _handler = WishlistBenchmarkFixtures.CreateCartFromWishlistHandler(LineItemCount);
        _command = WishlistBenchmarkFixtures.CreateCartFromWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> CreateCartFromWishlist() => _handler.Handle(_command, CancellationToken.None);
}
