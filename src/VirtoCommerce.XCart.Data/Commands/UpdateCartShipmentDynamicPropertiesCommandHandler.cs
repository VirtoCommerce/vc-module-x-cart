using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class UpdateCartShipmentDynamicPropertiesCommandHandler : CartCommandHandler<UpdateCartShipmentDynamicPropertiesCommand>
    {
        public UpdateCartShipmentDynamicPropertiesCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(UpdateCartShipmentDynamicPropertiesCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            await cartAggregate.UpdateCartShipmentDynamicProperties(request.ShipmentId, request.DynamicProperties);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
