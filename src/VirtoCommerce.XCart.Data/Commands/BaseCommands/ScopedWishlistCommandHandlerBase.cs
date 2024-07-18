using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;
using static VirtoCommerce.XCart.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands.BaseCommands;

public abstract class ScopedWishlistCommandHandlerBase<TCommand> : CartCommandHandler<TCommand>
    where TCommand : ScopedWishlistCommand
{
    protected ScopedWishlistCommandHandlerBase(ICartAggregateRepository cartAggregateRepository)
        : base(cartAggregateRepository)
    {
    }

    protected virtual Task UpdateScopeAsync(CartAggregate cartAggregate, TCommand request)
    {
        if (request.Scope?.EqualsIgnoreCase(OrganizationScope) == true)
        {
            if (!string.IsNullOrEmpty(request.WishlistUserContext.CurrentOrganizationId))
            {
                cartAggregate.Cart.OrganizationId = request.WishlistUserContext.CurrentOrganizationId;
            }
        }
        else if (request.Scope?.EqualsIgnoreCase(PrivateScope) == true)
        {
            cartAggregate.Cart.OrganizationId = null;

            cartAggregate.Cart.CustomerId = request.WishlistUserContext.CurrentUserId;
            cartAggregate.Cart.CustomerName = request.WishlistUserContext.CurrentContact.Name;
        }

        return Task.CompletedTask;
    }
}
