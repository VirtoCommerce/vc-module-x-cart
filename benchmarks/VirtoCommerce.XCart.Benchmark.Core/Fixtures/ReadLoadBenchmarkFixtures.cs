using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query builders and the validation rule-set constant for the read cluster. Handlers and the validation
/// aggregate are resolved through the DI container (<see cref="CartBenchmarkHost"/>).
/// </summary>
internal static class ReadLoadBenchmarkFixtures
{
    /// <summary>A <c>getCart</c> query resolved by CartId (the CartId load path).</summary>
    public static GetCartQuery CreateGetCartQuery()
    {
        var query = AbstractTypeFactory<GetCartQuery>.TryCreateInstance();
        query.CartId = "benchmark-cart";
        query.StoreId = CartBenchmarkFixtures.StoreId;
        query.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        query.CultureName = "en-US";
        query.UserId = "benchmark-user";

        return query;
    }

    /// <summary>A <c>getPricesSum</c> query whose <see cref="GetPricesSumQuery.LineItemIds"/> covers
    /// all line items of the loaded cart so the copy + recalc path is fully exercised.</summary>
    public static GetPricesSumQuery CreateGetPricesSumQuery(int lineItemCount)
    {
        var query = AbstractTypeFactory<GetPricesSumQuery>.TryCreateInstance();
        query.CartId = "benchmark-cart";
        query.StoreId = CartBenchmarkFixtures.StoreId;
        query.CurrencyCode = CartBenchmarkFixtures.Currency.Code;
        query.CultureName = "en-US";
        query.UserId = "benchmark-user";
        query.LineItemIds = Enumerable.Range(0, lineItemCount).Select(i => $"li-{i}").ToList();

        return query;
    }

    /// <summary>The Items rule set — the per-line-item validation hot path exercised by
    /// <c>CartType.validationErrors</c>.</summary>
    public const string ItemsRuleSet = ModuleConstants.ValidationRuleSets.Items;
}
