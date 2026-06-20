using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>wishlist</c> GraphQL query
/// (<see cref="GetWishlistQueryHandler.Handle"/>). Measured compute = load the wishlist aggregate
/// via <c>CartAggregateRepository.GetCartByIdAsync</c>: currency + store resolution, real
/// <c>RecalculateAsync</c> over the cart's items, and product-include field filtering (empty →
/// no product service call).
///
/// This is the pure read path: no mutations, no saves. The item-count axis exercises the
/// recalculate cost as a function of wishlist size.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Wishlist)]
public abstract class GetWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private GetWishlistQuery _query = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, CartShape.Flat).GetRequiredService<IMediator>();
        _query = WishlistBenchmarkFixtures.CreateGetWishlistQuery();
    }

    [Benchmark]
    public Task<CartAggregate> GetWishlist() => _mediator.Send(_query);
}
