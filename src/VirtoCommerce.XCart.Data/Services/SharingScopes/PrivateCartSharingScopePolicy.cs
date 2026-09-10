using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class PrivateCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.Private;

    public override string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return CartSharingAccess.Write;
    }

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return IsOwner(cart, currentUserId);
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        EnsureSetting(cart, sharingKey: null, CartSharingAccess.Write, sharedWithId: null);
        SetOwner(cart, context.CurrentUserId, context.CustomerName, organizationId: null);

        return Task.CompletedTask;
    }

    public override void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria)
    {
        criteria.OrganizationId = null;
    }
}
