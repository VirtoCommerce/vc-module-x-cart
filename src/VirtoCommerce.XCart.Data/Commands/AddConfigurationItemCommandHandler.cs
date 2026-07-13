using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class AddConfigurationItemCommandHandler : CartConfigurationCommandHandler<AddConfigurationItemCommand>
{
    public AddConfigurationItemCommandHandler(
        ICartAggregateRepository cartAggregateRepository,
        ICartConfigurationService cartConfigurationService)
        : base(cartAggregateRepository, cartConfigurationService)
    {
    }

    protected override Task ApplyConfigurationAsync(CartAggregate cartAggregate, AddConfigurationItemCommand request)
        => cartAggregate.AddConfigurationItemAsync(request.LineItemId, request.ConfigurationSection);
}
