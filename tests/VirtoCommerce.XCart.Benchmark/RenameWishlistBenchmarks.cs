using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>renameWishlist</c> GraphQL mutation
/// (<see cref="RenameWishlistCommandHandler.Handle"/>). Measured compute = load existing wishlist
/// via <c>GetCartByIdAsync</c> (real recalc on load), rename <c>Cart.Name</c>, save (recalc again).
///
/// The rename itself is O(1); the load + save recalculate cost scales with cart size — 100 items
/// surfaces any super-linear growth in the totals calculator.
/// </summary>
[MemoryDiagnoser]
public class RenameWishlistBenchmarks
{
    private RenameWishlistCommandHandler _handler = null!;
    private readonly RenameWishlistCommand _command = WishlistBenchmarkFixtures.CreateRenameWishlistCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateRenameWishlistHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> RenameWishlist() => _handler.Handle(_command, CancellationToken.None);
}
