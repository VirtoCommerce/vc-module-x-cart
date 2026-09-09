using System.Collections.Generic;
using Moq;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Data.Services.SharingScopes;

namespace VirtoCommerce.XCart.Tests.Helpers
{
    // Built-in scopes in registration order - also the WishlistScopeType enum order.
    public static class CartSharingScopeFixtures
    {
        public static List<ICartSharingScopePolicy> BuiltInPolicies()
        {
            return
            [
                new PrivateCartSharingScopePolicy(),
                new AnyoneAnonymousCartSharingScopePolicy(),
                new AnyoneAuthorizedCartSharingScopePolicy(),
                new OrganizationCartSharingScopePolicy(),
                new UserCartSharingScopePolicy(),
            ];
        }

        // Real dispatcher; the repository is only used by GetWishlistBySharingKeyAsync.
        public static CartSharingService SharingService(IList<ICartSharingScopePolicy> policies = null)
        {
            return new CartSharingService(Mock.Of<ICartAggregateRepository>(), policies ?? BuiltInPolicies());
        }
    }
}
