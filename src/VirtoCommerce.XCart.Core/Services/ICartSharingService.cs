using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Services;

public interface ICartSharingService
{
    string GetSharingScope(ShoppingCart cart);
    string GetSharingAccess(ShoppingCart cart, string currentUserId);
    bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId);

    void SetOwner(ShoppingCart cart, string userId, string customerName, string organizationId);
    string GetSharingOwnerUserId(ShoppingCart cart);
    string GetSharingOwnerOrganizationId(ShoppingCart cart);

    void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access);
}
