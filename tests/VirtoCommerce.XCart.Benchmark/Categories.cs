namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Functional benchmark categories for selective runs. A benchmark class is tagged with
/// <c>[BenchmarkCategory(Categories.X)]</c> and runs are filtered with
/// <c>--anyCategories &lt;name&gt;</c> (space-separated = OR) or <c>--allCategories</c>. A class may
/// carry more than one category (e.g. ValidateCoupon is both <see cref="Coupon"/> and
/// <see cref="Validation"/>). Names are functional areas, not cost tiers.
/// </summary>
internal static class Categories
{
    public const string Items = "items";
    public const string Configuration = "configuration";
    public const string Checkout = "checkout";
    public const string CartState = "cart-state";
    public const string Coupon = "coupon";
    public const string Queries = "queries";
    public const string Recalculate = "recalculate";
    public const string Validation = "validation";
    public const string Wishlist = "wishlist";
    public const string Gifts = "gifts";
    public const string SavedForLater = "saved-for-later";
    public const string DynamicProperties = "dynamic-properties";
}
