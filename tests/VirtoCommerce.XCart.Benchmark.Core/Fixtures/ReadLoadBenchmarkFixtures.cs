using System.Linq;
using Moq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Benchmark;

/// <summary>
/// Query/validation builders for the read cluster. Handlers and the validation aggregate are now
/// resolved through the DI container (<see cref="CartBenchmarkHost"/>); only the query objects and the
/// working validation-context factory live here.
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

    /// <summary>
    /// A working <see cref="ICartValidationContextFactory"/> that supplies <c>AllCartProducts</c> built
    /// from the aggregate's own line items, so <c>CartValidator</c>'s per-item rules run on real
    /// (active/buyable/priced) products instead of a loose mock's null list. The validation benchmark
    /// registers this via <c>BuildProvider</c>'s <c>customizeServices</c> hook (overriding the host's
    /// loose default), so the resolved aggregate validates against meaningful data.
    /// </summary>
    public static ICartValidationContextFactory CreateValidationContextFactory()
    {
        var mock = new Mock<ICartValidationContextFactory>();
        mock.Setup(x => x.CreateValidationContextAsync(It.IsAny<CartAggregate>()))
            .ReturnsAsync((CartAggregate aggregate) =>
                new CartValidationContext
                {
                    CartAggregate = aggregate,
                    AllCartProducts = aggregate.LineItems
                        .Select(li => CartBenchmarkFixtures.CreateCartProduct(li.ProductId))
                        .ToList(),
                });

        return mock.Object;
    }
}
