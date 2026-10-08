using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models
{
    public class WishlistUserContext
    {
        public string CurrentUserId { get; set; }

        public string CurrentOrganizationId { get; set; }

        public Contact CurrentContact { get; set; }

        public ShoppingCart Cart { get; set; }

        public string UserId { get; set; }

        public string Scope { get; set; }

        public string RequestedAccess { get; set; }

        // Removing a list is an ownership act rather than a write: every member of an organization-scoped list
        // holds Write, so Write cannot be what gates it. Renaming and editing items stay open to them.
        public bool RequireOwner { get; set; }
    }
}
