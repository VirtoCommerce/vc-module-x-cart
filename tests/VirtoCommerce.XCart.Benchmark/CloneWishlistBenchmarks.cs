using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>cloneWishlist</c> GraphQL mutation
/// (<see cref="CloneWishlistCommandHandler.Handle"/>). Measured compute = load source wishlist via
/// <c>GetAsync</c>, create a new <see cref="VirtoCommerce.CartModule.Core.Model.ShoppingCart"/>
/// aggregate, copy all items with <c>AddItemsAsync</c> (real add-validation + recalc over the
/// growing clone), then save.
///
/// The item-count axis drives both the source-cart size and the number of items copied into the
/// clone. 100 items surfaces super-linear growth in the copy + recalculate loop.
/// </summary>
[MemoryDiagnoser]
public class CloneWishlistBenchmarks
{
    private CloneWishlistCommandHandler _handler = null!;
    private CloneWishlistCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _handler = WishlistBenchmarkFixtures.CreateCloneWishlistHandler(LineItemCount);
        _command = WishlistBenchmarkFixtures.CreateCloneWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> CloneWishlist() => _handler.Handle(_command, CancellationToken.None);
}
