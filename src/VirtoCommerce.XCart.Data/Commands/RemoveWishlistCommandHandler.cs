using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class RemoveWishlistCommandHandler : CartCommandHandler<RemoveWishlistCommand>
    {
        public RemoveWishlistCommandHandler(ICartAggregateRepository cartAggrRepository)
            : base(cartAggrRepository)
        {
        }

        public override async Task<CartAggregate> Handle(RemoveWishlistCommand request, CancellationToken cancellationToken)
        {
            await CartRepository.RemoveCartAsync(request.ListId);
            return null;
        }
    }
}
