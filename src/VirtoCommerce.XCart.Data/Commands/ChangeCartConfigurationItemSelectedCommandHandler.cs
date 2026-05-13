using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeCartConfigurationItemSelectedCommandHandler : CartCommandHandler<ChangeCartConfigurationItemSelectedCommand>
    {
        public ChangeCartConfigurationItemSelectedCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ChangeCartConfigurationItemSelectedCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.ChangeConfigurationItemSelectedAsync(request.LineItemId, request.ConfigurationSection, request.SelectedForCheckout);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
