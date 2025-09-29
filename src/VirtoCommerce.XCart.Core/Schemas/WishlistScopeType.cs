using GraphQL.Types;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistScopeType : EnumerationGraphType
    {
        public WishlistScopeType()
        {
            Add(CartSharingScope.Private, value: CartSharingScope.Private, description: "Private scope");
            Add(CartSharingScope.AnyoneAnonymous, value: CartSharingScope.AnyoneAnonymous, description: "Anyone (anonymous) scope");
            Add(CartSharingScope.AnyoneAuthorized, value: CartSharingScope.AnyoneAuthorized, description: "Anyone (authorized) scope");
            Add(CartSharingScope.Organization, value: CartSharingScope.Organization, description: "Organization scope");
            Add(CartSharingScope.User, value: CartSharingScope.User, description: "User scope");
        }
    }
}
