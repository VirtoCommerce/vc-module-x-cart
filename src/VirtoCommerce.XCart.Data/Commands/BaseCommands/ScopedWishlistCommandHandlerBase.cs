using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

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
        if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Anyone) == true)
        {
            EnsureActiveSharingSettings(cartAggregate.Cart, CartSharingScope.Anyone, CartSharingAccess.Read);
        }
        else if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Organization) == true)
        {
            EnsureActiveSharingSettings(cartAggregate.Cart, CartSharingScope.Organization, CartSharingAccess.Write);

            if (!string.IsNullOrEmpty(request.WishlistUserContext.CurrentOrganizationId))
            {
                cartAggregate.Cart.OrganizationId = request.WishlistUserContext.CurrentOrganizationId;
            }
        }
        else if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Private) == true)
        {
            DeactivateSharingSettings(cartAggregate.Cart);

            cartAggregate.Cart.OrganizationId = null;

            cartAggregate.Cart.CustomerId = request.WishlistUserContext.CurrentUserId;
            cartAggregate.Cart.CustomerName = request.WishlistUserContext.CurrentContact.Name;
        }

        return Task.CompletedTask;
    }

    protected void EnsureActiveSharingSettings(ShoppingCart cart, string mode, string access)
    {
        if (cart.SharingSettings.Count == 0)
        {
            cart.SharingSettings.Add(new CartSharingSetting
            {
                ShoppingCartId = cart.Id,
                Scope = mode,
                Access = access,
                IsActive = true
            });
        }
        else
        {
            DeactivateSharingSettings(cart);

            var sharingSetting = cart.SharingSettings.First();
            sharingSetting.IsActive = true;
            sharingSetting.Scope = mode;
            sharingSetting.Access = access;
        }
    }

    protected void DeactivateSharingSettings(ShoppingCart cart)
    {
        foreach (var setting in cart.SharingSettings)
        {
            setting.IsActive = false;
        }
    }
}
