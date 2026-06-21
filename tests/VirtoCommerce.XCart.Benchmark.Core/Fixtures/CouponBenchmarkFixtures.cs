using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Command/query builders for the coupon mutations and the coupon-validation query. Handlers and the
/// aggregate are resolved through the DI container (<see cref="CartBenchmarkHost"/>); only the command
/// and query objects live here.
/// </summary>
internal static class CouponBenchmarkFixtures
{
    /// <summary>An <c>addCoupon</c> command with a non-empty coupon code. <c>AddCouponAsync</c> does a
    /// case-insensitive duplicate check then appends the code — no validator throws for a non-empty
    /// string, so this is the success path for both shapes.</summary>
    public static AddCouponCommand CreateAddCouponCommand()
    {
        var command = AbstractTypeFactory<AddCouponCommand>.TryCreateInstance();
        command.CouponCode = "BENCHMARK10";

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>A <c>removeCoupon</c> command for a code not present in the loaded cart (the fixture
    /// starts with an empty <c>Coupons</c> list). <c>RemoveCouponAsync</c> removes the matching entry
    /// (a no-op here) then recalculates — the success path; the recalc is the measured cost.</summary>
    public static RemoveCouponCommand CreateRemoveCouponCommand()
    {
        var command = AbstractTypeFactory<RemoveCouponCommand>.TryCreateInstance();
        command.CouponCode = "BENCHMARK10";

        return CartBenchmarkFixtures.WithCartContext(command);
    }

    /// <summary>A <c>validateCoupon</c> query resolved by <c>CartId</c>. <see cref="ValidateCouponQuery"/>
    /// is not a <see cref="CartCommand"/>, so the cart-context fields are stamped here directly.</summary>
    public static ValidateCouponQuery CreateValidateCouponQuery()
    {
        var query = AbstractTypeFactory<ValidateCouponQuery>.TryCreateInstance();
        query.CartId = "benchmark-cart";
        query.StoreId = CartBenchmarkFixtures.StoreId;
        query.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        query.CultureName = "en-US";
        query.UserId = "benchmark-user";
        query.Coupon = "BENCHMARK10";

        return query;
    }
}
