using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class AnyoneAnonymousCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.AnyoneAnonymous;

    public override string Description => "Anyone (anonymous) scope";

    public override string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return IsOwner(cart, currentUserId) ? CartSharingAccess.Write : CartSharingAccess.Read;
    }

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return true;
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read, sharedWithId: null);
        SetOwner(cart, context.CurrentUserId, context.CustomerName, organizationId: null);

        return Task.CompletedTask;
    }
}
