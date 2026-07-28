using System.Collections.Generic;
using System.Threading.Tasks;
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

    void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access, string sharedWithId = null);

    // Authorizes the caller to persist a sharing setting with the given scope and target before it is written.
    // The base pipeline has no targeted scope and does nothing here; a module that adds a targeted scope (e.g. a
    // Sales Rep publishing to a customer organization) overrides this to enforce who may target whom. Throws when
    // the caller is not allowed.
    Task AuthorizeSharingAsync(string scope, string sharedWithId, string currentUserId);

    Task<CartAggregate> GetWishlistBySharingKeyAsync(string sharingKey, IList<string> includeFields);
}
