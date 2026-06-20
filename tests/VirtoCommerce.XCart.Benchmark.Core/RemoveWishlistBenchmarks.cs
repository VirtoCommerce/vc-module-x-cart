using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeWishlist</c> GraphQL mutation
/// (<see cref="RemoveWishlistCommandHandler.Handle"/>). Measured compute =
/// <c>CartAggregateRepository.RemoveCartAsync</c> → <c>IShoppingCartService.DeleteAsync</c> (no-op
/// mock) + cache expiry token invalidation. Returns null (no aggregate).
///
/// No item-count axis: the handler never loads the cart or recalculates — it issues one delete
/// call regardless of wishlist size. A single invocation measures the delete overhead cleanly.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public abstract class RemoveWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveWishlistCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(0, CartShape.Flat).GetRequiredService<IMediator>();
        _command = WishlistBenchmarkFixtures.CreateRemoveWishlistCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RemoveWishlist() => _mediator.Send(_command);
}
