using System.Collections.Generic;

namespace VirtoCommerce.XCart.Core.Models;

public class WishlistScopeContext
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }

    public IList<string> AddSharedWithIds { get; set; }

    public IList<string> RemoveSharedWithIds { get; set; }

    public string Message { get; set; }

    public string CurrentUserId { get; set; }

    public string CustomerName { get; set; }

    public string CurrentOrganizationId { get; set; }
}
