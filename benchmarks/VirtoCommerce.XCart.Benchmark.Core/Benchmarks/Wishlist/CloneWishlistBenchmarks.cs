using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

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
[BenchmarkCategory(Categories.Wishlist)]
public abstract class CloneWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private CloneWishlistCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateCloneWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> CloneWishlist() => _mediator.Send(_command);
}
