using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Data.Queries;

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
public class GetWishlistBenchmarks
{
    private GetWishlistQueryHandler _handler = null!;
    private readonly GetWishlistQuery _query = WishlistBenchmarkFixtures.CreateGetWishlistQuery();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateGetWishlistHandler(LineItemCount);

    [Benchmark]
    public Task<CartAggregate> GetWishlist() => _handler.Handle(_query, CancellationToken.None);
}
