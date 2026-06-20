using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>moveWishlistItem</c> GraphQL mutation
/// (<see cref="MoveWishListItemCommandHandler.Handle"/>). Measured compute = TWO cart loads
/// (source and destination), add the item to the destination (<c>AddItemsAsync</c> + recalc),
/// remove from source (linear scan + recalc), then save both.
///
/// This is the most expensive wishlist mutation: it recalculates two aggregates per invocation.
/// The item-count axis grows BOTH the source and destination carts simultaneously (the harness
/// returns the same fixture cart for both ids), so the recalculate cost doubles relative to a
/// single-cart mutation.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public abstract class MoveWishlistItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private MoveWishlistItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateMoveWishlistItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> MoveWishlistItem() => _mediator.Send(_command);
}
