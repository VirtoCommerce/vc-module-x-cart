namespace VirtoCommerce.XCart.Core.Models;

// Inputs for applying a wishlist sharing scope, decoupled from the command shape so ICartSharingService.UpdateScopeAsync
// stays command-agnostic. Scope defines SharedWithId's id space (null for the built-in non-targeted scopes).
public class WishlistScopeContext
{
    public string Scope { get; set; }

    public string SharingKey { get; set; }

    public string SharedWithId { get; set; }

    public string CurrentUserId { get; set; }

    public string CustomerName { get; set; }

    public string CurrentOrganizationId { get; set; }
}
