using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Commands.BaseCommands;
using static VirtoCommerce.XCart.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class CreateWishlistCommandHandler : ScopedWishlistCommandHandlerBase<CreateWishlistCommand>
    {
        public CreateWishlistCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartSharingService cartSharingService)
            : base(cartAggregateRepository, cartSharingService)
        {
        }

        public override async Task<CartAggregate> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
        {
            request.CartType = ListTypeName;

            var cartAggregate = await CreateNewCartAggregateAsync(request);
            cartAggregate.Cart.Description = request.Description;
            await UpdateScopeAsync(cartAggregate, request);

            return await SaveCartAsync(cartAggregate);
        }
    }
}
