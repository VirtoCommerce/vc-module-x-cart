using System.Collections.Generic;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using Store = VirtoCommerce.StoreModule.Core.Model.Store;

namespace VirtoCommerce.XCart.Tests.Services;

public class CartAggregateMappingExtensionsTests : XCartMoqHelper
{
    private CartAggregate BuildAggregate(ShoppingCart cart, Member member = null)
    {
        var aggregate = new CartAggregate(
            _marketingPromoEvaluatorMock.Object,
            new Moq.Mock<CartModule.Core.Services.IShoppingCartTotalsCalculator>().Object,
            _taxProviderSearchServiceMock.Object,
            _cartProductServiceMock.Object,
            _dynamicPropertyUpdaterService.Object,
            _mapperMock.Object,
            _memberService.Object,
            _genericPipelineLauncherMock.Object,
            _fileUploadService.Object,
            _cartSharingService.Object,
            _cartValidationContextFactoryMock.Object,
            _cartItemBuilder,
            _cartValidatorRegistry);

        aggregate.GrabCart(cart, new Store { Id = "store-1", Catalog = "catalog-1" }, member ?? GetMember(), GetCurrency());

        return aggregate;
    }

    [Fact]
    public void ToPriceEvaluationContext_FromCartAggregate_MapsStoreCatalogCurrencyAndCustomer()
    {
        var cart = new ShoppingCart
        {
            StoreId = "store-1",
            LanguageCode = "en-US",
            OrganizationId = "org-1",
            Currency = CURRENCY_CODE,
            Items = [],
        };

        var member = new Contact
        {
            Id = "member-1",
            Groups = ["vip"],
            Addresses =
            [
                new CustomerModule.Core.Model.Address
                {
                    AddressType = VirtoCommerce.CoreModule.Core.Common.AddressType.Shipping,
                    City = "Seattle",
                    CountryCode = "US",
                    RegionName = "WA",
                    PostalCode = "98101",
                },
            ],
        };

        var aggregate = BuildAggregate(cart, member);
        var mapper = new XCartMapper();

        var result = mapper.ToPriceEvaluationContext(aggregate);

        result.Language.Should().Be("en-US");
        result.StoreId.Should().Be("store-1");
        result.CatalogId.Should().Be("catalog-1");
        result.Currency.Should().Be(CURRENCY_CODE);
        result.OrganizationId.Should().Be("org-1");
        result.CustomerId.Should().Be("member-1");
        result.GeoCity.Should().Be("Seattle");
        result.GeoCountry.Should().Be("US");
        result.GeoState.Should().Be("WA");
        result.GeoZipCode.Should().Be("98101");
        result.UserGroups.Should().BeEquivalentTo(["vip"]);
    }

    [Fact]
    public void ToTaxEvaluationContext_CreatesFreshInstance_PopulatedViaMapTo()
    {
        var cart = new ShoppingCart
        {
            StoreId = "store-1",
            Name = "default",
            CustomerId = "customer-1",
            Currency = CURRENCY_CODE,
            Items = [],
        };

        var aggregate = BuildAggregate(cart);
        var mapper = new XCartMapper();

        var result = mapper.ToTaxEvaluationContext(aggregate);

        result.Should().NotBeNull();
        result.StoreId.Should().Be("store-1");
        result.CustomerId.Should().Be("customer-1");
        result.Currency.Should().Be(CURRENCY_CODE);
    }

