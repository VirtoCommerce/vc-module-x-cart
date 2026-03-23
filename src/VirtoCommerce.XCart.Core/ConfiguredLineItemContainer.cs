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

        public virtual void AddProductSectionLineItem(CartProduct cartProduct, int quantity, string sectionId, string type = ConfigurationSectionTypeProduct)
        {
            AddProductSectionLineItem(cartProduct, quantity, sectionId, type, selectedForCheckout: true);
        }

        public virtual void AddProductSectionLineItem(CartProduct cartProduct, ConfigurationItem configurationItem)
        {
            AddProductSectionLineItem(cartProduct, configurationItem.Quantity, configurationItem.SectionId, configurationItem.Type, configurationItem.SelectedForCheckout);
        }

        protected virtual void AddProductSectionLineItem(CartProduct cartProduct, int quantity, string sectionId, string type, bool selectedForCheckout)
        {
            var lineItem = CreateLineItem(cartProduct, quantity);
            lineItem.SelectedForCheckout = selectedForCheckout;

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

        public object Clone()
        {
            return MemberwiseClone();
        }

        protected class SectionLineItem
        {
            public string SectionId { get; set; }
            public LineItem Item { get; set; }
            public string CustomText { get; set; }
            public string Type { get; set; }
            public IList<ConfigurationItemFile> Files { get; set; } = [];
        }
    }
}
