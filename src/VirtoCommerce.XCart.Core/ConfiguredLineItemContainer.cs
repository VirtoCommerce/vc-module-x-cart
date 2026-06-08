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
            ArgumentNullException.ThrowIfNull(cartProduct);

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
        /// Adds a product configuration section line item for a new configuration item (e.g. from a GraphQL mutation).
        /// Prices are loaded from the catalog product. <paramref name="configurationSection"/> and
        /// <paramref name="cartProduct"/> are stored on the staging <see cref="SectionLineItem"/> so
        /// that materialize-time builder dispatch (<see cref="CreateConfigurationItem(SectionLineItem)"/>)
        /// has the configuration section + chosen-product context. Quantity and <c>SelectedForCheckout</c> are read
        /// from <paramref name="configurationSection"/>.<see cref="ProductConfigurationSection.Option"/>.
        /// </summary>
        public virtual void AddProductSectionLineItem(CartProduct cartProduct, ProductConfigurationSection configurationSection)
        {
            var lineItem = CreateLineItem(cartProduct, configurationSection.Option?.Quantity ?? 1);
            lineItem.SelectedForCheckout = configurationSection.Option?.SelectedForCheckout ?? true;

            var item = CreateSectionLineItem(configurationSection);
            item.Item = lineItem;
            item.CartProduct = cartProduct;

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
            item.CartProduct = cartProduct;

            Items.Add(item);
        }

        public virtual void AddTextSectionLineItem(ProductConfigurationSection configurationSection)
        {
            var item = CreateSectionLineItem(configurationSection);
            item.CustomText = configurationSection.CustomText;

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
            item.CustomText = configurationItem.CustomText;

            Items.Add(item);
        }

        public virtual void AddFileSectionLineItem(ProductConfigurationSection configurationSection, IList<ConfigurationItemFile> files)
        {
            var item = CreateSectionLineItem(configurationSection);
            item.Files = files;

            Items.Add(item);
        }

        /// <summary>
        /// Adds a file section line item for an existing configuration item. Stores
        /// <paramref name="configurationItem"/> on <see cref="SectionLineItem.Source"/>
        /// for downstream consumers.
        /// </summary>
        /// <param name="configurationItem">Source configuration item.</param>
        /// <param name="files">Optional override for the file list. Pass a non-null value
        /// when the files must be transformed before being added (e.g. duplicated for a
        /// different currency context). When <c>null</c>, the source's files are carried by
        /// the clone produced in <see cref="CreateConfigurationItem"/>; staging holds only the
        /// explicit override.</param>
        public virtual void AddFileSectionLineItem(ConfigurationItem configurationItem, IList<ConfigurationItemFile> files = null)
        {
            var item = CreateSectionLineItem(configurationItem);
            item.Files = files;

            Items.Add(item);
        }

        protected virtual SectionLineItem CreateSectionLineItem(ConfigurationItem configurationItem)
        {
            var item = CreateSectionLineItem();

            item.SectionId = configurationItem.SectionId;
            item.SectionName = configurationItem.SectionName;
            item.Type = configurationItem.Type;
            item.Source = configurationItem;

            return item;
        }

        protected virtual SectionLineItem CreateSectionLineItem(ProductConfigurationSection configurationSection)
        {
            var item = CreateSectionLineItem();

            item.SectionId = configurationSection.SectionId;
            item.SectionName = configurationSection.SectionName;
            item.Type = configurationSection.Type;
            item.ConfigurationSection = configurationSection;

            return item;
        }

        /// <summary>
        /// Creates an empty <see cref="SectionLineItem"/>.
        /// Used by both creation-path and source-aware overloads. Override to return a
        /// derived <see cref="SectionLineItem"/> type — <see cref="SectionLineItem"/> is
        /// nested-protected, so external <see cref="AbstractTypeFactory{T}"/> registration
        /// is not reachable; subclassing <see cref="ConfiguredLineItemContainer"/> is the
        /// supported extension point.
        /// </summary>
        protected virtual SectionLineItem CreateSectionLineItem()
        {
            return new SectionLineItem();
        }

        /// <summary>
        /// Builds the final <see cref="ConfigurationItem"/> for a section during
        /// <see cref="CreateConfiguredLineItem(int)"/> sub-item materialization.
        /// On the source-aware path (<see cref="SectionLineItem.Source"/> is set), updates
        /// the existing item in place and returns it — preserving the CLR subtype and all
        /// domain-specific fields the base type does not expose.
        /// On the creation path, routes instantiation through <see cref="CartItemBuilder"/>
        /// for subtype dispatch; falls back to
        /// <see cref="AbstractTypeFactory{T}.TryCreateInstance()"/> when the builder is null.
        /// </summary>
        protected virtual ConfigurationItem CreateConfigurationItem(SectionLineItem section)
        {
            ConfigurationItem configurationItem;

            if (section.Source is { } source)
            {
                configurationItem = source.CloneTyped();

                // The source-aware path may materialize an existing configuration item into a
                // different cart (currency change, saved-for-later). Reset keys so the clone
                // persists as a new row instead of reusing the source primary key — mirrors the
                // pre-merge reset in MergeWithCartAsync, extended to audit fields and the
                // LineItemId foreign key so the clone matches a freshly constructed instance.
                foreach (var entity in configurationItem.GetFlatObjectsListWithInterface<IEntity>())
                {
                    entity.Id = null;

                    if (entity is IAuditable auditable)
                    {
                        auditable.CreatedDate = default;
                        auditable.CreatedBy = null;
                        auditable.ModifiedDate = null;
                        auditable.ModifiedBy = null;
                    }
                }

                configurationItem.LineItemId = null;
            }
            else
            {
                configurationItem = CartItemBuilder?.Create(section.ConfigurationSection, section.CartProduct)
                    ?? AbstractTypeFactory<ConfigurationItem>.TryCreateInstance();

                configurationItem.SectionId = section.SectionId;
            }

            configurationItem.SectionName = section.SectionName;
            configurationItem.Type = section.Type;

            configurationItem.CatalogId = section.Item?.CatalogId;
            configurationItem.CategoryId = section.Item?.CategoryId;
            configurationItem.ProductId = section.Item?.ProductId;
            configurationItem.Name = section.Item?.Name;
            configurationItem.Sku = section.Item?.Sku;
            configurationItem.ImageUrl = section.Item?.ImageUrl;
            configurationItem.Quantity = section.Item?.Quantity ?? 1;
            if (section.Type is ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation)
            {
                configurationItem.ListPrice = section.Item?.ListPrice ?? 0m;
                configurationItem.SalePrice = section.Item?.SalePrice ?? 0m;
                configurationItem.SelectedForCheckout = section.Item?.SelectedForCheckout ?? true;
            }
            configurationItem.CustomText = section.CustomText;

            // An explicit file override (files transformed/dropped for a different cart on a
            // currency change or saved-for-later move) wins on both paths. Absent an override,
            // the creation path falls through to [] and the source path keeps the clone's own
            // (already key-cleared) files.
            if (section.Files is not null)
            {
                configurationItem.Files = section.Files;
            }
            configurationItem.Files ??= [];

            return configurationItem;
        }

        public virtual ExpConfigurationLineItem CreateConfiguredLineItem(int quantity)
        {
            var lineItem = CartItemBuilder?.Create(ConfigurableProduct)
                           ?? AbstractTypeFactory<LineItem>.TryCreateInstance();

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
            lineItem.ConfigurationItems = Items.Select(CreateConfigurationItem).ToList();

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

            foreach (var sectionLineItem in Items)
            {
                if (sectionLineItem.Item is not { } item)
                {
                    continue;
                }

                // Resolve the destination among the line item's own configuration items. Prefer the
                // staged Source by reference (the source-aware price-sync path carries the live item),
                // which also makes it explicit that we write back into lineItem.ConfigurationItems — not
                // into a detached clone. Fall back to matching by (SectionId, Type, ProductId) when the
                // staged item has no Source on the line item (e.g. a freshly materialized item).
                var configurationItem = sectionLineItem.Source is not null
                    ? lineItem.ConfigurationItems.FirstOrDefault(x => ReferenceEquals(x, sectionLineItem.Source))
                      : null;
                configurationItem ??= lineItem.ConfigurationItems.FirstOrDefault(x =>
                    x.SectionId == sectionLineItem.SectionId &&
                    x.Type == sectionLineItem.Type &&
                    (x.Type is not (ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation) || x.ProductId == item.ProductId));

                if (configurationItem is not null)
                {
                    configurationItem.ListPrice = item.ListPrice;
                    configurationItem.SalePrice = item.SalePrice;
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
            public string SectionName { get; set; }
            public string Type { get; set; }
            public LineItem Item { get; set; }
            public CartProduct CartProduct { get; set; }
            public string CustomText { get; set; }

            /// <summary>
            /// Explicit file override for the materialized configuration item. <c>null</c> means
            /// "no override": the creation path then yields an empty list, while the source-aware
            /// path keeps the clone's own (deep-copied) files. A non-null value — including an empty
            /// list — is honored as-is (e.g. files transformed/dropped for a different cart).
            /// </summary>
            public IList<ConfigurationItemFile> Files { get; set; }

            /// <summary>
            /// The creation-path <see cref="ProductConfigurationSection"/> this staging item was built
            /// from, and the section's chosen product. Both are <c>null</c> for items added via the
            /// source-aware overloads (which carry <see cref="Source"/> instead). Passed to
            /// <see cref="ICartItemBuilder.Create(ProductConfigurationSection, CartProduct)"/> at
            /// materialize time so an override can dispatch subtype + subtype-specific fields.
            /// </summary>
            public ProductConfigurationSection ConfigurationSection { get; set; }

            /// <summary>
            /// Reference to the source <see cref="ConfigurationItem"/> from which this section line
            /// item was built, when one exists. <c>null</c> for items added via creation-path
            /// overloads (e.g. <see cref="AddTextSectionLineItem(ProductConfigurationSection)"/>)
            /// where the configuration item does not yet exist; non-null for items added via the
            /// source-aware overloads (e.g. <see cref="AddTextSectionLineItem(ConfigurationItem)"/>).
            /// </summary>
            public ConfigurationItem Source { get; set; }
        }
    }
}
