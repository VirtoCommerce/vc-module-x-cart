using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class WishlistScopeContext
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }

    public IList<string> AddSharedWithIds { get; set; }

    public IList<string> RemoveSharedWithIds { get; set; }

    // The released storefront's single recipient; folded into the deltas by ICartSharingService.
    public string LegacySharedWithId { get; set; }

    public string Message { get; set; }

    public string CurrentUserId { get; set; }

    public string CustomerName { get; set; }

    public string CurrentOrganizationId { get; set; }
}
