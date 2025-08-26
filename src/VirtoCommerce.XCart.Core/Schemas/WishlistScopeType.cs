using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistScopeType : EnumerationGraphType
    {
        public WishlistScopeType()
        {
            Add(CartSharingScope.Private, value: CartSharingScope.Private, description: "Private scope");
            Add(CartSharingScope.Organization, value: CartSharingScope.Organization, description: "Organization scope");
        }
    }
}
