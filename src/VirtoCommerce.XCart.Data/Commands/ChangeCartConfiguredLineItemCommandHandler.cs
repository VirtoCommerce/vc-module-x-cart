using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
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
            var command = AbstractTypeFactory<CreateConfiguredLineItemCommand>.TryCreateInstance();
            command.StoreId = request.StoreId;
            command.UserId = request.UserId;
            command.OrganizationId = request.OrganizationId;
            command.CultureName = request.CultureName;
            command.CurrencyCode = request.CurrencyCode;
            command.ConfigurableProductId = lineItem.ProductId;
            command.ConfigurationSections = request.ConfigurationSections;
            command.Quantity = request.Quantity ?? lineItem.Quantity;
            command.CartId = cartAggregate.Cart.Id;

            var mediatorResult = await _mediator.Send(command, cancellationToken);
            await cartAggregate.UpdateConfiguredLineItemAsync(lineItem.Id, mediatorResult.Item);

            return await SaveCartAsync(cartAggregate);
        }

        return cartAggregate;
    }
}
