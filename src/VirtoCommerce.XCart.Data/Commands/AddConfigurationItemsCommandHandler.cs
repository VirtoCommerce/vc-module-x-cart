using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands;

public class AddConfigurationItemsCommandHandler : CartCommandHandler<AddConfigurationItemsCommand>
{
    public AddConfigurationItemsCommandHandler(ICartAggregateRepository cartAggregateRepository)
        : base(cartAggregateRepository)
    {
    }

    public override async Task<CartAggregate> Handle(AddConfigurationItemsCommand request, CancellationToken cancellationToken)
    {
        var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

        await cartAggregate.AddConfigurationItemsAsync(request.LineItemId, request.ConfigurationSections);

        return await SaveCartAsync(cartAggregate);
    }
}
