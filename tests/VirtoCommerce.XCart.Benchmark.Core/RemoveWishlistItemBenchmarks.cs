using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeWishlistItem</c> GraphQL mutation
/// (<see cref="RemoveWishlistItemCommandHandler.Handle"/>). Measured compute = load wishlist via
/// <c>GetCartByIdAsync</c>, remove <c>li-0</c> by line item id (O(n) linear scan over
/// <c>Items</c>), recalculate, then save.
///
/// The item-count axis grows the wishlist and exercises the linear-scan cost for the remove lookup
/// and the recalculate walk. The harness returns a fresh cart per call so the item is always
/// present.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public abstract class RemoveWishlistItemBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveWishlistItemCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateRemoveWishlistItemCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RemoveWishlistItem() => _mediator.Send(_command);
}
