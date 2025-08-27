using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Services;

public class CartSharingScopeCompatibilityService : ICartSharingScopeCompatibilityService
{
    public string GetSharingScope(ShoppingCart cart)
    {
        if (cart == null)
        {
            return CartSharingScope.Private;
        }

        return cart.SharingSettings?.FirstOrDefault()?.Scope ??
            (string.IsNullOrEmpty(cart.OrganizationId) ? CartSharingScope.Private : CartSharingScope.Organization);
    }
}
