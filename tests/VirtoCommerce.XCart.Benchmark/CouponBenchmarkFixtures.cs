using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Data.Commands;
using VirtoCommerce.XCart.Data.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Fixture builders for the coupon mutations and the coupon-validation query. All three handlers
/// take only <c>ICartAggregateRepository</c>, so they build directly over the shared
/// <see cref="CartBenchmarkFixtures.CreateMutationHarness"/> (whose loaded cart carries a non-null
/// <c>Coupons</c> collection — required by <c>AddCouponAsync</c>/<c>RemoveCouponAsync</c>).
/// </summary>
internal static class CouponBenchmarkFixtures
{
    /// <summary>Real <see cref="AddCouponCommandHandler"/> over the shared mutation harness.</summary>
    public static AddCouponCommandHandler CreateAddCouponHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>An <c>addCoupon</c> command with a non-empty coupon code. <c>AddCouponAsync</c> does a
    /// case-insensitive duplicate check then appends the code — no validator throws for a non-empty
    /// string, so this is the success path for both shapes.</summary>
    public static AddCouponCommand CreateAddCouponCommand() =>
        CartBenchmarkFixtures.WithCartContext(new AddCouponCommand { CouponCode = "BENCHMARK10" });

    /// <summary>Real <see cref="RemoveCouponCommandHandler"/> over the shared mutation harness.</summary>
    public static RemoveCouponCommandHandler CreateRemoveCouponHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>removeCoupon</c> command for a code not present in the loaded cart (the fixture
    /// starts with an empty <c>Coupons</c> list). <c>RemoveCouponAsync</c> removes the matching entry
    /// (a no-op here) then recalculates — the success path; the recalc is the measured cost.</summary>
    public static RemoveCouponCommand CreateRemoveCouponCommand() =>
        CartBenchmarkFixtures.WithCartContext(new RemoveCouponCommand { CouponCode = "BENCHMARK10" });

    /// <summary>Real <see cref="ValidateCouponQueryHandler"/> over the shared mutation harness. The
    /// validate path loads the cart, clones it, sets <c>Coupons = [coupon]</c>, then evaluates
    /// promotions via the (mocked, empty-result) marketing evaluator — returning <c>false</c> without
    /// throwing.</summary>
    public static ValidateCouponQueryHandler CreateValidateCouponHandler(int lineItemCount, CartShape shape) =>
        new(CartBenchmarkFixtures.CreateMutationHarness(lineItemCount, shape).Repository);

    /// <summary>A <c>validateCoupon</c> query resolved by <c>CartId</c>. <see cref="ValidateCouponQuery"/>
    /// is not a <see cref="CartCommand"/>, so the cart-context fields are stamped here directly.</summary>
    public static ValidateCouponQuery CreateValidateCouponQuery() =>
        new()
        {
            CartId = "benchmark-cart",
            StoreId = CartBenchmarkFixtures.StoreId,
            CurrencyCode = CartBenchmarkFixtures.Currency.Code,
            CultureName = "en-US",
            UserId = "benchmark-user",
            Coupon = "BENCHMARK10",
        };
}
