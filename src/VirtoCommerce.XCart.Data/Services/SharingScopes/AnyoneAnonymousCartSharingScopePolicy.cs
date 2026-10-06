using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services.SharingScopes;

public class AnyoneAnonymousCartSharingScopePolicy : CartSharingScopePolicyBase
{
    public override string Scope => CartSharingScope.AnyoneAnonymous;

    public override string Description => "Anyone (anonymous) scope";

    public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        return true;
    }

    public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
    {
        EnsureSetting(cart, context.SharingKey);
        SetOrganization(cart, organizationId: null);

        return Task.CompletedTask;
    }
}
