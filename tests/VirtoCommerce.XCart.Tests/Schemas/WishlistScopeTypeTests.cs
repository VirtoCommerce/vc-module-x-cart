using System;
using System.Linq;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Schemas
{
    /// <summary>
    /// The wishlist scope enum is assembled from the registered scope policies, so its values are part of the
    /// published GraphQL schema and a downstream module can extend them. Pins both.
    /// </summary>
    public class WishlistScopeTypeTests
    {
        [Fact]
        public void Values_AreTheBuiltInScopes_InRegistrationOrder()
        {
            var type = new WishlistScopeType(CartSharingScopeFixtures.BuiltInPolicies());

            type.Values.Select(x => x.Name).Should().Equal(
                CartSharingScope.Private,
                CartSharingScope.AnyoneAnonymous,
                CartSharingScope.AnyoneAuthorized,
                CartSharingScope.Organization,
                CartSharingScope.User);
        }

        [Fact]
        public void Values_CarryThePolicyDescriptions()
        {
            var type = new WishlistScopeType(CartSharingScopeFixtures.BuiltInPolicies());

            type.Values.Single(x => x.Name == CartSharingScope.AnyoneAnonymous)
                .Description.Should().Be("Anyone (anonymous) scope");
            type.Values.Single(x => x.Name == CartSharingScope.Private)
                .Description.Should().Be("Private scope");
        }

        [Fact]
        public void Values_IncludeADownstreamPolicysScope()
        {
            var policies = CartSharingScopeFixtures.BuiltInPolicies();
            policies.Add(new TestScopePolicy("Customer"));

            var type = new WishlistScopeType(policies);

            type.Values.Select(x => x.Name).Should().Contain("Customer");
        }

        [Theory]
        [InlineData("Not A Name")]
        [InlineData("with-hyphen")]
        [InlineData("1LeadingDigit")]
        public void Constructor_PolicyWithAnIllegalEnumValueName_ThrowsNamingThePolicy(string scope)
        {
            var policies = CartSharingScopeFixtures.BuiltInPolicies();
            policies.Add(new TestScopePolicy(scope));

            var act = () => new WishlistScopeType(policies);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{nameof(TestScopePolicy)}*")
                .WithMessage($"*{scope}*");
        }

        private sealed class TestScopePolicy(string scope) : CartSharingScopePolicyBase
        {
            public override string Scope => scope;

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId) =>
                IsOwner(cart, currentUserId);
        }
    }
}
