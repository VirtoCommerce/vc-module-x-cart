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

        private readonly List<SectionLineItem> _items = [];

        public virtual LineItem CreateLineItem(CartProduct cartProduct, int quantity)
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();
            lineItem.ProductId = cartProduct.Id;
            lineItem.Name = cartProduct.GetName(CultureName);
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

            return lineItem;
        }

        public virtual void AddProductSectionLineItem(CartProduct cartProduct, int quantity, string sectionId)
        {
            var lineItem = CreateLineItem(cartProduct, quantity);

            _items.Add(new SectionLineItem
            {
                SectionId = sectionId,
                Type = ConfigurationSectionTypeProduct,
                Item = lineItem,
            });
        }

        public virtual void AddTextSectionLineItem(string customText, string sectionId)
        {
            _items.Add(new SectionLineItem
            {
                SectionId = sectionId,
                Type = ConfigurationSectionTypeText,
                CustomText = customText,
            });
        }

        public virtual void AddFileSectionLineItem(IList<ConfigurationItemFile> files, string sectionId)
        {
            _items.Add(new SectionLineItem
            {
                SectionId = sectionId,
                Type = ConfigurationSectionTypeFile,
                Files = files,
            });
        }

        public virtual ExpConfigurationLineItem CreateConfiguredLineItem(int quantity)
        {
            var lineItem = AbstractTypeFactory<LineItem>.TryCreateInstance();

            lineItem.IsConfigured = true;
            lineItem.Quantity = quantity;

            lineItem.Discounts = [];
            lineItem.TaxDetails = [];

            lineItem.ProductId = ConfigurableProduct.Product.Id;
            lineItem.Sku = $"Configuration-{ConfigurableProduct.Product.Code}";

            lineItem.CatalogId = ConfigurableProduct.Product.CatalogId;
            lineItem.CategoryId = ConfigurableProduct.Product.CategoryId;

            lineItem.Name = ConfigurableProduct.GetName(CultureName);
            lineItem.ImageUrl = ConfigurableProduct.Product.ImgSrc;
            lineItem.ProductOuterId = ConfigurableProduct.Product.OuterId;
            lineItem.ProductType = ConfigurableProduct.Product.ProductType;
            lineItem.TaxType = ConfigurableProduct.Product.TaxType;

            lineItem.FulfillmentCenterId = ConfigurableProduct.Inventory?.FulfillmentCenterId;
            lineItem.FulfillmentCenterName = ConfigurableProduct.Inventory?.FulfillmentCenterName;
            lineItem.VendorId = ConfigurableProduct.Product.Vendor;

            // create sub items
            lineItem.ConfigurationItems = _items
                .Select(x =>
                {
                    var subItem = AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();

                    subItem.SectionId = x.SectionId;
                    subItem.ProductId = x.Item?.ProductId;
                    subItem.Name = x.Item?.Name;
                    subItem.Sku = x.Item?.Sku;
                    subItem.ImageUrl = x.Item?.ImageUrl;
                    subItem.Quantity = x.Item?.Quantity ?? 1;
                    subItem.CatalogId = x.Item?.CatalogId;
                    subItem.CategoryId = x.Item?.CategoryId;
                    subItem.Type = x.Type;
                    subItem.CustomText = x.CustomText;
                    subItem.Files = x.Files;

                    return subItem;
                })
                .ToList();

            // prices
            lineItem.Currency = Currency.Code;

            UpdatePrice(lineItem);

            return new ExpConfigurationLineItem
            {
                Id = lineItem.Id,
                Quantity = lineItem.Quantity,
                Item = lineItem,
                Currency = Currency,
                CultureName = CultureName,
                UserId = UserId,
                StoreId = Store.Id,
            };
        }

        public virtual void UpdatePrice(LineItem lineItem)
        {
            var configurableProductPrice = ConfigurableProduct.Price ?? new Xapi.Core.Models.ProductPrice(Currency);
            var items = _items.Where(x => x.Item != null).Select(x => x.Item).ToArray();

            lineItem.ListPrice = items.Sum(x => x.ListPrice * x.Quantity) + configurableProductPrice.ListPrice.Amount;
            lineItem.SalePrice = items.Sum(x => x.SalePrice * x.Quantity) + configurableProductPrice.SalePrice.Amount;
            lineItem.DiscountAmount = items.Sum(x => x.DiscountAmount) + configurableProductPrice.DiscountAmount.Amount;
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

        private sealed class SectionLineItem
        {
            public string SectionId { get; set; }
            public LineItem Item { get; set; }
            public string CustomText { get; set; }
            public string Type { get; set; }
            public IList<ConfigurationItemFile> Files { get; set; } = [];
        }
    }
}
