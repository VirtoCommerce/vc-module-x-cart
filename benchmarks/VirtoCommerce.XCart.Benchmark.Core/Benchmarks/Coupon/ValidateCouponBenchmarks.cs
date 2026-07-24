using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>validateCoupon</c> GraphQL query, resolved through
/// <see cref="IMediator"/>: load the cart by <c>CartId</c> (real build + recalc), clone the aggregate,
/// set <c>Coupons = [coupon]</c>, then evaluate promotions (the mocked marketing evaluator returns an
/// empty result → <c>false</c>). Only I/O leaves are mocked; the totals calculator and clone are real.
/// Result is <c>Task&lt;bool&gt;</c> (returning it prevents DCE). Idempotent without [IterationSetup].
/// Flat vs Configured; count axis.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Coupon, Categories.Validation)]
public abstract class ValidateCouponBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private ValidateCouponQuery _query = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _query = CouponBenchmarkFixtures.CreateValidateCouponQuery();
    }

    [Benchmark]
    public Task<bool> ValidateCoupon() => _mediator.Send(_query);
}
