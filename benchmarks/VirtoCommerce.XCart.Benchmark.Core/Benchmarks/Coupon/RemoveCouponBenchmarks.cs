using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeCoupon</c> GraphQL mutation, resolved through
/// <see cref="IMediator"/>: load (real build + recalc), remove the coupon code from <c>Cart.Coupons</c>
/// (pure list op), save (recalc again). Only I/O leaves are mocked. Idempotent without [IterationSetup]
/// (fresh cart per call); the recalculate cycle dominates. Flat vs Configured; count axis.
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory(Categories.Coupon)]
public abstract class RemoveCouponBenchmarksBase : CartBenchmarkBase
{
    private IMediator _mediator = null!;
    private RemoveCouponCommand _command = null!;

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mediator = BuildProvider(LineItemCount, Shape).GetRequiredService<IMediator>();
        _command = CouponBenchmarkFixtures.CreateRemoveCouponCommand();
    }

    [Benchmark]
    public Task<CartAggregate> RemoveCoupon() => _mediator.Send(_command);
}
