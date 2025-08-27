using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class CartSharingService : ICartSharingService
{
    public string GetSharingScope(ShoppingCart cart)
    {
        if (cart == null)
        {
            return CartSharingScope.Private;
        }

        if (cart.SharingSettings.IsNullOrEmpty())
        {
            return string.IsNullOrEmpty(cart.OrganizationId) ? CartSharingScope.Private : CartSharingScope.Organization;
        }

        if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAnonymous))
        {
            return CartSharingScope.AnyoneAnonymous;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAuthorized))
        {
            return CartSharingScope.AnyoneAuthorized;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.Organization))
        {
            return CartSharingScope.Organization;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.User))
        {
            return CartSharingScope.User;
        }

        return CartSharingScope.Private;
    }

    public bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
    {
        if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAnonymous))
        {
            return true;
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.AnyoneAuthorized))
        {
            return !string.IsNullOrEmpty(currentUserId);
        }
        else if (cart.SharingSettings.Any(x => x.Scope == CartSharingScope.Organization))
        {
            return !string.IsNullOrEmpty(currentUserId) && cart.OrganizationId == currentOrganizationId;
        }

        return !string.IsNullOrEmpty(currentUserId) && cart.CustomerId == currentUserId;
    }
}
