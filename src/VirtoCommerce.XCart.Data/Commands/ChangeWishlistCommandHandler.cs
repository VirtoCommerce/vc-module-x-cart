using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class ChangeWishlistCommandHandler : ScopedWishlistCommandHandlerBase<ChangeWishlistCommand>
    {
        public ChangeWishlistCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartSharingService cartSharingService)
            : base(cartAggregateRepository, cartSharingService)
        {
        }

        public override async Task<CartAggregate> Handle(ChangeWishlistCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = request.WishlistUserContext.Cart == null
                ? await CartRepository.GetCartByIdAsync(request.ListId, request.CultureName)
                : await CartRepository.GetCartForShoppingCartAsync(request.WishlistUserContext.Cart, request.CultureName);

            if (request.ListName != null)
            {
                cartAggregate.Cart.Name = request.ListName;
            }

            if (request.Description != null)
            {
                cartAggregate.Cart.Description = request.Description;
            }

            await UpdateScopeAsync(cartAggregate, request);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
