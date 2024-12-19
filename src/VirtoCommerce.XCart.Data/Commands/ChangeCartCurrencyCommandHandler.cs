using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeCartCurrencyCommandHandler : CartCommandHandler<ChangeCartCurrencyCommand>
    {
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;
        private readonly ICartProductService _cartProductService;
        private readonly IMediator _mediator;

        public ChangeCartCurrencyCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            IProductConfigurationSearchService productConfigurationSearchService,
            ICartProductService cartProductService,
            IMediator mediator)
            : base(cartAggregateRepository)
        {
            _productConfigurationSearchService = productConfigurationSearchService;
            _cartProductService = cartProductService;
            _mediator = mediator;
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

            // copy configured items
            var configuredItems = currentCurrencyCartAggregate.LineItems
                .Where(x => x.IsConfigured)
                .ToArray();

            await CopyConfiguredItems(newCurrencyCartAggregate, configuredItems);
        }

        protected virtual async Task CopyConfiguredItems(CartAggregate newCurrencyCartAggregate, IList<LineItem> configuredItems)
        {
            if (configuredItems.Count == 0)
            {
                return;
            }

            var configProductsIds = configuredItems
                            .Where(x => !x.ConfigurationItems.IsNullOrEmpty())
                            .SelectMany(x => x.ConfigurationItems.Select(x => x.ProductId))
                            .Distinct()
                            .ToList();

            configProductsIds.AddRange(configuredItems.Select(x => x.ProductId));

            var configProducts = await _cartProductService.GetCartProductsByIdsAsync(newCurrencyCartAggregate, configProductsIds);

            foreach (var configurationLineItem in configuredItems)
            {
                var contaner = AbstractTypeFactory<ConfiguredLineItemContainer>.TryCreateInstance();
                contaner.Currency = newCurrencyCartAggregate.Currency;
                contaner.Store = newCurrencyCartAggregate.Store;

                var configurableProduct = configProducts.FirstOrDefault(x => x.Product.Id == configurationLineItem.ProductId);
                if (configurableProduct != null)
                {
                    contaner.ConfigurableProduct = configurableProduct;
                }

                foreach (var configurationItem in configurationLineItem.ConfigurationItems ?? [])
                {
                    var product = configProducts.FirstOrDefault(x => x.Product.Id == configurationItem.ProductId);
                    if (product != null)
                    {
                        contaner.AddItem(product, configurationItem.Quantity, configurationItem.SectionId);
                    }
                }

                var expItem = contaner.CreateConfiguredLineItem(configurationLineItem.Quantity);

                await newCurrencyCartAggregate.AddConfiguredItemAsync(new NewCartItem(configurableProduct.Id, configurationLineItem.Quantity)
                {
                    CartProduct = configurableProduct,
                    IgnoreValidationErrors = true,
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
    }
}
