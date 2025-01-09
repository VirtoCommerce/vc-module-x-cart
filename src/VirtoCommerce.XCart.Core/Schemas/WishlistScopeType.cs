using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistScopeType : EnumerationGraphType
    {
        public WishlistScopeType()
        {
            Add(ModuleConstants.PrivateScope, value: ModuleConstants.PrivateScope, description: "Private scope");
            Add(ModuleConstants.OrganizationScope, value: ModuleConstants.OrganizationScope, description: "Organization scope");
        }
    }
}
