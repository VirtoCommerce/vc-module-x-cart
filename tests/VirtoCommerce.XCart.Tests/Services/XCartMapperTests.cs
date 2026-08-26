using System;
using System.Linq;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Tests.Helpers.Stubs;
using VirtoCommerce.XCart.Tests.Mappers;
using Xunit;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using ProductPrice = VirtoCommerce.Xapi.Core.Models.ProductPrice;

namespace VirtoCommerce.XCart.Tests.Services;

[Collection(TaxAddressFactoryStateCollection.Name)]
public class XCartMapperTests
{
    private readonly IXCartMapper _mapper = new XCartMapper(new CartItemBuilder());

    [Fact]
    public void ToGiftItem_CopiesRewardFields_LeavesGiftItemOwnFieldsUnset()
    {
        var reward = new GiftReward
        {
            Id = "reward-1",
            IsValid = true,
            Coupon = "SAVE10",
            PromotionId = "promo-1",
            Name = "Free gift",
            CategoryId = "cat-1",
            ProductId = "prod-1",
            Quantity = 2,
            MeasureUnit = "each",
            ImageUrl = "http://example.com/img.png",
        };

        var result = _mapper.ToGiftItem(reward);

        result.Id.Should().Be("reward-1");
        result.IsValid.Should().BeTrue();
        result.Coupon.Should().Be("SAVE10");
        result.PromotionId.Should().Be("promo-1");
        result.Name.Should().Be("Free gift");
        result.CategoryId.Should().Be("cat-1");
        result.ProductId.Should().Be("prod-1");
        result.Quantity.Should().Be(2);
        result.MeasureUnit.Should().Be("each");
        result.ImageUrl.Should().Be("http://example.com/img.png");

        result.CatalogId.Should().BeNull();
        result.Sku.Should().BeNull();
        result.LineItemId.Should().BeNull();
        result.HasLineItem.Should().BeFalse();
    }

    [Fact]
    public void ToGiftItem_NullSource_ReturnsNull()
    {
        _mapper.ToGiftItem(null).Should().BeNull();
    }

    [Fact]
    public void ToGiftLineItem_CopiesMatchingFields_LeavesLineItemOnlyFieldsUnset()
    {
        var giftItem = new GiftItem
        {
            Id = "gift-1",
            Name = "Free gift",
            CategoryId = "cat-1",
            ProductId = "prod-1",
            Quantity = 2,
            MeasureUnit = "each",
            ImageUrl = "http://example.com/img.png",
            CatalogId = "catalog-1",
            Sku = "SKU-1",
            LineItemId = "line-1",
        };

        var result = _mapper.ToGiftLineItem(giftItem);

        result.Id.Should().Be("gift-1");
        result.Name.Should().Be("Free gift");
        result.CategoryId.Should().Be("cat-1");
        result.ProductId.Should().Be("prod-1");
        result.Quantity.Should().Be(2);
        result.MeasureUnit.Should().Be("each");
        result.ImageUrl.Should().Be("http://example.com/img.png");
        result.CatalogId.Should().Be("catalog-1");
        result.Sku.Should().Be("SKU-1");

        result.GiftItemId.Should().BeNull();
    }

    [Fact]
    public void ToGiftLineItem_NullSource_ReturnsNull()
    {
        _mapper.ToGiftLineItem(null).Should().BeNull();
    }

    [Fact]
    public void ToLineItem_CopiesAllProductFields()
    {
        var currency = new Currency(CoreModule.Core.Common.Language.InvariantLanguage, "USD");
        var catalogProduct = new CatalogProduct
        {
            Id = "prod-1",
            CatalogId = "catalog-1",
            CategoryId = "cat-1",
            Code = "SKU-1",
            OuterId = "outer-1",
            ProductType = "Physical",
            TaxType = "TaxA",
            Height = 1,
            Length = 2,
            Width = 3,
            Weight = 4,
            WeightUnit = "kg",
            MeasureUnit = "each",
            Images = [new Image { Url = "http://example.com/img.png", SortOrder = 0 }],
            Vendor = "vendor-1",
        };
        var cartProduct = new CartProduct(catalogProduct)
        {
            Price = new ProductPrice(currency)
            {
                ListPrice = new Money(100m, currency),
                SalePrice = new Money(80m, currency),
                DiscountAmount = new Money(20m, currency),
                PricelistId = "pl-1",
                TaxPercentRate = 0.1m,
            },
        };

        var result = _mapper.ToLineItem(cartProduct, new CartMappingContext());

        result.ProductId.Should().Be("prod-1");
        result.CatalogId.Should().Be("catalog-1");
        result.CategoryId.Should().Be("cat-1");
        result.Sku.Should().Be("SKU-1");
        result.ProductOuterId.Should().Be("outer-1");
        result.ProductType.Should().Be("Physical");
        result.TaxType.Should().Be("TaxA");
        result.Height.Should().Be(1);
        result.Length.Should().Be(2);
        result.Width.Should().Be(3);
        result.Weight.Should().Be(4);
        result.WeightUnit.Should().Be("kg");
        result.MeasureUnit.Should().Be("each");
        result.ImageUrl.Should().Be("http://example.com/img.png");
        result.VendorId.Should().Be("vendor-1");
        result.Currency.Should().Be("USD");
        result.ListPrice.Should().Be(100m);
        result.SalePrice.Should().Be(80m);
        result.DiscountAmount.Should().Be(20m);
        result.PriceId.Should().Be("pl-1");
        result.TaxPercentRate.Should().Be(0.1m);
    }

