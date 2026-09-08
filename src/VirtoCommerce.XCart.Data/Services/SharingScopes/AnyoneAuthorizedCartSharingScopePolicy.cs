using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class AnyoneAuthorizedCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.AnyoneAuthorized;

    public override string Description => "Anyone (authorized) scope";

    // Resolvable and authorizable, but never settable: UpdateScopeAsync has always rejected it.
    public override bool CanApply => false;

    public override string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return IsOwner(cart, currentUserId) ? CartSharingAccess.Write : CartSharingAccess.Read;
    }

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return !string.IsNullOrEmpty(currentUserId);
    }
}
