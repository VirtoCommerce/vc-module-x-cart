using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Data.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>wishlists</c> GraphQL query
/// (<see cref="SearchWishlistQueryHandler.Handle"/>). Measured compute = build
/// <see cref="VirtoCommerce.CartModule.Core.Model.Search.ShoppingCartSearchCriteria"/> via
/// <c>CartSearchCriteriaBuilder</c>, dispatch to <c>IShoppingCartSearchService.SearchAsync</c>
/// (returns empty — no per-cart load or recalculate), return an empty
/// <see cref="SearchCartResponse"/>.
///
/// No item-count axis: the search handler does not load or recalculate cart aggregates when the
/// search result is empty — cost is constant regardless of wishlist count. This benchmark measures
/// criteria-build + mock-dispatch overhead, establishing a baseline for the search path.
/// </summary>
[MemoryDiagnoser]
public class SearchWishlistBenchmarks
{
    private SearchWishlistQueryHandler _handler = null!;
    private readonly SearchWishlistQuery _query = WishlistBenchmarkFixtures.CreateSearchWishlistQuery();

    [GlobalSetup]
    public void Setup() => _handler = WishlistBenchmarkFixtures.CreateSearchWishlistHandler();

    [Benchmark]
    public Task<SearchCartResponse> SearchWishlists() => _handler.Handle(_query, CancellationToken.None);
}