    [Fact]
    public void ToLineItem_NullSource_ReturnsNull()
    {
        _mapper.ToLineItem(null, null).Should().BeNull();
    }

    [Fact]
    public void ToLineItem_NullContext_Throws()
    {
        var cartProduct = new CartProduct(new CatalogProduct { Id = "prod-1" });

        FluentActions.Invoking(() => _mapper.ToLineItem(cartProduct, null))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToLineItem_UsesCtorInjectedCartItemBuilder()
    {
        var expected = new LineItem { ProductId = "from-injected-builder" };
        var builderMock = new Mock<ICartItemBuilder>();
        builderMock.Setup(x => x.Create(It.IsAny<CartProduct>())).Returns(expected);
        var mapper = new XCartMapper(builderMock.Object);

        var result = mapper.ToLineItem(new CartProduct(new CatalogProduct { Id = "prod-1" }), new CartMappingContext());

        result.Should().BeSameAs(expected);
        builderMock.Verify(x => x.Create(It.IsAny<CartProduct>()), Times.Once);
    }

    [Fact]
    public void ToTaxLines_ShippingRate_UsesDiscountAmountWhenPositive()
    {
        var shippingRate = new ShippingRate
        {
            ShippingMethod = new StubShippingMethod("standard") { TaxType = "TaxA" },
            OptionName = "opt-1",
            Rate = 10m,
            DiscountAmount = 3m,
        };

        var lines = _mapper.ToTaxLines(shippingRate).ToList();

        lines.Should().ContainSingle();
        lines[0].Id.Should().Be("standard&opt-1");
        lines[0].Code.Should().Be("standard");
        lines[0].TaxType.Should().Be("TaxA");
        lines[0].Amount.Should().Be(3m);
    }

    [Fact]
    public void ToTaxLines_ShippingRate_FallsBackToRateWhenNoDiscount()
    {
        var shippingRate = new ShippingRate
        {
            ShippingMethod = new StubShippingMethod("standard"),
            Rate = 10m,
            DiscountAmount = 0m,
        };

        var lines = _mapper.ToTaxLines(shippingRate).ToList();

        lines.Should().ContainSingle().Which.Amount.Should().Be(10m);
    }

    [Fact]
    public void ToTaxLines_ShippingRate_NullSource_ReturnsNull()
    {
        _mapper.ToTaxLines((ShippingRate)null).Should().BeNull();
    }

    [Fact]
    public void ToTaxLines_PaymentMethod_UsesTotalWhenPositive()
    {
        var paymentMethod = new StubPaymentMethod("gateway-1") { TaxType = "TaxA", Price = 5m, DiscountAmount = 1m };

        var lines = _mapper.ToTaxLines(paymentMethod).ToList();

        lines.Should().ContainSingle();
        lines[0].Id.Should().Be("gateway-1");
        lines[0].Code.Should().Be("gateway-1");
        lines[0].TaxType.Should().Be("TaxA");
        lines[0].Amount.Should().Be(4m);
    }

    [Fact]
    public void ToTaxLines_PaymentMethod_FallsBackToPriceWhenTotalNotPositive()
    {
        var paymentMethod = new StubPaymentMethod("gateway-1") { Price = 5m, DiscountAmount = 5m };

        var lines = _mapper.ToTaxLines(paymentMethod).ToList();

        lines.Should().ContainSingle().Which.Amount.Should().Be(5m);
    }

    [Fact]
    public void ToTaxLines_PaymentMethod_NullSource_ReturnsNull()
    {
        _mapper.ToTaxLines((PaymentMethod)null).Should().BeNull();
    }

    [Fact]
    public void ToProductPromoEntry_CopiesExpectedFields()
    {
        var lineItem = new LineItem
        {
            CatalogId = "catalog-1",
            CategoryId = "cat-1",
            Sku = "SKU-1",
            ProductId = "prod-1",
            Quantity = 3,
            ListPrice = 100m,
            SalePrice = 80m,
            DiscountAmount = 20m,
        };

        var result = _mapper.ToProductPromoEntry(lineItem);

        result.CatalogId.Should().Be("catalog-1");
        result.CategoryId.Should().Be("cat-1");
        result.Code.Should().Be("SKU-1");
        result.ProductId.Should().Be("prod-1");
        result.Quantity.Should().Be(3);
        result.Price.Should().Be(80m);
    }

    [Fact]
    public void ToProductPromoEntry_NullSource_ReturnsNull()
    {
        _mapper.ToProductPromoEntry(null).Should().BeNull();
    }

    [Fact]
    public void ToPriceEvaluationContext_FromCartProductsRequest_MapsStoreAndCurrency()
    {
        var request = new CartProductsRequest
        {
            CultureName = "en-US",
            Store = new StoreModule.Core.Model.Store { Id = "store-1", Catalog = "catalog-1" },
            Currency = new Currency(CoreModule.Core.Common.Language.InvariantLanguage, "USD"),
        };

        var result = _mapper.ToPriceEvaluationContext(request);

        result.Language.Should().Be("en-US");
        result.StoreId.Should().Be("store-1");
        result.CatalogId.Should().Be("catalog-1");
        result.Currency.Should().Be("USD");
        result.CustomerId.Should().BeNull();
    }

    [Fact]
    public void ToPriceEvaluationContext_FromCartProductsRequest_WithMember_MapsCustomerAndAddress()
    {
        var request = new CartProductsRequest
        {
            CultureName = "en-US",
            Store = new StoreModule.Core.Model.Store { Id = "store-1", Catalog = "catalog-1" },
            Currency = new Currency(CoreModule.Core.Common.Language.InvariantLanguage, "USD"),
            Member = new Contact
            {
                Id = "member-1",
                Groups = ["vip"],
                Addresses =
                [
                    new CustomerModule.Core.Model.Address { AddressType = CoreModule.Core.Common.AddressType.Shipping, City = "Seattle", CountryCode = "US", RegionName = "WA", PostalCode = "98101" },
                ],
            },
        };

        var result = _mapper.ToPriceEvaluationContext(request);

        result.CustomerId.Should().Be("member-1");
        result.GeoCity.Should().Be("Seattle");
        result.GeoCountry.Should().Be("US");
        result.GeoState.Should().Be("WA");
        result.GeoZipCode.Should().Be("98101");
        result.UserGroups.Should().BeEquivalentTo(["vip"]);
    }

    [Fact]
    public void ToPriceEvaluationContext_FromCartProductsRequest_NullSource_ReturnsNull()
    {
        _mapper.ToPriceEvaluationContext((CartProductsRequest)null).Should().BeNull();
    }

    [Fact]
    public void ToPriceEvaluationContext_FromCartAggregate_NullSource_ReturnsNull()
    {
        _mapper.ToPriceEvaluationContext((CartAggregate)null).Should().BeNull();
    }

    [Fact]
    public void ToTaxEvaluationContext_FromCartAggregate_NullSource_ReturnsNull()
    {
        _mapper.ToTaxEvaluationContext(null).Should().BeNull();
    }

    [Fact]
    public void ToTaxAddress_CopiesAllFields()
    {
        var source = new CartAddress
        {
            Name = "John Doe",
            City = "Seattle",
            CountryCode = "US",
            RegionName = "WA",
            PostalCode = "98101",
            Line1 = "123 Main St",
        };

        var result = _mapper.ToTaxAddress(source);

        result.Name.Should().Be("John Doe");
        result.City.Should().Be("Seattle");
        result.CountryCode.Should().Be("US");
        result.RegionName.Should().Be("WA");
        result.PostalCode.Should().Be("98101");
        result.Line1.Should().Be("123 Main St");
    }

    [Fact]
    public void ToTaxAddress_NullSource_ReturnsNull()
    {
        _mapper.ToTaxAddress(null).Should().BeNull();
    }
}
