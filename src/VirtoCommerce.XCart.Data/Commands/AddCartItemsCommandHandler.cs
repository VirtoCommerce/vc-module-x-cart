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

            var productPairs = request.CartItems.Select(x =>
            {
                var itemCurrencyCode = GetNewItemCurrencyCode(x, cartAggregate);
                return (itemCurrencyCode, x.ProductId);
            }).Distinct().ToList();
            var productsTask = _cartProductService.GetCartProductsAsync(cartAggregate, productPairs);

            var criteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
            criteria.ProductIds = productIds;
            criteria.IsActive = true;

            var configurationsTask = _productConfigurationSearchService.SearchAllNoCloneAsync(criteria);

            await Task.WhenAll(productsTask, configurationsTask);

            var products = productsTask.Result;
            var activeConfigByProductId = configurationsTask.Result.DistinctBy(x => x.ProductId).ToDictionary(x => x.ProductId);

            foreach (var item in request.CartItems)
            {
                var itemCurrencyCode = GetNewItemCurrencyCode(item, cartAggregate);
                if (!products.TryGetValue(cartAggregate.GetCartProductKey(item.ProductId, itemCurrencyCode), out var product))
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
            var command = AbstractTypeFactory<CreateConfiguredLineItemCommand>.TryCreateInstance();
            command.StoreId = request.StoreId;
            command.UserId = request.UserId;
            command.OrganizationId = request.OrganizationId;
            command.CultureName = request.CultureName;
            command.CurrencyCode = GetNewItemCurrencyCode(item, cartAggregate);
            command.ConfigurableProductId = item.ProductId;
            command.CartId = cartAggregate.Cart.Id;

            var result = await _mediator.Send(command, cancellationToken);

            await cartAggregate.AddConfiguredItemAsync(item, result.Item);
        }

        private static string GetNewItemCurrencyCode(NewCartItem item, CartAggregate cartAggregate)
        {
            return !item.ItemCurrencyCode.IsNullOrEmpty() ? item.ItemCurrencyCode : cartAggregate.Currency.Code;
        }
    }
}
