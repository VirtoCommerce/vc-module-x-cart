using System.Collections.Generic;
using Moq;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Data.Services.SharingScopes;

namespace VirtoCommerce.XCart.Tests.Helpers
{
    /// <summary>
    /// The built-in sharing scopes in registration order — the order XCart registers them, which is also the
    /// GraphQL <c>WishlistScopeType</c> enum order.
    /// </summary>
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

        /// <summary>
        /// A real dispatcher over the given policies (the built-in ones by default). The aggregate repository is
        /// only used by GetWishlistBySharingKeyAsync, so it is mocked.
        /// </summary>
        public static CartSharingService SharingService(IList<ICartSharingScopePolicy> policies = null)
        {
            return new CartSharingService(Mock.Of<ICartAggregateRepository>(), policies ?? BuiltInPolicies());
        }
    }
}
