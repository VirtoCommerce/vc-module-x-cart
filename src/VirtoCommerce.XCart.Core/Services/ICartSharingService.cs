using System;
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

    [Obsolete("Use the overload with sharedWithId (null for the built-in non-targeted scopes).", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access);

    void EnsureSharingSettings(ShoppingCart cart, string sharingKey, string mode, string access, string sharedWithId);

    Task UpdateScopeAsync(ShoppingCart cart, WishlistScopeContext context);

    Task<CartAggregate> GetWishlistBySharingKeyAsync(string sharingKey, IList<string> includeFields);
}
