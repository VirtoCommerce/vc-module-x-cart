using System;
using System.Collections.Generic;
using System.Linq;
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
                // Checked here so the error names the policy; the schema builder reports only the bad string.
                if (!IsValidEnumValueName(policy.Scope))
                {
                    throw new InvalidOperationException(
                        $"{policy.GetType().FullName} declares the sharing scope '{policy.Scope}', which is not a valid GraphQL enum value name.");
                }

                Add(policy.Scope, value: policy.Scope, description: policy.Description);
            }
        }

        private static bool IsValidEnumValueName(string name)
        {
            return !string.IsNullOrEmpty(name)
                && (char.IsAsciiLetter(name[0]) || name[0] == '_')
                && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
        }
    }
}
