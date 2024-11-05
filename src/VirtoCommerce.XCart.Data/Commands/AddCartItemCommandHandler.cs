using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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

        public AddCartItemCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartProductService cartProductService,
            IMediator mediator)
            : base(cartAggregateRepository)
        {
            _cartProductService = cartProductService;
            _mediator = mediator;
        }

        public override async Task<CartAggregate> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            var product = (await _cartProductService.GetCartProductsByIdsAsync(cartAggregate, new[] { request.ProductId })).FirstOrDefault();

            var newItem = new NewCartItem(request.ProductId, request.Quantity)
            {
                Comment = request.Comment,
                DynamicProperties = request.DynamicProperties,
                Price = request.Price,
                CartProduct = product,
                ConfigurationSections = request.ConfigurationSections,
            };

            if (product.Product.IsConfigurable)
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

                var mediatorResult = await _mediator.Send(createConfigurableProductCommand);
                var lineItem = mediatorResult.GetConfiguredLineItem();
                await cartAggregate.AddConfiguredItemAsync(newItem, lineItem);
            }
            else
            {
                await cartAggregate.AddItemAsync(newItem);
            }

            return await SaveCartAsync(cartAggregate);
        }
    }
}
