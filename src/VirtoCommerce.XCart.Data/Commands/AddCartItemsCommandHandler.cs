using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Core.Validators;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddCartItemsCommandHandler : CartCommandHandler<AddCartItemsCommand>
    {
        private readonly ICartProductService _cartProductService;
        private readonly IMediator _mediator;
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;

        public AddCartItemsCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartProductService cartProductService,
            IMediator mediator,
            IProductConfigurationSearchService productConfigurationSearchService)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
            _mediator = mediator;
            _productConfigurationSearchService = productConfigurationSearchService;
        }

        public override async Task<CartAggregate> Handle(AddCartItemsCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await AddItemsToCartAsync(request, cartAggregate, cancellationToken);

            return await SaveCartAsync(cartAggregate);
        }

        protected virtual async Task AddItemsToCartAsync(AddCartItemsCommand request, CartAggregate cartAggregate, CancellationToken cancellationToken)
        {
            if (request.CartItems.IsNullOrEmpty())
            {
                return;
            }

            var productIds = request.CartItems.Select(x => x.ProductId).Distinct().ToArray();

            var productsTask = _cartProductService.GetCartProductsByIdsAsync(cartAggregate, productIds);
            var configurationsTask = _productConfigurationSearchService.SearchAllNoCloneAsync(
                new ProductConfigurationSearchCriteria
                {
                    ProductIds = productIds,
                    IsActive = true,
                });

            await Task.WhenAll(productsTask, configurationsTask);

            var productsByIds = productsTask.Result.ToDictionary(x => x.Id);
            var activeConfigByProductId = configurationsTask.Result.DistinctBy(x => x.ProductId).ToDictionary(x => x.ProductId);

            foreach (var item in request.CartItems)
            {
                if (!productsByIds.TryGetValue(item.ProductId, out var product))
                {
                    var error = CartErrorDescriber.ProductUnavailableError(nameof(CatalogProduct), item.ProductId);
                    cartAggregate.OperationValidationErrors.Add(error);
                    continue;
                }

                item.CartProduct = product;

                if (activeConfigByProductId.ContainsKey(item.ProductId))
                {
                    await AddConfiguredItemAsync(request, item, cartAggregate, cancellationToken);
                }
                else
                {
                    await cartAggregate.AddItemAsync(item);
                }
            }
        }

        protected virtual async Task AddConfiguredItemAsync(AddCartItemsCommand request, NewCartItem item, CartAggregate cartAggregate, CancellationToken cancellationToken)
        {
            var command = new CreateConfiguredLineItemCommand
            {
                StoreId = request.StoreId,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                CultureName = request.CultureName,
                CurrencyCode = request.CurrencyCode,
                ConfigurableProductId = item.ProductId,
                ConfigurationSections = item.ConfigurationSections,
                CartId = cartAggregate.Cart.Id,
            };

            var result = await _mediator.Send(command, cancellationToken);

            await cartAggregate.AddConfiguredItemAsync(item, result.Item);
        }
    }
}
