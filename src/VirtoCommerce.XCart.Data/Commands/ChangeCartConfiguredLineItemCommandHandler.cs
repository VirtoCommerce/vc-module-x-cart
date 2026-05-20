using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

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
            var oldConfigurationItems = lineItem.ConfigurationItems?.ToList() ?? [];

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
            PreserveSelectedForCheckoutFromOldConfiguration(mediatorResult.Item.ConfigurationItems, oldConfigurationItems);

            await cartAggregate.UpdateConfiguredLineItemAsync(lineItem.Id, mediatorResult.Item);
            await cartAggregate.UpdateConfiguredLineItemPrice([lineItem]);

            return await SaveCartAsync(cartAggregate);
        }

        return cartAggregate;
    }

    private static void PreserveSelectedForCheckoutFromOldConfiguration(
        ICollection<ConfigurationItem> newConfigurationItems,
        ICollection<ConfigurationItem> oldConfigurationItems)
    {
        if (newConfigurationItems.IsNullOrEmpty() || oldConfigurationItems.IsNullOrEmpty())
        {
            return;
        }

        foreach (var newConfigurationItem in newConfigurationItems)
        {
            var oldConfigurationItem = oldConfigurationItems.FirstOrDefault(x => MatchesSection(x, newConfigurationItem));
            if (oldConfigurationItem != null)
            {
                newConfigurationItem.SelectedForCheckout = oldConfigurationItem.SelectedForCheckout;
            }
        }

        static bool MatchesSection(ConfigurationItem a, ConfigurationItem b)
        {
            if (a.Type != b.Type || a.SectionId != b.SectionId)
            {
                return false;
            }

            if (a.Type == ConfigurationSectionTypeVariation)
            {
                return a.ProductId == b.ProductId;
            }

            return true;
        }
    }
}
