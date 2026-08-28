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

        if (lineItem == null)
        {
            return cartAggregate;
        }

        var oldConfigurationItems = lineItem.ConfigurationItems?.ToList() ?? [];

        var newConfiguredItem = await CreateConfiguredLineItemAsync(cartAggregate, lineItem, request, cancellationToken);
        PreserveSelectedForCheckoutFromOldConfiguration(newConfiguredItem.ConfigurationItems, oldConfigurationItems);

        await ApplyConfiguredLineItemAsync(cartAggregate, lineItem, newConfiguredItem, request, cancellationToken);

        return await SaveCartAsync(cartAggregate);
    }

    /// <summary>
    /// Builds and dispatches the nested <see cref="CreateConfiguredLineItemCommand"/> for the configured
    /// product and returns the freshly created line item. Override this seam to inspect or post-process
    /// the created <see cref="LineItem.ConfigurationItems"/> before they are applied to the cart.
    /// </summary>
    protected virtual async Task<LineItem> CreateConfiguredLineItemAsync(
        CartAggregate cartAggregate,
        LineItem lineItem,
        ChangeCartConfiguredLineItemCommand request,
        CancellationToken cancellationToken)
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

        return mediatorResult.Item;
    }

    /// <summary>
    /// Applies the freshly created <paramref name="newConfiguredItem"/> onto the existing cart line item
    /// and recalculates the configured-line-item price. Override this seam to run additional work after
    /// the cart has been updated but before it is saved.
    /// </summary>
    protected virtual async Task ApplyConfiguredLineItemAsync(
        CartAggregate cartAggregate,
        LineItem lineItem,
        LineItem newConfiguredItem,
        ChangeCartConfiguredLineItemCommand request,
        CancellationToken cancellationToken)
    {
        await cartAggregate.UpdateConfiguredLineItemAsync(lineItem.Id, newConfiguredItem);
        await cartAggregate.UpdateConfiguredLineItemPrice([lineItem]);
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
