using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Data.Commands;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command-level microbenchmark of the <c>removeCoupon</c> GraphQL mutation
/// (<see cref="RemoveCouponCommandHandler.Handle"/>): the mutate-existing-cart path — load (real
/// build + recalc), remove the coupon code from <c>Cart.Coupons</c> (pure list op), save (recalc
/// again). Only I/O leaves are mocked. Idempotent without [IterationSetup]: the fixture cart has
/// no coupons so <c>RemoveCouponAsync</c> calls <c>Coupons.Remove(null)</c> (no-op) every
/// invocation — the recalculate cycle is the dominant cost, not the coupon list op itself. Flat
/// vs Configured surfaces configured-product regressions; count surfaces super-linear growth.
/// </summary>
[MemoryDiagnoser]
public class RemoveCouponBenchmarks
{
    private RemoveCouponCommandHandler _handler = null!;
    private readonly RemoveCouponCommand _command = CouponBenchmarkFixtures.CreateRemoveCouponCommand();

    [Params(1, 5, 20, 100)]
    public int LineItemCount { get; set; }

    [Params(CartShape.Flat, CartShape.Configured)]
    public CartShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => _handler = CouponBenchmarkFixtures.CreateRemoveCouponHandler(LineItemCount, Shape);

    [Benchmark]
    public Task<CartAggregate> RemoveCoupon() => _handler.Handle(_command, CancellationToken.None);
}
