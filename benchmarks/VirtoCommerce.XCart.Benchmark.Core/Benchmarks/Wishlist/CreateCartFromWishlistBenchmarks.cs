using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

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
public abstract class CreateCartFromWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private CreateCartFromWishlistCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateCartFromWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> CreateCartFromWishlist() => _mediator.Send(_command);
}
