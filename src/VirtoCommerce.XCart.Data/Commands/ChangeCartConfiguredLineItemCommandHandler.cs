using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class ChangeCartConfiguredLineItemCommandHandler : CartCommandHandler<ChangeCartConfiguredLineItemCommand>
{
    private readonly IMediator _mediator;

    public ChangeCartConfiguredLineItemCommandHandler(ICartAggregateRepository cartAggregateRepository, IMediator mediator)
        : base(cartAggregateRepository)
    {
        _mediator = mediator;
    }

    public override async Task<CartAggregate> Handle(ChangeCartConfiguredLineItemCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
        var lineItem = cartAggregate.GetConfiguredLineItem(request.LineItemId);

        if (lineItem != null)
        {
            var command = new CreateConfiguredLineItemCommand
            {
                StoreId = request.StoreId,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                CultureName = request.CultureName,
                CurrencyCode = request.CurrencyCode,
                ConfigurableProductId = lineItem.ProductId,
                ConfigurationSections = request.ConfigurationSections,
                Quantity = request.Quantity ?? lineItem.Quantity,
                CartId = cartAggregate.Cart.Id,
            };

            var mediatorResult = await _mediator.Send(command, cancellationToken);
            await cartAggregate.UpdateConfiguredLineItemAsync(lineItem.Id, mediatorResult.Item);

            return await SaveCartAsync(cartAggregate);
        }

        return cartAggregate;
    }
}
