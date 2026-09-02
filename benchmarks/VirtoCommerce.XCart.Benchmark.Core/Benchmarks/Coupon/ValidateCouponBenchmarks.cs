using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query-level microbenchmark of the <c>validateCoupon</c> GraphQL query, resolved through
/// <see cref="IMediator"/>: load the cart by <c>CartId</c> (real build + recalc), clone the aggregate,
/// set <c>Coupons = [coupon]</c>, then evaluate promotions. The setup's evaluator returns a reward, so
/// the real reward pipeline runs; the answer is nevertheless <c>false</c>, because that reward carries
/// no coupon code and <c>ValidateCouponAsync</c> only accepts a reward that is valid AND whose
/// <c>Coupon</c> equals the requested one. The clone is real. Result is <c>Task&lt;bool&gt;</c>
/// (returning it prevents DCE).
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
