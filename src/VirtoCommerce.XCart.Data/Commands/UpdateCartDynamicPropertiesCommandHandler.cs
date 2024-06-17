using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class UpdateCartDynamicPropertiesCommandHandler : CartCommandHandler<UpdateCartDynamicPropertiesCommand>
    {
        public UpdateCartDynamicPropertiesCommandHandler(ICartAggregateRepository cartRepository)
            : base(cartRepository)
        {
        }

        public override async Task<CartAggregate> Handle(UpdateCartDynamicPropertiesCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.UpdateCartDynamicProperties(request.DynamicProperties);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
