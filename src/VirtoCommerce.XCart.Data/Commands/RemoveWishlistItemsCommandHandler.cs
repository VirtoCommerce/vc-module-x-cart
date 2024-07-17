using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class RemoveWishlistItemsCommandHandler : CartCommandHandler<RemoveWishlistItemsCommand>
    {
        public RemoveWishlistItemsCommandHandler(ICartAggregateRepository cartAggregateRepository)
            : base(cartAggregateRepository)
        {
        }

        public override async Task<CartAggregate> Handle(RemoveWishlistItemsCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await CartRepository.GetCartByIdAsync(request.ListId);

            foreach (var lineItemId in request.LineItemIds)
            {
                await cartAggregate.RemoveItemAsync(lineItemId);
            }

            return await SaveCartAsync(cartAggregate);
        }
    }
}
