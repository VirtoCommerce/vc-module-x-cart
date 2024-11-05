using System.Linq;
using AutoMapper;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.MarketingModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.TaxModule.Core.Services;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core
{
    public class ConfiguredLineItemAggregate : CartAggregate
    {
        public ConfiguredLineItemAggregate(
            IMarketingPromoEvaluator marketingEvaluator,
            IShoppingCartTotalsCalculator cartTotalsCalculator,
            ITaxProviderSearchService taxProviderSearchService,
            ICartProductService cartProductService,
            IDynamicPropertyUpdaterService dynamicPropertyUpdaterService,
            IMapper mapper,
            IMemberService memberService,
            IGenericPipelineLauncher pipeline)
            : base(marketingEvaluator, cartTotalsCalculator, taxProviderSearchService, cartProductService, dynamicPropertyUpdaterService, mapper, memberService, pipeline)
        {
        }

        public CartProduct ConfigurableProduct { get; set; }

        public LineItem GetConfiguredLineItem()
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();

            lineItem.IsConfigured = true;

            lineItem.CatalogId = ConfigurableProduct.Product.CatalogId;
            lineItem.CategoryId = ConfigurableProduct.Product.CategoryId;

            lineItem.Quantity = 1;
            lineItem.ProductId = ConfigurableProduct.Product.Id;
            lineItem.Sku = $"Configuration-{ConfigurableProduct.Product.Code}";

            lineItem.Name = ConfigurableProduct.Product.Name;
            lineItem.ImageUrl = ConfigurableProduct.Product.ImgSrc;
            lineItem.ProductOuterId = ConfigurableProduct.Product.OuterId;
            lineItem.ProductType = ConfigurableProduct.Product.ProductType;
            lineItem.TaxType = ConfigurableProduct.Product.TaxType;

            lineItem.FulfillmentCenterId = ConfigurableProduct.Inventory?.FulfillmentCenterId;
            lineItem.FulfillmentCenterName = ConfigurableProduct.Inventory?.FulfillmentCenterName;
            lineItem.VendorId = ConfigurableProduct.Product.Vendor;

            // prices
            lineItem.Currency = Cart.Currency;
            lineItem.ListPrice = Cart.Items.Sum(x => x.ListPrice * x.Quantity);
            lineItem.SalePrice = Cart.Items.Sum(x => x.SalePrice * x.Quantity);
            lineItem.DiscountAmount = Cart.Items.Sum(x => x.DiscountAmount);

            lineItem.TaxPercentRate = Cart.TaxPercentRate;
            lineItem.TaxDetails = Cart.Items.SelectMany(x => x.TaxDetails).ToList();
            lineItem.Discounts = Cart.Items.SelectMany(x => x.Discounts).ToList();

            // create subitems
            lineItem.ConfigurationItems = Cart.Items
                .Select(x =>
                {
                    var subItem = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();

                    subItem.ProductId = x.ProductId;
                    subItem.Name = x.Name;
                    subItem.Sku = x.Sku;
                    subItem.ImageUrl = x.ImageUrl;
                    subItem.Quantity = x.Quantity;
                    subItem.CatalogId = x.CatalogId;
                    subItem.CategoryId = x.CategoryId;

                    return subItem;
                })
                .ToList();

            return lineItem;
        }
    }
}
