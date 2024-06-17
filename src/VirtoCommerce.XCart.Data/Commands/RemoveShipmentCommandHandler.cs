using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class RemoveShipmentCommandHandler : CartCommandHandler<RemoveShipmentCommand>
    {
        public RemoveShipmentCommandHandler(ICartAggregateRepository cartRepository)
            : base(cartRepository)
        {
        }

        public override async Task<CartAggregate> Handle(RemoveShipmentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            await cartAggregate.RemoveShipmentAsync(request.ShipmentId);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
