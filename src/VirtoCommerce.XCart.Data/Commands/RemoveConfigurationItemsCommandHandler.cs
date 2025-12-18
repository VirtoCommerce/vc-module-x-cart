using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class RemoveConfigurationItemsCommandHandler : CartCommandHandler<RemoveConfigurationItemsCommand>
{
    public RemoveConfigurationItemsCommandHandler(ICartAggregateRepository cartAggregateRepository)
        : base(cartAggregateRepository)
    {
    }

    public override async Task<CartAggregate> Handle(RemoveConfigurationItemsCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        await cartAggregate.RemoveConfigurationItemsAsync(request.LineItemId, request.ConfigurationSections);

        return await SaveCartAsync(cartAggregate);
    }
}
