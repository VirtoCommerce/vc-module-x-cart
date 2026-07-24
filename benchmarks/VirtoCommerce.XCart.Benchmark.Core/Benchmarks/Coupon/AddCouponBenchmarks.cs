using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCoupon</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: load the cart (real build + recalc), append the coupon code to
/// <c>Cart.Coupons</c> (pure list op), then save (recalc again). Only I/O leaves are mocked.
/// Idempotent without [IterationSetup] (fresh cart per call). Flat vs Configured surfaces the
/// configured recalc cost; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Coupon)]
public abstract class AddCouponBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private AddCouponCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CouponBenchmarkFixtures.CreateAddCouponCommand();
    }

    [Benchmark]
    public Task<CartAggregate> AddCoupon() => _mediator.Send(_command);
}
