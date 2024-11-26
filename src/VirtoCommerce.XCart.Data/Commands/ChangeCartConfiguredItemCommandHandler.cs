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

public class ChangeCartConfiguredItemCommandHandler : CartCommandHandler<ChangeCartConfiguredItemCommand>
{
    private readonly IMediator _mediator;

    public ChangeCartConfiguredItemCommandHandler(ICartAggregateRepository cartAggregateRepository, IMediator mediator)
        : base(cartAggregateRepository)
    {
        _mediator = mediator;
    }

    public override async Task<CartAggregate> Handle(ChangeCartConfiguredItemCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        var lineItem = GetConfiguredLineItem(request, cartAggregate);
        if (lineItem != null)
        {
            var createConfigurableProductCommand = new CreateConfiguredLineItemCommand
            {
                StoreId = request.StoreId,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                CultureName = request.CultureName,
                CurrencyCode = request.CurrencyCode,
                ConfigurableProductId = lineItem.ProductId,
                ConfigurationSections = request.ConfigurationSections,
                Quantity = request.Quantity ?? lineItem.Quantity,
            };

            var mediatorResult = await _mediator.Send(createConfigurableProductCommand, cancellationToken);
            await cartAggregate.UpdateConfiguredItemAsync(lineItem.Id, mediatorResult.Item);

            return await SaveCartAsync(cartAggregate);
        }

        return cartAggregate;
    }

    private static LineItem GetConfiguredLineItem(ChangeCartConfiguredItemCommand request, CartAggregate cartAggregate)
    {
        return cartAggregate.Cart.Items.FirstOrDefault(x => x.Id == request.LineItemId && x.IsConfigured);
    }
}
