using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangePurchaseOrderNumberCommandHandler : CartCommandHandler<ChangePurchaseOrderNumberCommand>
    {
        public ChangePurchaseOrderNumberCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(ChangePurchaseOrderNumberCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);
            await cartAggregate.ChangePurchaseOrderNumber(request.PurchaseOrderNumber);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
