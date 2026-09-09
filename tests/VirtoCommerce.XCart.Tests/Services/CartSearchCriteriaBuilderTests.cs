using System;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Services
{
    /// <summary>
    /// Pins the scope narrowing applied by <see cref="CartSearchCriteriaBuilder.WithScope"/>, which routes through
    /// the sharing scope registry instead of hard-coding Organization/Private.
    /// </summary>
    public class CartSearchCriteriaBuilderTests
    {
        private const string CustomerId = "customer-1";
        private const string OrgId = "org-1";

        private static CartSearchCriteriaBuilder Builder(ICartSharingService sharingService) =>
            new(Mock.Of<ISearchPhraseParser>(), Mock.Of<IXCartMapper>(), sharingService);

        [Fact]
        public void WithScope_Organization_DropsTheCustomerNarrowing()
        {
            var criteria = Builder(CartSharingScopeFixtures.SharingService())
                .WithCustomerId(CustomerId)
                .WithOrganizationId(OrgId)
                .WithScope(CartSharingScope.Organization)
                .Build();

            criteria.CustomerOrOrganization.Should().BeTrue();
            criteria.CustomerId.Should().BeNull();
            criteria.OrganizationId.Should().Be(OrgId);
        }

        [Fact]
        public void WithScope_Private_DropsTheOrganizationNarrowing()
        {
            var criteria = Builder(CartSharingScopeFixtures.SharingService())
                .WithCustomerId(CustomerId)
                .WithOrganizationId(OrgId)
                .WithScope(CartSharingScope.Private)
                .Build();

            criteria.CustomerOrOrganization.Should().BeTrue();
            criteria.CustomerId.Should().Be(CustomerId);
            criteria.OrganizationId.Should().BeNull();
        }

        [Theory]
        [InlineData(CartSharingScope.AnyoneAnonymous)]
        [InlineData("NoPolicyRegistered")]
        public void WithScope_ScopeThatNarrowsNothing_KeepsBothIds(string scope)
        {
            // Documents inherited behaviour: a scope with no narrowing of its own (and an unregistered scope) is
            // left to the CustomerOrOrganization filter alone, so the caller sees their own and their org's lists.
            var criteria = Builder(CartSharingScopeFixtures.SharingService())
                .WithCustomerId(CustomerId)
                .WithOrganizationId(OrgId)
                .WithScope(scope)
                .Build();

            criteria.CustomerOrOrganization.Should().BeTrue();
            criteria.CustomerId.Should().Be(CustomerId);
            criteria.OrganizationId.Should().Be(OrgId);
        }

        [Fact]
        public void WithScope_BuilderConstructedWithoutTheSharingService_Throws()
        {
            var builder = new CartSearchCriteriaBuilder(Mock.Of<ISearchPhraseParser>(), Mock.Of<IXCartMapper>());

            var act = () => builder.WithScope(CartSharingScope.Organization);

            act.Should().Throw<InvalidOperationException>().WithMessage("*cart sharing service*");
        }

        [Fact]
        public void WithScope_DownstreamPolicy_AppliesItsOwnNarrowing()
        {
            var policies = CartSharingScopeFixtures.BuiltInPolicies();
            policies.Add(new OrganizationlessScopePolicy());

            var criteria = Builder(CartSharingScopeFixtures.SharingService(policies))
                .WithCustomerId(CustomerId)
                .WithOrganizationId(OrgId)
                .WithScope(OrganizationlessScopePolicy.ScopeName)
                .Build();

            criteria.CustomerId.Should().Be(CustomerId);
            criteria.OrganizationId.Should().BeNull();
        }

        private sealed class OrganizationlessScopePolicy : CartSharingScopePolicyBase
        {
            public const string ScopeName = "Organizationless";

            public override string Scope => ScopeName;

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId) =>
                IsOwner(cart, currentUserId);

            public override void ConfigureSearchCriteria(ShoppingCartSearchCriteria criteria) =>
                criteria.OrganizationId = null;
        }
    }
}
