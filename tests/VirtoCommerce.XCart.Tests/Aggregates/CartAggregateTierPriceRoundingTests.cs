using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Aggregates
{
    // VCST-6824: tier prices reach the line item unrounded, the same way as the regular list and sale prices
    public class CartAggregateTierPriceRoundingTests : XCartMoqHelper
    {
        [Theory]
        [InlineData("Rounding001", 2)]
        [InlineData("Rounding005Up", 2)]
        [InlineData("Rounding1", 2)]
        [InlineData("Rounding001", 0)]
        public void SetLineItemTierPrice_TierWithMoreDecimalsThanCurrency_LineItemGetsExactPrices(string roundingType, int decimalDigits)
        {
            // Arrange
            var currency = new Currency(Language.InvariantLanguage, "TST")
            {
                DecimalDigits = decimalDigits,
                RoundingType = roundingType,
                MidpointRounding = "AwayFromZero",
                RoundingPolicy = new DefaultMoneyRoundingPolicy(),
                ExchangeRate = 1m,
            };
            var aggregate = GetValidCartAggregate(currency: currency);
            var productPrice = new ProductPrice(currency)
            {
                ListPrice = new Money(10.0049m, currency),
                SalePrice = new Money(10.0049m, currency),
            };
            productPrice.TierPrices.Add(new TierPrice(new Money(10.0049m, currency), new Money(8.0049m, currency), 5));
            var lineItem = new LineItem { Quantity = 5 };

            // Act
            aggregate.SetLineItemTierPrice(productPrice, 5, lineItem);

            // Assert
            lineItem.ListPrice.Should().Be(10.0049m);
            lineItem.SalePrice.Should().Be(8.0049m);
        }
    }
}
