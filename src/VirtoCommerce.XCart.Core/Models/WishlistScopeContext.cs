namespace VirtoCommerce.XCart.Core.Models;

public class WishlistScopeContext
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }

    public string SharedWithId { get; set; }

    public string CurrentUserId { get; set; }

    public string CustomerName { get; set; }

    public string CurrentOrganizationId { get; set; }
}
