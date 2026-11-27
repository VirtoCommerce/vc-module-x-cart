using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;
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
            var currency = CreateCurrency(roundingType, decimalDigits);
            var aggregate = GetValidCartAggregate(currency: currency);
            var productPrice = CreateTierProductPrice(currency);
            var lineItem = new LineItem { Quantity = 5 };

            // Act
            aggregate.SetLineItemTierPrice(productPrice, 5, lineItem);

            // Assert
            lineItem.ListPrice.Should().Be(10.0049m);
            lineItem.SalePrice.Should().Be(8.0049m);
        }

        [Theory]
        [InlineData("Rounding001", 2)]
        [InlineData("Rounding001", 0)]
        public async Task ChangeQuantityOfExistingLineItem_TierWithMoreDecimalsThanCurrency_LineItemGetsExactPrices(string roundingType, int decimalDigits)
        {
            // Arrange
            var currency = CreateCurrency(roundingType, decimalDigits);
            var aggregate = CreateTestableAggregate(currency);
            var cartProduct = new CartProduct(new CatalogProduct { Id = "product-1" }) { Price = CreateTierProductPrice(currency) };
            var lineItem = new LineItem { ProductId = "product-1", Quantity = 1, ListPrice = 10m, SalePrice = 10m };

            // Act
            await aggregate.ChangeQuantity(lineItem, 5, cartProduct);

            // Assert
            lineItem.Quantity.Should().Be(5);
            lineItem.ListPrice.Should().Be(10.0049m);
            lineItem.SalePrice.Should().Be(8.0049m);
        }

        [Theory]
        [InlineData("Rounding001", 2)]
        [InlineData("Rounding001", 0)]
        public void UpdateConfigurationItemForProduct_TierWithMoreDecimalsThanCurrency_ConfigurationItemGetsExactPrices(string roundingType, int decimalDigits)
        {
            // Arrange
            var currency = CreateCurrency(roundingType, decimalDigits);
            var aggregate = CreateTestableAggregate(currency);
            var cartProduct = new CartProduct(new CatalogProduct { Id = "product-1", Code = "SKU-1" }) { Price = CreateTierProductPrice(currency) };
            var section = new ProductConfigurationSection
            {
                SectionId = "section-1",
                Option = new ConfigurableProductOption { ProductId = "product-1", Quantity = 5 },
            };
            var configurationItem = new ConfigurationItem();

            // Act
            aggregate.UpdateConfigurationItem(configurationItem, section, cartProduct);

            // Assert
            configurationItem.ListPrice.Should().Be(10.0049m);
            configurationItem.SalePrice.Should().Be(8.0049m);
        }

        [Fact]
        public void ConfiguredLineItemContainer_CreateLineItem_ZeroDigitCurrency_LineItemGetsExactPrices()
        {
            // Arrange
            var currency = CreateCurrency("Rounding001", 0);
            var container = new ConfiguredLineItemContainer { Currency = currency, CultureName = "en-US" };
            var productPrice = new ProductPrice(currency)
            {
                ListPrice = new Money(10.50m, currency),
                SalePrice = new Money(7.20m, currency),
            };
            productPrice.TierPrices.Add(new TierPrice(productPrice.ListPrice, productPrice.SalePrice, 1));
            var cartProduct = new CartProduct(new CatalogProduct { Id = "product-1", Code = "SKU-1" }) { Price = productPrice };

            // Act
            var lineItem = container.CreateLineItem(cartProduct, 2);

            // Assert
            lineItem.ListPrice.Should().Be(10.50m);
            lineItem.SalePrice.Should().Be(7.20m);
            lineItem.DiscountAmount.Should().Be(3.30m);
            lineItem.PlacedPrice.Should().Be(7.20m);
            lineItem.ExtendedPrice.Should().Be(14.40m);
        }

        [Fact]
        public void ConfiguredLineItemContainer_UpdatePrice_ZeroDigitCurrency_UsesExactConfigurableProductPrice()
        {
            // Arrange
            var currency = CreateCurrency("Rounding001", 0);
            var productPrice = new ProductPrice(currency)
            {
                ListPrice = new Money(10.50m, currency),
                SalePrice = new Money(7.20m, currency),
                DiscountAmount = new Money(3.30m, currency),
            };
            var container = new ConfiguredLineItemContainer
            {
                Currency = currency,
                CultureName = "en-US",
                ConfigurableProduct = new CartProduct(new CatalogProduct { Id = "configurable-1" }) { Price = productPrice },
            };
            var lineItem = new LineItem { Quantity = 1 };

            // Act
            container.UpdatePrice(lineItem);

            // Assert
            lineItem.ListPrice.Should().Be(10.50m);
            lineItem.SalePrice.Should().Be(7.20m);
            lineItem.DiscountAmount.Should().Be(3.30m);
            lineItem.PlacedPrice.Should().Be(7.20m);
        }

        private static Currency CreateCurrency(string roundingType, int decimalDigits)
        {
            return new Currency(Language.InvariantLanguage, "TST")
            {
                DecimalDigits = decimalDigits,
                RoundingType = roundingType,
                MidpointRounding = "AwayFromZero",
                RoundingPolicy = new DefaultMoneyRoundingPolicy(),
                ExchangeRate = 1m,
            };
        }

        private static ProductPrice CreateTierProductPrice(Currency currency)
        {
            var productPrice = new ProductPrice(currency)
            {
                ListPrice = new Money(10.0049m, currency),
                SalePrice = new Money(10.0049m, currency),
            };
            productPrice.TierPrices.Add(new TierPrice(new Money(10.0049m, currency), new Money(10.0049m, currency), 1));
            productPrice.TierPrices.Add(new TierPrice(new Money(10.0049m, currency), new Money(8.0049m, currency), 5));

            return productPrice;
        }

        private TestableCartAggregate CreateTestableAggregate(Currency currency)
        {
            var aggregate = new TestableCartAggregate(
                _marketingPromoEvaluatorMock.Object,
                new Mock<IShoppingCartTotalsCalculator>().Object,
                _taxProviderSearchServiceMock.Object,
                _cartProductServiceMock.Object,
                _dynamicPropertyUpdaterService.Object,
                _mapperMock.Object,
                _memberService.Object,
                _genericPipelineLauncherMock.Object,
                _fileUploadService.Object,
                _cartValidationContextFactoryMock.Object,
                _cartItemBuilder,
                _cartValidatorRegistry);

            aggregate.GrabCart(GetCart(), new Store(), GetMember(), currency);

            return aggregate;
        }

        private sealed class TestableCartAggregate(
            IMarketingPromoEvaluator marketingEvaluator,
            IShoppingCartTotalsCalculator cartTotalsCalculator,
            IOptionalDependency<ITaxProviderSearchService> taxProviderSearchService,
            ICartProductService cartProductService,
            IDynamicPropertyUpdaterService dynamicPropertyUpdaterService,
            IXCartMapper mapper,
            IMemberService memberService,
            IGenericPipelineLauncher pipeline,
            IFileUploadService fileUploadService,
            ICartValidationContextFactory cartValidationContextFactory,
            ICartItemBuilder cartItemBuilder,
            ICartValidatorRegistry cartValidatorRegistry)
            : CartAggregate(marketingEvaluator, cartTotalsCalculator, taxProviderSearchService, cartProductService, dynamicPropertyUpdaterService,
                mapper, memberService, pipeline, fileUploadService, cartValidationContextFactory, cartItemBuilder, cartValidatorRegistry)
        {
            public Task<CartAggregate> ChangeQuantity(LineItem lineItem, int quantity, CartProduct product)
                => InnerChangeItemQuantityAsync(lineItem, quantity, product);

            public void UpdateConfigurationItem(ConfigurationItem item, ProductConfigurationSection section, CartProduct cartProduct)
                => UpdateConfigurationItemForProduct(item, section, cartProduct);
        }
    }
}
