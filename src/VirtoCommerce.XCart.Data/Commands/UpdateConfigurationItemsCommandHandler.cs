using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemsCommandHandler : CartConfigurationCommandHandler<UpdateConfigurationItemsCommand>
{
    public UpdateConfigurationItemsCommandHandler(
        ICartAggregateRepository cartAggregateRepository,
        ICartConfigurationService cartConfigurationService)
        : base(cartAggregateRepository, cartConfigurationService)
    {
    }

    protected override Task ApplyConfigurationAsync(CartAggregate cartAggregate, UpdateConfigurationItemsCommand request)
        => cartAggregate.UpdateConfigurationItemsAsync(request.LineItemId, request.ConfigurationSections);
}
