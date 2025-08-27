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
        if (request.Scope?.EqualsIgnoreCase(CartSharingScope.AnyoneAnonymous) == true)
        {
            EnsureSharingSettings(cartAggregate.Cart, request.SharingKey, CartSharingScope.AnyoneAnonymous, CartSharingAccess.Read);

            cartAggregate.Cart.OrganizationId = null;
        }
        else if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Organization) == true)
        {
            EnsureSharingSettings(cartAggregate.Cart, request.SharingKey, CartSharingScope.Organization, CartSharingAccess.Write);

            if (!string.IsNullOrEmpty(request.WishlistUserContext.CurrentOrganizationId))
            {
                cartAggregate.Cart.OrganizationId = request.WishlistUserContext.CurrentOrganizationId;
            }
        }
        else if (request.Scope?.EqualsIgnoreCase(CartSharingScope.Private) == true)
        {
            EnsureSharingSettings(cartAggregate.Cart, null, CartSharingScope.Private, CartSharingAccess.Read);

            cartAggregate.Cart.OrganizationId = null;
            cartAggregate.Cart.CustomerId = request.WishlistUserContext.CurrentUserId;
            cartAggregate.Cart.CustomerName = request.WishlistUserContext.CurrentContact.Name;
        }

        return Task.CompletedTask;
    }

    protected void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access)
    {
        if (cart.SharingSettings.IsNullOrEmpty())
        {
            var sharingSetting = AbstractTypeFactory<CartSharingSetting>.TryCreateInstance();

            sharingSetting.Id = sharingKey;
            sharingSetting.ShoppingCartId = cart.Id;
            sharingSetting.Scope = mode;
            sharingSetting.Access = access;

            cart.SharingSettings.Add(sharingSetting);
        }
        else
        {
            foreach (var setting in cart.SharingSettings)
            {
                setting.Scope = CartSharingScope.Private;
            }

            var sharingSetting = cart.SharingSettings.First();

            sharingSetting.Scope = mode;
            sharingSetting.Access = access;
        }
    }
}
