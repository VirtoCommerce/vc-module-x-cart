using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Services;

public interface ICartSharingScopeCompatibilityService
{
    string GetSharingScope(ShoppingCart cart);
}
