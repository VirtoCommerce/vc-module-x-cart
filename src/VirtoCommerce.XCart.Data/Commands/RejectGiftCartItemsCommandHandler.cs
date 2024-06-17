using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class RejectGiftCartItemsCommandHandler : CartCommandHandler<RejectGiftCartItemsCommand>
    {
        public RejectGiftCartItemsCommandHandler(ICartAggregateRepository cartRepository)
            : base(cartRepository)
        {
        }

        public override async Task<CartAggregate> Handle(RejectGiftCartItemsCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            cartAggregate.RejectCartItems(request.Ids);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
