using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>updateWishlistItems</c> GraphQL mutation
/// (<see cref="UpdateWishlistItemsCommandHandler.Handle"/>). Measured compute = load wishlist via
/// <c>GetCartByIdAsync</c>, iterate over the command's Items (one item — <c>li-0</c>), look up
/// the cart product, call <c>ChangeItemQuantityAsync</c>, then save.
///
/// The item-count axis grows the wishlist the update runs AGAINST (affecting load + recalculate
/// cost) while the number of items updated stays fixed at one. This isolates the per-item update
/// cost from the batch-size cost. To benchmark batch updates, vary the Items list size; for now
/// the single-item case is the representative path.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public class UpdateWishlistItemsBenchmarks
{
    private UpdateWishlistItemsCommandHandler _handler = null!;
    private readonly UpdateWishlistItemsCommand _command = WishlistBenchmarkFixtures.CreateUpdateWishlistItemsCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateUpdateWishlistItemsHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> UpdateWishlistItems() => _handler.Handle(_command, CancellationToken.None);
}
