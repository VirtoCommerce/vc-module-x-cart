using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
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
                    .Select(x => new NewCartItem(x.ProductId, x.Quantity)
                    {
                        IgnoreValidationErrors = true,
                        CreatedDate = x.CreatedDate,
                        Comment = x.Note,
                        IsSelectedForCheckout = x.SelectedForCheckout,
                        DynamicProperties = x.DynamicProperties.SelectMany(x => x.Values.Select(y => new DynamicPropertyValue()
                        {
                            Name = x.Name,
                            Value = y.Value,
                            Locale = y.Locale,
                        })).ToArray(),
                    })
                    .ToArray();

                await newCurrencyCartAggregate.AddItemsAsync(newCartItems);
            }

            await CopyConfiguredItems(currentCurrencyCartAggregate, newCurrencyCartAggregate);
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

            var configProductsIds = configuredItems
                            .Where(x => !x.ConfigurationItems.IsNullOrEmpty())
                            .SelectMany(x => x.ConfigurationItems.Select(y => y.ProductId))
                            .Where(x => !string.IsNullOrEmpty(x))
                            .Distinct()
                            .ToList();

            configProductsIds.AddRange(configuredItems.Select(x => x.ProductId));

            var configProducts = await _cartProductService.GetCartProductsByIdsAsync(newCurrencyCartAggregate, configProductsIds);

            foreach (var configurationLineItem in configuredItems)
            {
                var container = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();
                container.Currency = newCurrencyCartAggregate.Currency;
                container.Store = newCurrencyCartAggregate.Store;

                container.ConfigurableProduct = configProducts.FirstOrDefault(x => x.Product.Id == configurationLineItem.ProductId);

                foreach (var configurationItem in configurationLineItem.ConfigurationItems ?? [])
                {
                    if (configurationItem.Type == ConfigurationSectionTypeProduct)
                    {
                        var product = configProducts.FirstOrDefault(x => x.Product.Id == configurationItem.ProductId);
                        if (product != null)
                        {
                            container.AddProductSectionLineItem(product, configurationItem.Quantity, configurationItem.SectionId);
                        }
                    }
                    else if (configurationItem.Type == ConfigurationSectionTypeText)
                    {
                        container.AddTextSectionLineItem(configurationItem.CustomText, configurationItem.SectionId);
                    }
                    else if (configurationItem.Type == ConfigurationSectionTypeFile)
                    {
                        var files = await CopyConfigurationFiles(configurationItem, currentCurrencyCartAggregate.Cart);
                        container.AddFileSectionLineItem(files, configurationItem.SectionId);
                    }
                }

                var expItem = container.CreateConfiguredLineItem(configurationLineItem.Quantity);

                await newCurrencyCartAggregate.AddConfiguredItemAsync(new NewCartItem(configurationLineItem.ProductId, configurationLineItem.Quantity)
                {
                    CartProduct = container.ConfigurableProduct,
                    IgnoreValidationErrors = true,
                    CreatedDate = configurationLineItem.CreatedDate,
                    Comment = configurationLineItem.Note,
                    IsSelectedForCheckout = configurationLineItem.SelectedForCheckout,
                    DynamicProperties = configurationLineItem.DynamicProperties.SelectMany(x => x.Values.Select(y => new DynamicPropertyValue()
                    {
                        Name = x.Name,
                        Value = y.Value,
                        Locale = y.Locale,
                    })).ToArray(),
                }, expItem.Item);
            }
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
                return null;
            }

            return (await _fileUploadService.GetByPublicUrlAsync(fileUrls))
                .Where(x => x.Scope == ConfigurationSectionFilesScope && x.OwnerIs(cart))
                .Select(x => x.ConvertToConfigurationItemFile())
                .ToList();
        }
    }
}
