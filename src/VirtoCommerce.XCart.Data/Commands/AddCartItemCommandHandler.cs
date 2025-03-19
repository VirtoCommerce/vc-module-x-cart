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
            var product = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, [request.ProductId])).FirstOrDefault();

            var newItem = new NewCartItem(request.ProductId, request.Quantity)
            {
                Comment = request.Comment,
                DynamicProperties = request.DynamicProperties,
                Price = request.Price,
                CartProduct = product,
                CreatedDate = request.CreatedDate,
            };

            var configurations = await _productConfigurationSearchService.SearchNoCloneAsync(new ProductConfigurationSearchCriteria
            {
                ProductId = request.ProductId
            });
            var configuration = configurations.Results.FirstOrDefault();

            if (configuration?.IsActive == true)
            {
                var createConfigurableProductCommand = new CreateConfiguredLineItemCommand
                {
                    StoreId = request.StoreId,
                    UserId = request.UserId,
                    OrganizationId = request.OrganizationId,
                    CultureName = request.CultureName,
                    CurrencyCode = request.CurrencyCode,
                    ConfigurableProductId = request.ProductId,
                    ConfigurationSections = request.ConfigurationSections,
                };

                var mediatorResult = await _mediator.Send(createConfigurableProductCommand, cancellationToken);
                await cartAggregate.AddConfiguredItemAsync(newItem, mediatorResult.Item);
            }
            else
            {
                await cartAggregate.AddItemAsync(newItem);
            }

            return await SaveCartAsync(cartAggregate);
        }
    }
}
