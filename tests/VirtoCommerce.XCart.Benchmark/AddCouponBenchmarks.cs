using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>addCoupon</c> GraphQL mutation
/// (<see cref="AddCouponCommandHandler.Handle"/>): the mutate-existing-cart path. The measured
/// compute = load the cart (real <see cref="VirtoCommerce.XCart.Data.Services.CartAggregateRepository"/>
/// build + recalc), append the coupon code to <c>Cart.Coupons</c> (pure list op, no I/O), then
/// save (recalc again); only I/O leaves are mocked (DB read/write, never-cache).
///
/// Idempotent without [IterationSetup]: the GetAsync mock returns a fresh cart per call (no
/// coupons) and the never-cache forces a real load every invocation, so the coupon is always
/// added fresh. Mean precision is preserved.
///
/// Two axes: <b>shape</b> (Flat vs Configured — the configured cart's recalculate walk is the
/// more expensive path, visible on the allocation delta) and cart-size count (100 surfaces
/// super-linear growth in the recalculate loop).
/// </summary>
[MemoryDiagnoser]
public class AddCouponBenchmarks
{
    private AddCouponCommandHandler _handler = null!;
    private readonly AddCouponCommand _command = CouponBenchmarkFixtures.CreateAddCouponCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CouponBenchmarkFixtures.CreateAddCouponHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> AddCoupon() => _handler.Handle(_command, CancellationToken.None);
}
