using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeCartConfigurationItemsSelectedCommandHandler : CartCommandHandler<ChangeCartConfigurationItemsSelectedCommand>
    {
        public ChangeCartConfigurationItemsSelectedCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ChangeCartConfigurationItemsSelectedCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.ChangeConfigurationItemsSelectedAsync(request.LineItemId, request.ConfigurationSections, request.SelectedForCheckout);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
