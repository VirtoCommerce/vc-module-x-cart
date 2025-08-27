using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Services;

public interface ICartSharingService
{
    string GetSharingScope(ShoppingCart cart);
    bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId);
}
