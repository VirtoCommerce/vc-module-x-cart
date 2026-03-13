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

            var newItem = new NewCartItem(request.ProductId, request.Quantity ?? 1)
            {
                IsWishlist = true,
                IgnoreValidationErrors = true,
            };

            var productConfiguration = await GetProductConfiguration(request);
            if (productConfiguration?.IsActive == true)
            {
                var cartProduct = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, [request.ProductId])).FirstOrDefault();
                newItem.CartProduct = cartProduct;

                var createConfigurableProductCommand = new CreateConfiguredLineItemCommand
                {
                    StoreId = cartAggregate.Store.Id,
                    UserId = cartAggregate.Cart.CustomerId,
                    OrganizationId = cartAggregate.Cart.OrganizationId,
                    CultureName = cartAggregate.Cart.LanguageCode,
                    CurrencyCode = cartAggregate.Currency.Code,
                    ConfigurableProductId = request.ProductId,
                    ConfigurationSections = request.ConfigurationSections,
                    CartId = cartAggregate.Cart.Id,
                };

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
            criteria.IsActive = true;

            var configurations = await _productConfigurationSearchService.SearchNoCloneAsync(criteria);

            return configurations.Results.FirstOrDefault();
        }
    }
}
