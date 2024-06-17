using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class RemoveWishlistItemCommandHandler : CartCommandHandler<RemoveWishlistItemCommand>
    {
        public RemoveWishlistItemCommandHandler(ICartAggregateRepository cartAggrRepository)
            : base(cartAggrRepository)
        {
        }

        public override async Task<CartAggregate> Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await CartRepository.GetCartByIdAsync(request.ListId);

            if (!string.IsNullOrEmpty(request.LineItemId))
            {
                await cartAggregate.RemoveItemAsync(request.LineItemId);
            }

            if (!string.IsNullOrEmpty(request.ProductId))
            {
                await cartAggregate.RemoveItemsByProductIdAsync(request.ProductId);
            }

            return await SaveCartAsync(cartAggregate);
        }
    }
}
