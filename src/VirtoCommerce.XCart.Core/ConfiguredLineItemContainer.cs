using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCatalog.Core.Models;

namespace VirtoCommerce.XCart.Core
{
    public class ExpConfigurationLineItem
    {
        public LineItem Item { get; set; }
        public ExpProduct Product { get; set; }
        public Currency Currency { get; set; }
    }

    public class ConfiguredLineItemContainer : ICartProductContainer, ICloneable
    {
        public Currency Currency { get; set; }
        public Store Store { get; set; }
        public Member Member { get; set; }
        public string CultureName { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public IList<string> ProductsIncludeFields { get; set; }

        public CartProduct ConfigurableProduct { get; set; }
        public IList<LineItem> Items { get; set; } = new List<LineItem>();

        private readonly IMapper _mapper;

        public ConfiguredLineItemContainer(IMapper mapper)
        {
            _mapper = mapper;
        }

        public LineItem CreateItem(CartProduct cartProduct, int quantity)
        {
            var lineItem = _mapper.Map<LineItem>(cartProduct);

            lineItem.Quantity = quantity;

            // calculate prices and only static rewards
            if (cartProduct.Price != null)
            {
                var tierPrice = cartProduct.Price.GetTierPrice(quantity);
                if (tierPrice.Price.Amount > 0)
                {
                    lineItem.SalePrice = tierPrice.ActualPrice.Amount;
                    lineItem.ListPrice = tierPrice.Price.Amount;
                }

                lineItem.DiscountAmount = Math.Max(0, lineItem.ListPrice - lineItem.SalePrice);
                lineItem.PlacedPrice = lineItem.ListPrice - lineItem.DiscountAmount;
                lineItem.ExtendedPrice = lineItem.PlacedPrice * lineItem.Quantity;
            }

            return lineItem;
        }

        public LineItem CreateConfiguredLineItem()
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();

            lineItem.IsConfigured = true;
            lineItem.Quantity = 1;

            lineItem.Discounts = [];
            lineItem.TaxDetails = [];

            lineItem.ProductId = ConfigurableProduct.Product.Id;
            lineItem.Sku = $"Configuration-{ConfigurableProduct.Product.Code}";

            lineItem.CatalogId = ConfigurableProduct.Product.CatalogId;
            lineItem.CategoryId = ConfigurableProduct.Product.CategoryId;

            lineItem.Name = ConfigurableProduct.Product.Name;
            lineItem.ImageUrl = ConfigurableProduct.Product.ImgSrc;
            lineItem.ProductOuterId = ConfigurableProduct.Product.OuterId;
            lineItem.ProductType = ConfigurableProduct.Product.ProductType;
            lineItem.TaxType = ConfigurableProduct.Product.TaxType;

            lineItem.FulfillmentCenterId = ConfigurableProduct.Inventory?.FulfillmentCenterId;
            lineItem.FulfillmentCenterName = ConfigurableProduct.Inventory?.FulfillmentCenterName;
            lineItem.VendorId = ConfigurableProduct.Product.Vendor;

            // prices
            lineItem.Currency = Currency.Code;

            if (ConfigurableProduct.Price == null)
            {
                ConfigurableProduct.Price = new Xapi.Core.Models.ProductPrice(Currency);
            }

            lineItem.ListPrice = Items.Sum(x => x.ListPrice) + ConfigurableProduct.Price.ListPrice.Amount;
            lineItem.SalePrice = Items.Sum(x => x.SalePrice) + ConfigurableProduct.Price.SalePrice.Amount;

            lineItem.DiscountAmount = Math.Max(0, lineItem.ListPrice - lineItem.SalePrice);
            lineItem.PlacedPrice = lineItem.ListPrice - lineItem.DiscountAmount;
            lineItem.ExtendedPrice = lineItem.PlacedPrice * lineItem.Quantity;

            // create sub items
            lineItem.ConfigurationItems = Items
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

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}