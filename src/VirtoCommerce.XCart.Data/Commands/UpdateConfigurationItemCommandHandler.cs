using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class UpdateConfigurationItemCommandHandler : CartCommandHandler<UpdateConfigurationItemCommand>
{
    public UpdateConfigurationItemCommandHandler(ICartAggregateRepository cartAggregateRepository)
        : base(cartAggregateRepository)
    {
    }

    public override async Task<CartAggregate> Handle(UpdateConfigurationItemCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        await cartAggregate.UpdateConfigurationItemAsync(request.LineItemId, request.ConfigurationSection);

        return await SaveCartAsync(cartAggregate);
    }
}
