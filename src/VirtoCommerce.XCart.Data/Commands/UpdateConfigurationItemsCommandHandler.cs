using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemsCommandHandler : CartCommandHandler<UpdateConfigurationItemsCommand>
{
    public UpdateConfigurationItemsCommandHandler(ICartAggregateRepository cartAggregateRepository)
        : base(cartAggregateRepository)
    {
    }

    public override async Task<CartAggregate> Handle(UpdateConfigurationItemsCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        await cartAggregate.UpdateConfigurationItemsAsync(request.LineItemId, request.ConfigurationSections);

        return await SaveCartAsync(cartAggregate);
    }
}
