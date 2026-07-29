using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

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

    // Authorizes and applies the requested sharing scope to the cart (writes the sharing setting + owner). Throws
    // when the scope is unsupported or the caller is not allowed to use it. A null/empty scope is a no-op (e.g. a
    // rename-only edit that does not touch sharing).
    Task UpdateScopeAsync(ShoppingCart cart, WishlistScopeContext context);

    Task<CartAggregate> GetWishlistBySharingKeyAsync(string sharingKey, IList<string> includeFields);
}
