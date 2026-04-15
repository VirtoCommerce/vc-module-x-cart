using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class RemoveConfigurationItemCommandHandler : CartCommandHandler<RemoveConfigurationItemCommand>
{
    public RemoveConfigurationItemCommandHandler(ICartAggregateRepository cartAggregateRepository)
        : base(cartAggregateRepository)
    {
    }

    public override async Task<CartAggregate> Handle(RemoveConfigurationItemCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        await cartAggregate.RemoveConfigurationItemAsync(request.LineItemId, request.ConfigurationSection);

        return await SaveCartAsync(cartAggregate);
    }
}
