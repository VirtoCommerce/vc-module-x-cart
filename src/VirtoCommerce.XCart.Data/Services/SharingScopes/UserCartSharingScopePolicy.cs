using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class UserCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.User;

    // Never settable: UpdateScopeAsync has always rejected this scope.
    public override bool CanApply => false;

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return IsOwner(cart, currentUserId);
    }
}
