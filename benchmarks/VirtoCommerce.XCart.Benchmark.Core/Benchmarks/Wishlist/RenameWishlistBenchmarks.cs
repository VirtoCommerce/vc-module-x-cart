using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

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
[BenchmarkCategory(Categories.Wishlist)]
public abstract class RenameWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RenameWishlistCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateRenameWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RenameWishlist() => _mediator.Send(_command);
}
