using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

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
[BenchmarkCategory(Categories.Wishlist)]
public abstract class SearchWishlistBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private SearchWishlistQuery _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(0, CartShape.Flat).GetRequiredService<IMediator>();
        _query = WishlistBenchmarkFixtures.CreateSearchWishlistQuery();
    }

    [Benchmark]
    public Task<SearchCartResponse> SearchWishlists() => _mediator.Send(_query);
}
