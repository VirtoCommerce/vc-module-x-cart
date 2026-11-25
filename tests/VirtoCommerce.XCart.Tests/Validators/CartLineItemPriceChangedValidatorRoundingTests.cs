using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Validators;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Validators
{
    // VCST-6824: line items keep the exact sale price, the product price is rounded to the currency;
    // the price-changed warning must compare them at the currency precision
    public class CartLineItemPriceChangedValidatorRoundingTests
    {
        private const string ProductId = "product-1";

        [Theory]
        // exact price kept in the line item, more digits than the currency
        [InlineData(0, 7.20, 7.20)]
        [InlineData(2, 10.0049, 10.0049)]
        // line item saved before VCST-6824 with an already rounded price
        [InlineData(0, 7, 7.20)]
        [InlineData(2, 10.00, 10.0049)]
        public async Task Validate_SamePriceAtCurrencyPrecision_NoWarning(int decimalDigits, decimal lineItemSalePrice, decimal productSalePrice)
        {
            // Arrange
            var context = CreateContext(decimalDigits, lineItemSalePrice, productSalePrice);

            // Act
            var result = await new CartLineItemPriceChangedValidator().ValidateAsync(context, TestContext.Current.CancellationToken);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0, 7.20, 8.40)]
        [InlineData(2, 10.0049, 10.0151)]
        public async Task Validate_PriceChangedAtCurrencyPrecision_Warning(int decimalDigits, decimal lineItemSalePrice, decimal productSalePrice)
        {
            // Arrange
            var context = CreateContext(decimalDigits, lineItemSalePrice, productSalePrice);

            // Act
            var result = await new CartLineItemPriceChangedValidator().ValidateAsync(context, TestContext.Current.CancellationToken);

            // Assert
            result.Errors.Should().ContainSingle(x => x.ErrorCode == "PRODUCT_PRICE_CHANGED");
        }

        private static CartLineItemPriceChangedValidationContext CreateContext(int decimalDigits, decimal lineItemSalePrice, decimal productSalePrice)
        {
            var currency = new Currency(Language.InvariantLanguage, "TST")
            {
                DecimalDigits = decimalDigits,
                RoundingType = "Rounding001",
                MidpointRounding = "AwayFromZero",
                RoundingPolicy = new DefaultMoneyRoundingPolicy(),
                ExchangeRate = 1m,
            };

            var productPrice = new ProductPrice(currency)
            {
                ListPrice = new Money(20m, currency),
                SalePrice = new Money(productSalePrice, currency),
            };
            productPrice.TierPrices.Add(new TierPrice(productPrice.ListPrice, productPrice.SalePrice, 1));

            var cartProduct = new CartProduct(new CatalogProduct { Id = ProductId }) { Price = productPrice };

            var lineItem = new LineItem
            {
                ProductId = ProductId,
                Currency = currency.Code,
                Quantity = 1,
                ListPrice = 20m,
                SalePrice = lineItemSalePrice,
            };

            return new CartLineItemPriceChangedValidationContext
            {
                LineItem = lineItem,
                CartProducts = new Dictionary<string, CartProduct>
                {
                    [CartAggregate.FormatGetCartProductKey(ProductId, currency.Code)] = cartProduct,
                },
            };
        }
    }
}
