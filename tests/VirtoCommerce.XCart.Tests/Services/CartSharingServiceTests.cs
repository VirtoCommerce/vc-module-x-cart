using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Data.Services.SharingScopes;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Services
{
    public class CartSharingServiceTests
    {
        private const string OwnerId = "owner-user";
        private const string OtherUserId = "other-user";
        private const string OrgId = "org-1";
        private const string CustomScope = "Customer";
        private const string UnknownScope = "NoPolicyRegistered";

        [Fact]
        public void Constructor_TwoPoliciesForOneScope_Throws()
        {
            var policies = BuiltInPolicies();
            policies.Add(new TestScopePolicy(CartSharingScope.Organization));

            var act = () => CreateService(policies);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Organization*")
                .WithMessage($"*{nameof(OrganizationCartSharingScopePolicy)}*");
        }

        [Fact]
        public void Constructor_PolicyWithoutScope_Throws()
        {
            var act = () => CreateService([new TestScopePolicy(scope: null)]);

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{nameof(TestScopePolicy)}*");
        }

        [Theory]
        [InlineData(CartSharingScope.Private)]
        [InlineData(CartSharingScope.Organization)]
        [InlineData(CartSharingScope.AnyoneAnonymous)]
        [InlineData(CartSharingScope.AnyoneAuthorized)]
        [InlineData(CartSharingScope.User)]
        public void GetSharingScope_SettingScope_IsReturned(string scope)
        {
            CreateService().GetSharingScope(CartWithScope(scope)).Should().Be(scope);
        }

        [Fact]
        public void GetSharingScope_NullCartOrNoSettings_FallsBackToPrivate()
        {
            var service = CreateService();

            service.GetSharingScope(null).Should().Be(CartSharingScope.Private);
            service.GetSharingScope(new ShoppingCart()).Should().Be(CartSharingScope.Private);
        }

        [Fact]
        public void GetSharingScope_NoSettingsButOrganization_InfersOrganization()
        {
            var cart = new ShoppingCart { OrganizationId = OrgId };

            CreateService().GetSharingScope(cart).Should().Be(CartSharingScope.Organization);
        }

        [Fact]
        public void GetSharingScope_UnknownScope_FallsBackToPrivate()
        {
            CreateService().GetSharingScope(CartWithScope(UnknownScope)).Should().Be(CartSharingScope.Private);
        }

        [Theory]
        [InlineData(CartSharingScope.Private, CartSharingAccess.Write)]
        [InlineData(CartSharingScope.Organization, CartSharingAccess.Write)]
        [InlineData(CartSharingScope.User, CartSharingAccess.Read)]
        [InlineData(CartSharingScope.AnyoneAnonymous, CartSharingAccess.Read)]
        [InlineData(CartSharingScope.AnyoneAuthorized, CartSharingAccess.Read)]
        public void GetSharingAccess_NonOwner_MatchesScope(string scope, string expected)
        {
            CreateService().GetSharingAccess(CartWithScope(scope), OtherUserId).Should().Be(expected);
        }

        [Theory]
        [InlineData(CartSharingScope.AnyoneAnonymous)]
        [InlineData(CartSharingScope.AnyoneAuthorized)]
        public void GetSharingAccess_OwnerOfPubliclySharedCart_GetsWrite(string scope)
        {
            CreateService().GetSharingAccess(CartWithScope(scope), OwnerId).Should().Be(CartSharingAccess.Write);
        }

        [Fact]
        public void IsAuthorized_NoSettingsButOrganizationMatches_StillRequiresOwnership()
        {
            // GetSharingScope infers "Organization" for a cart that carries an OrganizationId but was never shared.
            // IsAuthorized must not follow that inference, or every org member would reach every private cart.
            var cart = new ShoppingCart { CustomerId = OwnerId, OrganizationId = OrgId };
            var service = CreateService();

            service.GetSharingScope(cart).Should().Be(CartSharingScope.Organization);

            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeFalse();
            service.IsAuthorized(cart, OwnerId, OrgId).Should().BeTrue();
        }

        [Fact]
        public void IsAuthorized_OrganizationScopeShared_AllowsOrganizationMember()
        {
            var cart = CartWithScope(CartSharingScope.Organization);
            cart.OrganizationId = OrgId;

            var service = CreateService();

            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeTrue();
            service.IsAuthorized(cart, OtherUserId, "another-org").Should().BeFalse();
            service.IsAuthorized(cart, currentUserId: null, OrgId).Should().BeFalse();
        }

        [Fact]
        public void IsAuthorized_AnyoneScopes_MatchAnonymousAndAuthenticated()
        {
            var service = CreateService();

            service.IsAuthorized(CartWithScope(CartSharingScope.AnyoneAnonymous), null, null).Should().BeTrue();
            service.IsAuthorized(CartWithScope(CartSharingScope.AnyoneAuthorized), null, null).Should().BeFalse();
            service.IsAuthorized(CartWithScope(CartSharingScope.AnyoneAuthorized), OtherUserId, null).Should().BeTrue();
        }

        [Theory]
        [InlineData(CartSharingScope.Private)]
        [InlineData(CartSharingScope.User)]
        [InlineData(UnknownScope)]
        public void IsAuthorized_OwnerOnlyScopes_FailClosedForOtherUsers(string scope)
        {
            var cart = CartWithScope(scope);
            var service = CreateService();

            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeFalse();
            service.IsAuthorized(cart, null, OrgId).Should().BeFalse();
            service.IsAuthorized(cart, OwnerId, OrgId).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateScopeAsync_EmptyScope_DoesNothing()
        {
            var cart = new ShoppingCart();

            await CreateService().UpdateScopeAsync(cart, new WishlistScopeContext());

            cart.SharingSettings.Should().BeNull();
        }

        [Theory]
        [InlineData(CartSharingScope.User)]
        [InlineData(CartSharingScope.AnyoneAuthorized)]
        [InlineData(UnknownScope)]
        public async Task UpdateScopeAsync_ScopeThatCannotBeApplied_Throws(string scope)
        {
            var context = new WishlistScopeContext { Scope = scope, CurrentUserId = OwnerId };

            var act = () => CreateService().UpdateScopeAsync(new ShoppingCart(), context);

            (await act.Should().ThrowAsync<InvalidOperationException>())
                .WithMessage($"Unsupported sharing scope '{scope}'.");
        }

        [Fact]
        public async Task UpdateScopeAsync_Organization_WritesSettingAndOwner()
        {
            var cart = new ShoppingCart();
            var context = new WishlistScopeContext
            {
                Scope = CartSharingScope.Organization,
                SharingKey = "key-1",
                CurrentUserId = OwnerId,
                CustomerName = "Owner",
                CurrentOrganizationId = OrgId,
            };

            await CreateService().UpdateScopeAsync(cart, context);

            cart.SharingSettings.Should().ContainSingle();
            cart.SharingSettings[0].Scope.Should().Be(CartSharingScope.Organization);
            cart.SharingSettings[0].Access.Should().Be(CartSharingAccess.Write);
            cart.SharingSettings[0].Id.Should().Be("key-1");
            cart.CustomerId.Should().Be(OwnerId);
            cart.OrganizationId.Should().Be(OrgId);
        }

        [Fact]
        public async Task UpdateScopeAsync_Private_ClearsOrganizationAndSharingKey()
        {
            var cart = new ShoppingCart();
            var context = new WishlistScopeContext
            {
                Scope = CartSharingScope.Private,
                SharingKey = "key-1",
                CurrentUserId = OwnerId,
                CurrentOrganizationId = OrgId,
            };

            await CreateService().UpdateScopeAsync(cart, context);

            cart.SharingSettings[0].Scope.Should().Be(CartSharingScope.Private);
            cart.SharingSettings[0].Id.Should().BeNull();
            cart.OrganizationId.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_DownstreamPolicy_AddsScopeWithoutTouchingBuiltIns()
        {
            var policies = BuiltInPolicies();
            policies.Add(new TestScopePolicy(CustomScope));
            var service = CreateService(policies);

            var cart = new ShoppingCart();
            var context = new WishlistScopeContext { Scope = CustomScope, SharedWithId = OrgId, CurrentUserId = OwnerId };

            await service.UpdateScopeAsync(cart, context);

            service.GetSharingScope(cart).Should().Be(CustomScope);
            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeTrue();
            service.IsAuthorized(cart, OtherUserId, "another-org").Should().BeFalse();

            service.GetSharingScope(CartWithScope(CartSharingScope.Organization)).Should().Be(CartSharingScope.Organization);
        }

        [Theory]
        [InlineData(CartSharingScope.Organization, null, OrgId)]
        [InlineData(CartSharingScope.Private, OwnerId, null)]
        [InlineData(CartSharingScope.AnyoneAnonymous, OwnerId, OrgId)]
        [InlineData(UnknownScope, OwnerId, OrgId)]
        public void ConfigureSearchCriteria_NarrowsPerScope(string scope, string expectedCustomerId, string expectedOrganizationId)
        {
            var criteria = new ShoppingCartSearchCriteria { CustomerId = OwnerId, OrganizationId = OrgId };

            CreateService().ConfigureSearchCriteria(criteria, scope);

            criteria.CustomerId.Should().Be(expectedCustomerId);
            criteria.OrganizationId.Should().Be(expectedOrganizationId);
        }

        [Fact]
        public void EnsureSharingSettings_UnknownScope_Throws()
        {
            var act = () => CreateService().EnsureSharingSettings(new ShoppingCart(), "key-1", UnknownScope, CartSharingAccess.Read, sharedWithId: null);

            act.Should().Throw<InvalidOperationException>().WithMessage($"Unsupported sharing scope '{UnknownScope}'.");
        }

        [Fact]
        public void EnsureSharingSettings_RoutesThroughTheScopePolicy()
        {
            var policies = BuiltInPolicies();
            policies.Add(new MultiRowScopePolicy(CustomScope));
            var cart = CartWithScope(CustomScope);

            CreateService(policies).EnsureSharingSettings(cart, "key-2", CustomScope, CartSharingAccess.Read, OrgId);

            // MultiRowScopePolicy appends instead of demoting-and-reusing, so the override drives the legacy API too.
            cart.SharingSettings.Should().HaveCount(2);
            cart.SharingSettings.Should().OnlyContain(x => x.Scope == CustomScope);
            cart.SharingSettings[1].SharedWithId.Should().Be(OrgId);
        }

        [Fact]
        public async Task ApplyAsync_PolicyOverridingEnsureSetting_KeepsItsOwnWritePolicy()
        {
            var policies = BuiltInPolicies();
            policies.Add(new MultiRowScopePolicy(CustomScope));
            var service = CreateService(policies);

            var cart = new ShoppingCart();
            await service.UpdateScopeAsync(cart, new WishlistScopeContext { Scope = CustomScope, SharedWithId = OrgId, CurrentUserId = OwnerId });
            await service.UpdateScopeAsync(cart, new WishlistScopeContext { Scope = CustomScope, SharedWithId = "org-2", CurrentUserId = OwnerId });

            cart.SharingSettings.Should().HaveCount(2);
            service.GetSharingScope(cart).Should().Be(CustomScope);
        }

        [Fact]
        public async Task ApplyAsync_DefaultWritePolicy_KeepsAnExistingSharingKey()
        {
            var cart = CartWithScope(CartSharingScope.Organization);
            cart.SharingSettings[0].Id = "existing-key";

            var context = new WishlistScopeContext { Scope = CartSharingScope.Private, CurrentUserId = OwnerId };
            await CreateService().UpdateScopeAsync(cart, context);

            cart.SharingSettings.Should().ContainSingle();
            cart.SharingSettings[0].Id.Should().Be("existing-key");
            cart.SharingSettings[0].Scope.Should().Be(CartSharingScope.Private);
        }

        private static ShoppingCart CartWithScope(string scope)
        {
            return new ShoppingCart
            {
                CustomerId = OwnerId,
                SharingSettings = [new CartSharingSetting { Scope = scope }],
            };
        }

        private static List<ICartSharingScopePolicy> BuiltInPolicies()
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

        private static CartSharingService CreateService(IList<ICartSharingScopePolicy> policies = null)
        {
            return new CartSharingService(Mock.Of<ICartAggregateRepository>(), policies ?? BuiltInPolicies());
        }

        private sealed class TestScopePolicy(string scope) : CartSharingScopePolicyBase
        {
            public override string Scope => scope;

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
            {
                return IsOwner(cart, currentUserId) || IsSharedWith(cart, currentOrganizationId);
            }

            public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
            {
                EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read, context.SharedWithId);
                SetOwner(cart, context.CurrentUserId, context.CustomerName, organizationId: null);

                return Task.CompletedTask;
            }

            private bool IsSharedWith(ShoppingCart cart, string organizationId)
            {
                return !string.IsNullOrEmpty(organizationId)
                    && cart.SharingSettings?.Any(x => x.Scope == Scope && x.SharedWithId == organizationId) == true;
            }
        }

        private sealed class MultiRowScopePolicy(string scope) : CartSharingScopePolicyBase
        {
            public override string Scope => scope;

            public override void EnsureSetting(ShoppingCart cart, string sharingKey, string access, string sharedWithId)
            {
                cart.SharingSettings ??= [];
                cart.SharingSettings.Add(new CartSharingSetting
                {
                    Id = sharingKey,
                    ShoppingCartId = cart.Id,
                    Scope = Scope,
                    Access = access,
                    SharedWithId = sharedWithId,
                });
            }

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
            {
                return IsOwner(cart, currentUserId);
            }

            public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
            {
                EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read, context.SharedWithId);

                return Task.CompletedTask;
            }
        }
    }
}
