using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddCartItemCommandHandler : CartCommandHandler<AddCartItemCommand>
    {
        private readonly ICartProductService _cartProductService;
        private readonly IMediator _mediator;
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;

        public AddCartItemCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartProductService cartProductService,
            IMediator mediator,
            IProductConfigurationSearchService productConfigurationSearchService
            )
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
            _mediator = mediator;
            _productConfigurationSearchService = productConfigurationSearchService;
        }

        public override async Task<CartAggregate> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            await AddItemToCartAsync(request, cartAggregate, cancellationToken);

            return await SaveCartAsync(cartAggregate);
        }

        protected virtual async Task AddItemToCartAsync(AddCartItemCommand request, CartAggregate cartAggregate, CancellationToken cancellationToken)
        {
            var itemCurrencyCode = !request.ItemCurrencyCode.IsNullOrEmpty() ? request.ItemCurrencyCode : cartAggregate.Currency.Code;
            var product = (await _cartProductService.GetCartProductsAsync(cartAggregate, [(itemCurrencyCode, request.ProductId)])).Values.FirstOrDefault();

            var newItem = CreateNewCartItem(request, product, itemCurrencyCode);

            var configurations = await _productConfigurationSearchService.SearchNoCloneAsync(new ProductConfigurationSearchCriteria
            {
                ProductId = request.ProductId
            });
            var configuration = configurations.Results.FirstOrDefault();

            if (configuration?.IsActive == true)
            {
                var createConfigurableProductCommand = AbstractTypeFactory<CreateConfiguredLineItemCommand>.TryCreateInstance();
                createConfigurableProductCommand.StoreId = request.StoreId;
                createConfigurableProductCommand.UserId = request.UserId;
                createConfigurableProductCommand.OrganizationId = request.OrganizationId;
                createConfigurableProductCommand.CultureName = request.CultureName;
                createConfigurableProductCommand.CurrencyCode = itemCurrencyCode;
                createConfigurableProductCommand.ConfigurableProductId = request.ProductId;
                createConfigurableProductCommand.ConfigurationSections = request.ConfigurationSections;
                createConfigurableProductCommand.CartId = cartAggregate.Cart.Id;

                var mediatorResult = await _mediator.Send(createConfigurableProductCommand, cancellationToken);
                await cartAggregate.AddConfiguredItemAsync(newItem, mediatorResult.Item);
            }
            else
            {
                await cartAggregate.AddItemAsync(newItem);
            }
        }

        protected virtual NewCartItem CreateNewCartItem(AddCartItemCommand request, CartProduct product, string itemCurrencyCode)
        {
            var newItem = AbstractTypeFactory<NewCartItem>.TryCreateInstance();
            newItem.ProductId = request.ProductId;
            newItem.Quantity = request.Quantity;
            newItem.Comment = request.Comment;
            newItem.DynamicProperties = request.DynamicProperties;
            newItem.Price = request.Price;
            newItem.CartProduct = product;
            newItem.CreatedDate = request.CreatedDate;
            newItem.ItemCurrencyCode = itemCurrencyCode;

            return newItem;
        }
    }
}
