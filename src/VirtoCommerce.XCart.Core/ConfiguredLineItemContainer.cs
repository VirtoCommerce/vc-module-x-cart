using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
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

        /// <summary>
        /// Builder used to construct <see cref="LineItem"/> instances inside this container's
        /// <see cref="CreateLineItem(CartProduct, int)"/> and <see cref="CreateConfiguredLineItem(int)"/>
        /// methods, and <see cref="ConfigurationItem"/> sub-items inside
        /// <see cref="CreateConfigurationItem(SectionLineItem)"/>. Populated by every
        /// <see cref="ConfiguredLineItemContainer"/> construction site (in-tree: <see cref="CartAggregate"/>,
        /// <c>ChangeCartCurrencyCommandHandler</c>, <c>SavedForLaterListService</c>,
        /// <c>ConfiguredLineItemContainerService</c>). When null, the container falls back to
        /// <see cref="AbstractTypeFactory{T}.TryCreateInstance()"/> to preserve behaviour for
        /// external direct-instantiation call sites that bypass DI.
        /// </summary>
        public ICartItemBuilder CartItemBuilder { get; set; }

        protected List<SectionLineItem> Items { get; } = [];

        public virtual LineItem CreateLineItem(CartProduct cartProduct, int quantity)
        {
            var lineItem = CartItemBuilder?.Create(cartProduct) ?? AbstractTypeFactory<LineItem>.TryCreateInstance();
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
            AddProductSectionLineItem(cartProduct, quantity, selectedForCheckout: true, sectionId, type);
        }

        public virtual void AddProductSectionLineItem(CartProduct cartProduct, int quantity, bool selectedForCheckout, string sectionId, string type = ConfigurationSectionTypeProduct)
        {
            var lineItem = CreateLineItem(cartProduct, quantity);
            lineItem.SelectedForCheckout = selectedForCheckout;

            AddProductSectionLineItem(lineItem, sectionId, type);
        }

        protected virtual void AddProductSectionLineItem(LineItem lineItem, string sectionId, string type)
        {
            var item = CreateSectionLineItem(sectionId, type);
            item.Item = lineItem;

            Items.Add(item);
        }

        /// <summary>
        /// Adds a product section line item for an existing configuration item (e.g. during price recalculation).
        /// Propagates <see cref="ConfigurationItem.SelectedForCheckout"/> and uses
        /// <see cref="CreateLineItem(CartProduct, ConfigurationItem)"/> for pricing — override
        /// to inject pre-computed prices instead of catalog prices. Stores
        /// <paramref name="configurationItem"/> on <see cref="SectionLineItem.Source"/>
        /// for downstream consumers.
        /// </summary>
        public virtual void AddProductSectionLineItem(CartProduct cartProduct, ConfigurationItem configurationItem)
        {
            var lineItem = CreateLineItem(cartProduct, configurationItem);

            var item = CreateSectionLineItem(configurationItem);
            item.Item = lineItem;

            Items.Add(item);
        }

        public virtual void AddTextSectionLineItem(string customText, string sectionId)
        {
            var item = CreateSectionLineItem(sectionId, ConfigurationSectionTypeText);
            item.CustomText = customText;

            Items.Add(item);
        }

        /// <summary>
        /// Adds a text section line item for an existing configuration item. Stores
        /// <paramref name="configurationItem"/> on <see cref="SectionLineItem.Source"/>
        /// for downstream consumers.
        /// </summary>
        public virtual void AddTextSectionLineItem(ConfigurationItem configurationItem)
        {
            var item = CreateSectionLineItem(configurationItem);

            Items.Add(item);
        }

        public virtual void AddFileSectionLineItem(IList<ConfigurationItemFile> files, string sectionId)
        {
            var item = CreateSectionLineItem(sectionId, ConfigurationSectionTypeFile);
            item.Files = files;

            Items.Add(item);
        }

        /// <summary>
        /// Adds a file section line item for an existing configuration item. Stores
        /// <paramref name="configurationItem"/> on <see cref="SectionLineItem.Source"/>
        /// for downstream consumers.
        /// </summary>
        /// <param name="configurationItem">Source configuration item; its
        /// <see cref="ConfigurationItem.Files"/> are used by default.</param>
        /// <param name="files">Optional override for the file list. Pass a non-null value
        /// when the files must be transformed before being added (e.g. duplicated for a
        /// different currency context). When <c>null</c>, <c>configurationItem.Files</c>
        /// is used.</param>
        public virtual void AddFileSectionLineItem(ConfigurationItem configurationItem, IList<ConfigurationItemFile> files = null)
        {
            var item = CreateSectionLineItem(configurationItem);
            if (files is not null)
            {
                item.Files = files;
            }

            Items.Add(item);
        }

        /// <summary>
        /// Creates a <see cref="SectionLineItem"/> initialized from <paramref name="configurationItem"/>.
        /// Override to populate additional fields from a derived <see cref="ConfigurationItem"/> type.
        /// </summary>
        protected virtual SectionLineItem CreateSectionLineItem(ConfigurationItem configurationItem)
        {
            var item = CreateSectionLineItem(configurationItem.SectionId, configurationItem.Type);

            item.CustomText = configurationItem.CustomText;
            item.Files = configurationItem.Files ?? [];
            item.Source = configurationItem;

            return item;
        }

        /// <summary>
        /// Creates an empty <see cref="SectionLineItem"/> with the given identifying fields.
        /// Used by both creation-path and source-aware overloads. Override to return a
        /// derived <see cref="SectionLineItem"/> type — <see cref="SectionLineItem"/> is
        /// nested-protected, so external <see cref="AbstractTypeFactory{T}"/> registration
        /// is not reachable; subclassing <see cref="ConfiguredLineItemContainer"/> is the
        /// supported extension point.
        /// </summary>
        protected virtual SectionLineItem CreateSectionLineItem(string sectionId, string type)
        {
            return new SectionLineItem
            {
                SectionId = sectionId,
                Type = type,
            };
        }

        /// <summary>
        /// Builds the final <see cref="ConfigurationItem"/> for a section during
        /// <see cref="CreateConfiguredLineItem(int)"/> sub-item materialization. Routes
        /// instantiation through <see cref="CartItemBuilder"/> so subtype dispatch
        /// (e.g. <c>AbstractTypeFactory&lt;ConfigurationItem&gt;</c> overrides) applies; falls
        /// back to <see cref="AbstractTypeFactory{T}.TryCreateInstance()"/> when the builder
        /// is null. Override to add section-aware fields or subtype selection that depends on
        /// context not visible to <see cref="ICartItemBuilder"/> (e.g. section's product type).
        /// </summary>
        protected virtual ConfigurationItem CreateConfigurationItem(SectionLineItem section)
        {
            var subItem = CartItemBuilder?.Create(section.SectionId, section.Type)
                ?? AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();

            subItem.SectionId = section.SectionId;
            subItem.Type = section.Type;
            subItem.CatalogId = section.Item?.CatalogId;
            subItem.CategoryId = section.Item?.CategoryId;
            subItem.ProductId = section.Item?.ProductId;
            subItem.Name = section.Item?.Name;
            subItem.Sku = section.Item?.Sku;
            subItem.ImageUrl = section.Item?.ImageUrl;
            subItem.Quantity = section.Item?.Quantity ?? 1;
            if (section.Type is ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation)
            {
                subItem.ListPrice = section.Item?.ListPrice ?? 0m;
                subItem.SalePrice = section.Item?.SalePrice ?? 0m;
                subItem.SelectedForCheckout = section.Item?.SelectedForCheckout ?? true;
            }
            subItem.CustomText = section.CustomText;
            subItem.Files = section.Files;

            return subItem;
        }

        public virtual ExpConfigurationLineItem CreateConfiguredLineItem(int quantity)
        {
            var lineItem = CartItemBuilder?.Create(ConfigurableProduct) ?? AbstractTypeFactory<LineItem>.TryCreateInstance();

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
                .Select(CreateConfigurationItem)
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

            /// <summary>
            /// Reference to the source <see cref="ConfigurationItem"/> from which this section line
            /// item was built, when one exists. <c>null</c> for items added via creation-path
            /// overloads (e.g. <see cref="AddTextSectionLineItem(string, string)"/>) where the
            /// configuration item does not yet exist; non-null for items added via the source-aware
            /// overloads (e.g. <see cref="AddTextSectionLineItem(ConfigurationItem)"/>).
            /// </summary>
            public ConfigurationItem Source { get; set; }
        }
    }
}
