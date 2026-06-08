using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeCartCurrencyCommandHandler : CartCommandHandler<ChangeCartCurrencyCommand>
    {
        private readonly ICartProductService _cartProductService;
        private readonly IFileUploadService _fileUploadService;

        public ChangeCartCurrencyCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartProductService cartProductService,
            IFileUploadService fileUploadService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
            _fileUploadService = fileUploadService;
        }

        public override async Task<CartAggregate> Handle(ChangeCartCurrencyCommand request, CancellationToken cancellationToken)
        {
            // get (or create) both carts
            var currentCurrencyCartAggregate = await GetOrCreateCartFromCommandAsync(request)
                ?? throw new OperationCanceledException("Cart not found");

            var newCurrencyCartRequest = new ChangeCartCurrencyCommand
            {
                StoreId = request.StoreId ?? currentCurrencyCartAggregate.Cart.StoreId,
                CartName = request.CartName ?? currentCurrencyCartAggregate.Cart.Name,
                CartType = request.CartType ?? currentCurrencyCartAggregate.Cart.Type,
                UserId = request.UserId ?? currentCurrencyCartAggregate.Cart.CustomerId,
                OrganizationId = request.OrganizationId ?? currentCurrencyCartAggregate.Cart.OrganizationId,
                CultureName = request.CultureName ?? currentCurrencyCartAggregate.Cart.LanguageCode,
                CurrencyCode = request.NewCurrencyCode,
            };

            var newCurrencyCartAggregate = await GetOrCreateCartFromCommandAsync(newCurrencyCartRequest);

            // clear (old) cart items and add items from the currency cart
            newCurrencyCartAggregate.Cart.Items.Clear();

            await CopyItems(currentCurrencyCartAggregate, newCurrencyCartAggregate);

            await CartRepository.SaveAsync(newCurrencyCartAggregate);
            return newCurrencyCartAggregate;
        }

        protected virtual async Task CopyItems(CartAggregate currentCurrencyCartAggregate, CartAggregate newCurrencyCartAggregate)
        {
            var ordinaryItems = currentCurrencyCartAggregate.LineItems
                .Where(x => !x.IsConfigured)
                .ToArray();

            if (ordinaryItems.Length > 0)
            {
                var newCartItems = ordinaryItems
                    .Select(x =>
                    {
                        var newCartItem = AbstractTypeFactory<NewCartItem>.TryCreateInstance();
                        newCartItem.ItemCurrencyCode = ResolveTargetCurrency(x.Currency, currentCurrencyCartAggregate, newCurrencyCartAggregate).Code;
                        newCartItem.ProductId = x.ProductId;
                        newCartItem.Quantity = x.Quantity;
                        newCartItem.IgnoreValidationErrors = true;
                        newCartItem.CreatedDate = x.CreatedDate;
                        newCartItem.Comment = x.Note;
                        newCartItem.IsSelectedForCheckout = x.SelectedForCheckout;
                        newCartItem.DynamicProperties = x.DynamicProperties.SelectMany(p => p.Values.Select(v =>
                        {
                            var value = AbstractTypeFactory<DynamicPropertyValue>.TryCreateInstance();
                            value.Name = p.Name;
                            value.Value = v.Value;
                            value.Locale = v.Locale;

                            return value;
                        })).ToArray();

                        return newCartItem;
                    })
                    .ToArray();

                await newCurrencyCartAggregate.AddItemsAsync(newCartItems);
            }

            await CopyConfiguredItems(currentCurrencyCartAggregate, newCurrencyCartAggregate);
        }

        /// <summary>
        /// Resolves the target currency for a copied line item.
        /// Items in the source cart's base currency are converted to the new cart's currency;
        /// items in their own (non-base) currency keep it.
        /// </summary>
        protected virtual Currency ResolveTargetCurrency(string itemCurrencyCode, CartAggregate currentCurrencyCartAggregate, CartAggregate newCurrencyCartAggregate)
        {
            return itemCurrencyCode.EqualsIgnoreCase(currentCurrencyCartAggregate.Cart.Currency)
                ? newCurrencyCartAggregate.Currency
                : currentCurrencyCartAggregate.GetCurrencyByCode(itemCurrencyCode);
        }

        protected virtual async Task CopyConfiguredItems(CartAggregate currentCurrencyCartAggregate, CartAggregate newCurrencyCartAggregate)
        {
            var configuredItems = currentCurrencyCartAggregate.LineItems
                .Where(x => x.IsConfigured)
                .ToList();

            if (configuredItems.Count == 0)
            {
                return;
            }

            var configProductPairs = configuredItems
                .SelectMany(item =>
                {
                    var targetCurrencyCode = ResolveTargetCurrency(item.Currency, currentCurrencyCartAggregate, newCurrencyCartAggregate).Code;
                    var productIds = (item.ConfigurationItems ?? [])
                        .Select(c => c.ProductId)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Append(item.ProductId);

                    return productIds.Select(id => (targetCurrencyCode, id));
                })
                .Distinct()
                .ToList();

            var configProducts = await _cartProductService.GetCartProductsAsync(newCurrencyCartAggregate, configProductPairs);

            foreach (var configurationLineItem in configuredItems)
            {
                var container = await CreateConfiguredLineItemContainerAsync(configurationLineItem, configProducts, currentCurrencyCartAggregate, newCurrencyCartAggregate);
                if (container is null)
                {
                    continue;
                }

                var expItem = container.CreateConfiguredLineItem(configurationLineItem.Quantity);

                var newCartItem = AbstractTypeFactory<NewCartItem>.TryCreateInstance();
                newCartItem.ProductId = configurationLineItem.ProductId;
                newCartItem.Quantity = configurationLineItem.Quantity;
                newCartItem.CartProduct = container.ConfigurableProduct;
                newCartItem.IgnoreValidationErrors = true;
                newCartItem.CreatedDate = configurationLineItem.CreatedDate;
                newCartItem.Comment = configurationLineItem.Note;
                newCartItem.IsSelectedForCheckout = configurationLineItem.SelectedForCheckout;
                newCartItem.DynamicProperties = configurationLineItem.DynamicProperties.SelectMany(x => x.Values.Select(y =>
                {
                    var value = AbstractTypeFactory<DynamicPropertyValue>.TryCreateInstance();
                    value.Name = x.Name;
                    value.Value = y.Value;
                    value.Locale = y.Locale;

                    return value;
                })).ToArray();

                await newCurrencyCartAggregate.AddConfiguredItemAsync(newCartItem, expItem.Item);
            }
        }

        protected virtual async Task<ConfiguredLineItemContainer> CreateConfiguredLineItemContainerAsync(
            LineItem configurationLineItem,
            IDictionary<string, CartProduct> configProducts,
            CartAggregate currentCurrencyCartAggregate,
            CartAggregate newCurrencyCartAggregate)
        {
            var targetCurrency = ResolveTargetCurrency(configurationLineItem.Currency, currentCurrencyCartAggregate, newCurrencyCartAggregate);

            if (!configProducts.TryGetValue(CartAggregate.GetCartProductKey(configurationLineItem.ProductId, targetCurrency.Code), out var configurableProduct))
            {
                return null;
            }

            var container = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();
            container.Currency = targetCurrency;
            container.Store = newCurrencyCartAggregate.Store;
            container.ConfigurableProduct = configurableProduct;

            foreach (var configurationItem in configurationLineItem.ConfigurationItems ?? [])
            {
                switch (configurationItem.Type)
                {
                    case ConfigurationSectionTypeProduct or ConfigurationSectionTypeVariation:
                    {
                        if (configProducts.TryGetValue(CartAggregate.GetCartProductKey(configurationItem.ProductId, targetCurrency.Code), out var product))
                        {
                            container.AddProductSectionLineItem(product, configurationItem);
                        }

                        break;
                    }
                    case ConfigurationSectionTypeText:
                        container.AddTextSectionLineItem(configurationItem);
                        break;
                    case ConfigurationSectionTypeFile:
                    {
                        var files = await CopyConfigurationFiles(configurationItem, currentCurrencyCartAggregate.Cart);
                        container.AddFileSectionLineItem(configurationItem, files);
                        break;
                    }
                }
            }

            return container;
        }

        private async Task<IList<ConfigurationItemFile>> CopyConfigurationFiles(ConfigurationItem configurationItem, ShoppingCart cart)
        {
            var fileUrls = configurationItem.Files
                ?.Select(x => x.Url)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToArray();

            if (fileUrls.IsNullOrEmpty())
            {
                return [];
            }

            return (await _fileUploadService.GetByPublicUrlAsync(fileUrls))
                .Where(x => x.Scope == ConfigurationSectionFilesScope && x.OwnerIs(cart))
                .Select(x => x.ConvertToConfigurationItemFile())
                .ToList();
        }
    }
}
