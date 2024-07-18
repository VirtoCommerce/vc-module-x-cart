using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ClearShipmentsCommandHandler : CartCommandHandler<ClearShipmentsCommand>
    {
        public ClearShipmentsCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ClearShipmentsCommand request, CancellationToken cancellationToken)
        {
            var aggregate = await GetOrCreateCartFromCommandAsync(request);
            if (aggregate == null)
            {
                return null;
            }

            aggregate.Cart.Shipments.Clear();

            return await SaveCartAsync(aggregate);
        }
    }
}
