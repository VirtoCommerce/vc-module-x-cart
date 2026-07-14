using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemCommandHandler : CartConfigurationCommandHandler<UpdateConfigurationItemCommand>
{
    public UpdateConfigurationItemCommandHandler(
        ICartAggregateRepository cartAggregateRepository,
        ICartConfigurationService cartConfigurationService)
        : base(cartAggregateRepository, cartConfigurationService)
    {
    }

    protected override Task ApplyConfigurationAsync(CartAggregate cartAggregate, UpdateConfigurationItemCommand request)
        => cartAggregate.UpdateConfigurationItemAsync(request.LineItemId, request.ConfigurationSection);
}
