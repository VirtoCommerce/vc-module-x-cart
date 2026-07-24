using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

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
public abstract class UpdateWishlistItemsBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private UpdateWishlistItemsCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateUpdateWishlistItemsCommand();
    }

    [Benchmark]
    public Task<CartAggregate> UpdateWishlistItems() => _mediator.Send(_command);
}
