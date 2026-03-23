using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Core
{
    public class ConfiguredLineItemContainer : ICloneable
    {
        public Currency Currency { get; set; }
        public Store Store { get; set; }
        public Member Member { get; set; }
        public string CultureName { get; set; }
        public string UserId { get; set; }
        public IList<string> ProductsIncludeFields { get; set; }

        public CartProduct ConfigurableProduct { get; set; }

        protected List<SectionLineItem> Items { get; } = [];

        public virtual LineItem CreateLineItem(CartProduct cartProduct, int quantity)
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();
            lineItem.ProductId = cartProduct.Id;
            lineItem.Name = cartProduct.GetName(CultureName);
            lineItem.Sku = cartProduct.Product.Code;
            lineItem.ImageUrl = cartProduct.Product.ImgSrc;
            lineItem.CatalogId = cartProduct.Product.CatalogId;
            lineItem.CategoryId = cartProduct.Product.CategoryId;
            lineItem.DynamicProperties = [];

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

            return lineItem;
        }

        /// <summary>
        /// Creates a <see cref="LineItem"/> for a configuration item product.
        /// Override to customize pricing — e.g. use a pre-computed price from
        /// <paramref name="configurationItem"/> instead of the catalog product price.
        /// </summary>
        public virtual LineItem CreateLineItem(CartProduct cartProduct, ConfigurationItem configurationItem)
        {
            var lineItem = CreateLineItem(cartProduct, configurationItem.Quantity);
            lineItem.SelectedForCheckout = configurationItem.SelectedForCheckout;

            return lineItem;
        }

        /// <summary>
        /// Adds a product section line item for a new configuration item (e.g. from a GraphQL mutation).
        /// Prices are loaded from the catalog product.
        /// </summary>
        public virtual void AddProductSectionLineItem(CartProduct cartProduct, int quantity, string sectionId, string type = ConfigurationSectionTypeProduct)
        {
            var lineItem = CreateLineItem(cartProduct, quantity);

            AddProductSectionLineItem(lineItem, sectionId, type);
        }

        /// <summary>
        /// Adds a product section line item for an existing configuration item (e.g. during price recalculation).
        /// Propagates <see cref="ConfigurationItem.SelectedForCheckout"/> and uses
        /// <see cref="CreateLineItem(CartProduct, ConfigurationItem)"/> for pricing — override
        /// to inject pre-computed prices instead of catalog prices.
        /// </summary>
        public virtual void AddProductSectionLineItem(CartProduct cartProduct, ConfigurationItem configurationItem)
        {
            var lineItem = CreateLineItem(cartProduct, configurationItem);

            AddProductSectionLineItem(lineItem, configurationItem.SectionId, configurationItem.Type);
        }

        protected virtual void AddProductSectionLineItem(LineItem lineItem, string sectionId, string type)
        {
            var item = AbstractTypeFactory<SectionLineItem>.TryCreateInstance();
            item.SectionId = sectionId;
            item.Type = type;
            item.Item = lineItem;

            Items.Add(item);
        }

        public virtual void AddTextSectionLineItem(string customText, string sectionId)
        {
            var item = AbstractTypeFactory<SectionLineItem>.TryCreateInstance();
            item.SectionId = sectionId;
            item.Type = ConfigurationSectionTypeText;
            item.CustomText = customText;

            Items.Add(item);
        }

        public virtual void AddFileSectionLineItem(IList<ConfigurationItemFile> files, string sectionId)
        {
            var item = AbstractTypeFactory<SectionLineItem>.TryCreateInstance();
            item.SectionId = sectionId;
            item.Type = ConfigurationSectionTypeFile;
            item.Files = files;

            Items.Add(item);
        }

        public virtual ExpConfigurationLineItem CreateConfiguredLineItem(int quantity)
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();

            lineItem.IsConfigured = true;
            lineItem.Quantity = quantity;

            lineItem.Discounts = [];
            lineItem.TaxDetails = [];
            lineItem.DynamicProperties = [];

            if (ConfigurableProduct?.Product is { } product)
            {
                lineItem.ProductId = product.Id;
                lineItem.Sku = $"Configuration-{product.Code}";

                lineItem.CatalogId = product.CatalogId;
                lineItem.CategoryId = product.CategoryId;

                lineItem.Name = ConfigurableProduct.GetName(CultureName);
                lineItem.ImageUrl = product.ImgSrc;
                lineItem.ProductOuterId = product.OuterId;
                lineItem.ProductType = product.ProductType;
                lineItem.TaxType = product.TaxType;

                lineItem.FulfillmentCenterId = ConfigurableProduct.Inventory?.FulfillmentCenterId;
                lineItem.FulfillmentCenterName = ConfigurableProduct.Inventory?.FulfillmentCenterName;
                lineItem.VendorId = product.Vendor;
            }

            // create sub items
            lineItem.ConfigurationItems = Items
                .Select(x =>
                {
                    var subItem = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();

                    subItem.SectionId = x.SectionId;
                    subItem.Type = x.Type;
                    subItem.CatalogId = x.Item?.CatalogId;
                    subItem.CategoryId = x.Item?.CategoryId;
                    subItem.ProductId = x.Item?.ProductId;
                    subItem.Name = x.Item?.Name;
                    subItem.Sku = x.Item?.Sku;
                    subItem.ImageUrl = x.Item?.ImageUrl;
                    subItem.Quantity = x.Item?.Quantity ?? 1;
                    if (x.Type is ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation)
                    {
                        subItem.ListPrice = x.Item?.ListPrice ?? 0m;
                        subItem.SalePrice = x.Item?.SalePrice ?? 0m;
                    }
                    subItem.CustomText = x.CustomText;
                    subItem.Files = x.Files;

                    return subItem;
                })
                .ToList();

            // prices
            lineItem.Currency = Currency.Code;

            UpdatePrice(lineItem);

            var result = AbstractTypeFactory<ExpConfigurationLineItem>.TryCreateInstance();
            result.Id = lineItem.Id;
            result.Quantity = lineItem.Quantity;
            result.Item = lineItem;
            result.Currency = Currency;
            result.CultureName = CultureName;
            result.UserId = UserId;
            result.StoreId = Store.Id;

            return result;
        }

        public virtual void UpdatePrice(LineItem lineItem)
        {
            var configurableProductPrice = ConfigurableProduct?.Price ?? new Xapi.Core.Models.ProductPrice(Currency);
            var items = Items.Where(x => x.Item is { SelectedForCheckout: true }).Select(x => x.Item).ToArray();

            lineItem.ListPrice = items.Sum(x => x.ListPrice * x.Quantity) + configurableProductPrice.ListPrice.Amount;
            lineItem.SalePrice = items.Sum(x => x.SalePrice * x.Quantity) + configurableProductPrice.SalePrice.Amount;
            lineItem.DiscountAmount = items.Sum(x => x.DiscountAmount * x.Quantity) + configurableProductPrice.DiscountAmount.Amount;
            lineItem.PlacedPrice = lineItem.ListPrice - lineItem.DiscountAmount;
            lineItem.ExtendedPrice = lineItem.PlacedPrice * lineItem.Quantity;
        }

        public virtual CartProductsRequest GetCartProductsRequest()
        {
            var request = AbstractTypeFactory<CartProductsRequest>.TryCreateInstance();

            request.Store = Store;
            request.Currency = Currency;
            request.CultureName = CultureName;
            request.Member = Member;
            request.UserId = UserId;

            return request;
        }

        /// <summary>
        /// Syncs adjusted prices from the container's internal items back to the
        /// original <see cref="ConfigurationItem"/> objects on the line item.
        /// Matches by Type + SectionId, and additionally by ProductId for Product/Variation sections
        /// (required for multi-product sections with multiple items in the same section).
        /// </summary>
        public virtual void SyncConfigurationPrices(LineItem lineItem)
        {
            if (lineItem.ConfigurationItems.IsNullOrEmpty())
            {
                return;
            }

            foreach (var sectionLineItem in Items.Where(x => x.Item is not null))
            {
                var configurationItem = lineItem.ConfigurationItems.FirstOrDefault(x =>
                    x.SectionId == sectionLineItem.SectionId &&
                    x.Type == sectionLineItem.Type &&
                    (x.Type is not (ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation) || x.ProductId == sectionLineItem.Item.ProductId));

                if (configurationItem is not null)
                {
                    configurationItem.ListPrice = sectionLineItem.Item.ListPrice;
                    configurationItem.SalePrice = sectionLineItem.Item.SalePrice;
                }
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        protected class SectionLineItem
        {
            public string SectionId { get; set; }
            public string Type { get; set; }
            public LineItem Item { get; set; }
            public string CustomText { get; set; }
            public IList<ConfigurationItemFile> Files { get; set; } = [];
        }
    }
}
