using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Commands.Configuration;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands.Configuration;

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
        var lineItem = GetConfiguredLineItem(request, cartAggregate);

        if (lineItem != null)
        {
            var command = new ChangeConfiguredLineItemCommand
            {
                StoreId = request.StoreId,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                CultureName = request.CultureName,
                CurrencyCode = request.CurrencyCode,
                ConfigurableProductId = lineItem.ProductId,
                ConfigurationSections = request.ConfigurationSections,
                Quantity = request.Quantity ?? lineItem.Quantity,
                ConfigurationItems = [.. lineItem.ConfigurationItems],
            };

            var mediatorResult = await _mediator.Send(command, cancellationToken);
            await cartAggregate.UpdateConfiguredLineItemAsync(lineItem.Id, mediatorResult.Item);

            return await SaveCartAsync(cartAggregate);
        }

        return cartAggregate;
    }

    private static LineItem GetConfiguredLineItem(ChangeCartConfiguredLineItemCommand request, CartAggregate cartAggregate)
    {
        return cartAggregate.Cart.Items.FirstOrDefault(x => x.Id == request.LineItemId && x.IsConfigured);
    }
}
