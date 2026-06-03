using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
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
    public class AddWishlistItemCommandHandler : CartCommandHandler<AddWishlistItemCommand>
    {
        private readonly IProductConfigurationSearchService _productConfigurationSearchService;
        private readonly ICartProductService _cartProductService;
        private readonly IMediator _mediator;

        public AddWishlistItemCommandHandler(
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

        public override async Task<CartAggregate> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await CartRepository.GetCartByIdAsync(request.ListId);
            cartAggregate.ValidationRuleSet = ["default"];

            var newItem = AbstractTypeFactory<NewCartItem>.TryCreateInstance();
            newItem.ProductId = request.ProductId;
            newItem.Quantity = request.Quantity ?? 1;
            newItem.IsWishlist = true;
            newItem.IgnoreValidationErrors = true;

            var productConfiguration = await GetProductConfiguration(request);
            if (productConfiguration?.IsActive == true)
            {
                var cartProduct = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, [request.ProductId])).FirstOrDefault();
                newItem.CartProduct = cartProduct;

                var createConfigurableProductCommand = AbstractTypeFactory<CreateConfiguredLineItemCommand>.TryCreateInstance();
                createConfigurableProductCommand.StoreId = cartAggregate.Store.Id;
                createConfigurableProductCommand.UserId = cartAggregate.Cart.CustomerId;
                createConfigurableProductCommand.OrganizationId = cartAggregate.Cart.OrganizationId;
                createConfigurableProductCommand.CultureName = cartAggregate.Cart.LanguageCode;
                createConfigurableProductCommand.CurrencyCode = cartAggregate.Currency.Code;
                createConfigurableProductCommand.ConfigurableProductId = request.ProductId;
                createConfigurableProductCommand.ConfigurationSections = request.ConfigurationSections;
                createConfigurableProductCommand.CartId = cartAggregate.Cart.Id;

                var mediatorResult = await _mediator.Send(createConfigurableProductCommand, cancellationToken);
                await cartAggregate.AddConfiguredItemAsync(newItem, mediatorResult.Item);
            }
            else
            {
                await cartAggregate.AddItemsAsync([newItem]);
            }

            return await SaveCartAsync(cartAggregate);
        }

        protected virtual async Task<ProductConfiguration> GetProductConfiguration(AddWishlistItemCommand request)
        {
            var criteria = AbstractTypeFactory<ProductConfigurationSearchCriteria>.TryCreateInstance();
            criteria.ProductId = request.ProductId;

            var configurations = await _productConfigurationSearchService.SearchNoCloneAsync(criteria);

            return configurations.Results.FirstOrDefault();
        }
    }
}
