using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

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
[BenchmarkCategory(Categories.Wishlist)]
public abstract class AddWishlistItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddWishlistItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateAddWishlistItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddWishlistItem() => _mediator.Send(_command);
}
