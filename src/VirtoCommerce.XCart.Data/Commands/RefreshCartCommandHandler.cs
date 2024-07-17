using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    /// <summary>
    /// Loads the cart from its persistent storage, then immediately saves it back without any modifications. This effectively triggers any relevant price updates and clears any warnings or errors that may have been present.
    /// </summary>
    public class RefreshCartCommandHandler : CartCommandHandler<RefreshCartCommand>
    {
        public RefreshCartCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(RefreshCartCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
