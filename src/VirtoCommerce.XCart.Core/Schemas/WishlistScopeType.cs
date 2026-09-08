using System.Collections.Generic;
using GraphQL.Types;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class WishlistScopeType : EnumerationGraphType
    {
        public WishlistScopeType(IEnumerable<ICartSharingScopePolicy> scopePolicies)
        {
            foreach (var policy in scopePolicies)
            {
                Add(policy.Scope, value: policy.Scope, description: policy.Description);
            }
        }
    }
}