    [Fact]
    public void MapTo_TaxEvaluationContext_CopiesLineShipmentAndPaymentLines()
    {
        var cart = new ShoppingCart
        {
            StoreId = "store-1",
            Name = "default",
            CustomerId = "customer-1",
            Currency = CURRENCY_CODE,
            Items =
            [
                new LineItem
                {
                    Id = "line-1",
                    Sku = "SKU-1",
                    Name = "Widget",
                    TaxType = "TaxA",
                    Currency = CURRENCY_CODE,
                    ExtendedPrice = 100m,
                    SalePrice = 90m,
                    Quantity = 2,
                    PlacedPrice = 50m,
                    SelectedForCheckout = true,
                },
            ],
            Shipments =
            [
                new Shipment { Id = "ship-1", ShipmentMethodCode = "standard", ShipmentMethodOption = "opt", TaxType = "TaxB", Total = 15m, Price = 20m },
            ],
            Payments =
            [
                new Payment { Id = "pay-1", PaymentGatewayCode = "gateway-1", TaxType = "TaxC", Total = 5m, Price = 8m },
            ],
        };

        var aggregate = BuildAggregate(cart);
        var target = new TaxEvaluationContext();
        var mapper = new XCartMapper();

        aggregate.MapTo(target, mapper);

        target.StoreId.Should().Be("store-1");
        target.Code.Should().Be("default");
        target.Type.Should().Be("Cart");
        target.CustomerId.Should().Be("customer-1");
        target.Currency.Should().Be(CURRENCY_CODE);

        target.Lines.Should().HaveCount(3);

        var itemLine = target.Lines.Should().ContainSingle(x => x.TypeName == "item").Subject;
        itemLine.Id.Should().Be("line-1");
        itemLine.Code.Should().Be("SKU-1");
        itemLine.TaxType.Should().Be("TaxA");
        itemLine.Amount.Should().Be(100m);
        itemLine.Quantity.Should().Be(2);
        itemLine.Price.Should().Be(50m);

        var shipmentLine = target.Lines.Should().ContainSingle(x => x.TypeName == "shipment").Subject;
        shipmentLine.Id.Should().Be("ship-1");
        shipmentLine.Code.Should().Be("standard");
        shipmentLine.TaxType.Should().Be("TaxB");
        shipmentLine.Amount.Should().Be(15m);

        var paymentLine = target.Lines.Should().ContainSingle(x => x.TypeName == "payment").Subject;
        paymentLine.Id.Should().Be("pay-1");
        paymentLine.Code.Should().Be("gateway-1");
        paymentLine.TaxType.Should().Be("TaxC");
        paymentLine.Amount.Should().Be(5m);
    }

    [Fact]
    public void MapTo_TaxEvaluationContext_NullSourceOrTarget_DoesNothing()
    {
        var mapper = new XCartMapper();

        var target = new TaxEvaluationContext();
        ((CartAggregate)null).MapTo(target, mapper);
        target.Lines.Should().BeEmpty();

        var aggregate = BuildAggregate(new ShoppingCart { Currency = CURRENCY_CODE, Items = [] });
        aggregate.MapTo((TaxEvaluationContext)null, mapper);
    }

    [Fact]
    public void MapTo_TaxEvaluationContext_ShipmentDeliveryAddress_RoutedThroughMapperToTaxAddress()
    {
        var cart = new ShoppingCart
        {
            StoreId = "store-1",
            Name = "default",
            CustomerId = "customer-1",
            Currency = CURRENCY_CODE,
            Items = [],
            Shipments =
            [
                new Shipment
                {
                    Id = "ship-1",
                    ShipmentMethodCode = "standard",
                    DeliveryAddress = new CartModule.Core.Model.Address { Name = "Warehouse" },
                },
            ],
        };

        var aggregate = BuildAggregate(cart);
        var target = new TaxEvaluationContext();
        var mapper = new OverridingXCartMapper();

        aggregate.MapTo(target, mapper);

        target.Address.Should().NotBeNull();
        target.Address.Name.Should().Be("Warehouse");
        target.Address.Description.Should().Be("overridden-by-derived-mapper");
    }

    private class OverridingXCartMapper : XCartMapper
    {
        public override VirtoCommerce.TaxModule.Core.Model.Address ToTaxAddress(CartModule.Core.Model.Address source)
        {
            var result = base.ToTaxAddress(source);
            result.Description = "overridden-by-derived-mapper";
            return result;
        }

        public override ProductPromoEntry ToProductPromoEntry(LineItem source)
        {
            var result = base.ToProductPromoEntry(source);
            result.Discount = -1m;
            return result;
        }
    }

