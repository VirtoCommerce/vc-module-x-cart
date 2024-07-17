using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ClearPaymentsCommandHandler : CartCommandHandler<ClearPaymentsCommand>
    {
        public ClearPaymentsCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ClearPaymentsCommand request, CancellationToken cancellationToken)
        {
            var aggregate = await GetOrCreateCartFromCommandAsync(request);
            if (aggregate == null)
            {
                return null;
            }

            aggregate.Cart.Payments.Clear();

            return await SaveCartAsync(aggregate);
        }
    }
}
