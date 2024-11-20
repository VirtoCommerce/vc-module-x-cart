using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core
{
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

        public LineItem AddItem(CartProduct cartProduct, int quantity)
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();
            lineItem.ProductId = cartProduct.Id;
            lineItem.Name = cartProduct.Product.Name;
            lineItem.Sku = cartProduct.Product.Code;
            lineItem.ImageUrl = cartProduct.Product.ImgSrc;
            lineItem.CatalogId = cartProduct.Product.CatalogId;
            lineItem.CategoryId = cartProduct.Product.CategoryId;

            lineItem.Quantity = quantity;

            // calculate prices and only static rewards
            if (cartProduct.Price != null)
            {
                lineItem.Currency = cartProduct.Price.Currency.Code;

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

            Items.Add(lineItem);

            return lineItem;
        }

        public ExpConfigurationLineItem CreateConfiguredLineItem()
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

            // prices
            lineItem.Currency = Currency.Code;

            UpdatePrice(lineItem);

            return new ExpConfigurationLineItem
            {
                Item = lineItem,
                Currency = Currency,
                CultureName = CultureName,
                UserId = UserId,
                StoreId = Store.Id,
            };
        }

        public void UpdatePrice(LineItem lineItem)
        {
            var configurableProductPrice = ConfigurableProduct.Price ?? new Xapi.Core.Models.ProductPrice(Currency);

            lineItem.ListPrice = Items.Sum(x => x.ListPrice * x.Quantity) + configurableProductPrice.ListPrice.Amount;
            lineItem.SalePrice = Items.Sum(x => x.SalePrice * x.Quantity) + configurableProductPrice.SalePrice.Amount;

            lineItem.DiscountAmount = Math.Max(0, lineItem.ListPrice - lineItem.SalePrice);
            lineItem.PlacedPrice = lineItem.ListPrice - lineItem.DiscountAmount;
            lineItem.ExtendedPrice = lineItem.PlacedPrice * lineItem.Quantity;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
