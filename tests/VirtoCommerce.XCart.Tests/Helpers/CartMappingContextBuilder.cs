using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Tests.Helpers;

/// <summary>
/// Builds a <see cref="CartMappingContext"/> with its <see cref="CartMappingContext.NewCartItem"/> carrier
/// always populated, so a fixture never hits the guard <c>XCartMapper.ToLineItem</c> puts on it - the shape
/// production always builds through <c>CartAggregate.CreateCartMappingContext</c>.
/// </summary>
public static class CartMappingContextBuilder
{
    public static CartMappingContext Build(string cultureName = null, string currencyCode = null, NewCartItem newCartItem = null)
    {
        return new CartMappingContext
        {
            CultureName = cultureName,
            CurrencyCode = currencyCode,
            NewCartItem = newCartItem ?? new NewCartItem(),
        };
    }
}