    [Fact]
    public void MapTo_PromotionEvaluationContext_CopiesCartFieldsAndPromoEntries()
    {
        var cart = new ShoppingCart
        {
            StoreId = "store-1",
            CustomerId = "customer-1",
            OrganizationId = "org-1",
            Currency = CURRENCY_CODE,
            LanguageCode = "en-US",
            Coupons = ["SAVE10"],
            SubTotal = 100m,
            Items =
            [
                new LineItem
                {
                    Id = "line-1",
                    ProductId = "prod-1",
                    Sku = "SKU-1",
                    CatalogId = "catalog-1",
                    CategoryId = "cat-1",
                    Currency = CURRENCY_CODE,
                    SalePrice = 80m,
                    Quantity = 2,
                    SelectedForCheckout = true,
                },
            ],
        };

        var aggregate = BuildAggregate(cart);
        var target = new PromotionEvaluationContext();
        var mapper = new XCartMapper();

        aggregate.MapTo(target, mapper);

        target.StoreId.Should().Be("store-1");
        target.CustomerId.Should().Be("customer-1");
        target.UserId.Should().Be("customer-1");
        target.OrganizationId.Should().Be("org-1");
        target.Currency.Should().Be(CURRENCY_CODE);
        target.Language.Should().Be("en-US");
        target.Coupons.Should().BeEquivalentTo(["SAVE10"]);
        target.CartTotal.Should().Be(100m);
        target.IsRegisteredUser.Should().BeTrue();
        target.IsEveryone.Should().BeTrue();

        var entry = target.CartPromoEntries.Should().ContainSingle().Subject;
        entry.ProductId.Should().Be("prod-1");
        entry.Code.Should().Be("SKU-1");
        entry.CatalogId.Should().Be("catalog-1");
        entry.CategoryId.Should().Be("cat-1");
        entry.Price.Should().Be(80m);
        entry.Quantity.Should().Be(2);

        target.PromoEntries.Should().BeSameAs(target.CartPromoEntries);
    }

    [Fact]
    public void MapTo_PromotionEvaluationContext_NullSourceOrTarget_DoesNothing()
    {
        var mapper = new XCartMapper();

        var target = new PromotionEvaluationContext();
        ((CartAggregate)null).MapTo(target, mapper);
        target.CartPromoEntries.Should().BeEmpty();

        var aggregate = BuildAggregate(new ShoppingCart { Currency = CURRENCY_CODE, Items = [] });
        aggregate.MapTo((PromotionEvaluationContext)null, mapper);
    }

    [Fact]
    public void MapTo_PromotionEvaluationContext_PromoEntries_RoutedThroughMapperToProductPromoEntry()
    {
        var cart = new ShoppingCart
        {
            StoreId = "store-1",
            CustomerId = "customer-1",
            Currency = CURRENCY_CODE,
            Items =
            [
                new LineItem
                {
                    Id = "line-1",
                    ProductId = "prod-1",
                    Sku = "SKU-1",
                    Currency = CURRENCY_CODE,
                    SalePrice = 80m,
                    Quantity = 2,
                    SelectedForCheckout = true,
                },
            ],
        };

        var aggregate = BuildAggregate(cart);
        var target = new PromotionEvaluationContext();
        var mapper = new OverridingXCartMapper();

        aggregate.MapTo(target, mapper);

        var entry = target.CartPromoEntries.Should().ContainSingle().Subject;
        entry.Discount.Should().Be(-1m);
    }
}

public class CartFilterMappingExtensionsTests
{
    [Fact]
    public void MapTo_TermFilter_SetsMatchingProperty()
    {
        var filters = new List<IFilter> { new TermFilter { FieldName = "customerId", Values = ["customer-1"] } };
        var criteria = new ShoppingCartSearchCriteria();

        filters.MapTo(criteria);

        criteria.CustomerId.Should().Be("customer-1");
    }

    [Fact]
    public void MapTo_NullFiltersOrCriteria_DoesNotThrow()
    {
        var criteria = new ShoppingCartSearchCriteria();

        FluentActions.Invoking(() => ((List<IFilter>)null).MapTo(criteria)).Should().NotThrow();
        FluentActions.Invoking(() => new List<IFilter>().MapTo(null)).Should().NotThrow();
    }
}
