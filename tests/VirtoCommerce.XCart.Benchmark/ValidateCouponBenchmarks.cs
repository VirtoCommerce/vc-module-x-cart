using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Data.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>validateCoupon</c> GraphQL query
/// (<see cref="ValidateCouponQueryHandler.Handle"/>): load the cart by <c>CartId</c> (real
/// <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/> build + recalc), clone
/// the aggregate, set <c>Coupons = [coupon]</c>, then call <c>ValidateCouponAsync</c> (promotion
/// evaluation — the mocked marketing evaluator returns an empty <see cref="VirtoCommerce.MarketingModule.Core.Model.Promotions.PromotionResult"/>
/// so the query returns <c>false</c>). Only I/O leaves are mocked; the totals calculator and
/// clone are real.
///
/// Unlike the mutation benchmarks the result is <c>Task&lt;bool&gt;</c> — returning it prevents
/// dead-code elimination while keeping the measurement clean. Idempotent without [IterationSetup]:
/// the never-cache forces a real load every call and the query only reads (never mutates) the
/// loaded cart. Flat vs Configured surfaces the configured-cart recalculate cost; count surfaces
/// super-linear growth in both the load and the clone path.
/// </summary>
[MemoryDiagnoser]
public class ValidateCouponBenchmarks
{
    private ValidateCouponQueryHandler _handler = null!;
    private readonly ValidateCouponQuery _query = CouponBenchmarkFixtures.CreateValidateCouponQuery();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CouponBenchmarkFixtures.CreateValidateCouponHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<bool> ValidateCoupon() => _handler.Handle(_query, CancellationToken.None);
}
