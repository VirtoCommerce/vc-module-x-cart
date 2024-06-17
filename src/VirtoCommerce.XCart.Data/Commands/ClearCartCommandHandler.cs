using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ClearCartCommandHandler : CartCommandHandler<ClearCartCommand>
    {
        public ClearCartCommandHandler(ICartAggregateRepository cartAggrFactory)
            : base(cartAggrFactory)
        {
        }

        public override async Task<CartAggregate> Handle(ClearCartCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            await cartAggregate.ClearAsync();

            return await SaveCartAsync(cartAggregate);
        }
    }
}
