using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class OrganizationCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.Organization;

    public override string GetAccess(ShoppingCart cart, string currentUserId)
    {
        return CartSharingAccess.Write;
    }

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return !string.IsNullOrEmpty(currentUserId) && cart.OrganizationId.EqualsIgnoreCase(currentOrganizationId);
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        EnsureSetting(cart, context.SharingKey, CartSharingAccess.Write);
        SetOwner(cart, context.CurrentUserId, context.CustomerName, context.CurrentOrganizationId);

        return Task.CompletedTask;
    }

    public override void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria)
    {
        criteria.CustomerId = null;
    }
}
