using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class UserCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.User;

    // Resolvable and authorizable, but never settable: UpdateScopeAsync has always rejected it.
    public override bool CanApply => false;

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return IsOwner(cart, currentUserId);
    }
}
