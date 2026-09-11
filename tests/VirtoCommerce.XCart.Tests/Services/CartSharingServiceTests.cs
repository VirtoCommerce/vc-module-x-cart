using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Services;
using VirtoCommerce.XCart.Data.Services.SharingScopes;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using CartModuleConstants = VirtoCommerce.CartModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Tests.Services
{
    public class CartSharingServiceTests
    {
        private const string OwnerId = "owner-user";
        private const string OtherUserId = "other-user";
        private const string OrgId = "org-1";
        private const string OtherOrgId = "org-2";
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

        [Fact]
        public void GetSharingScope_LegacyDemotedPrivateRowFirst_ReturnsTheActiveScope()
        {
            // The old writer demoted extra rows to Private and left them in place, so the active row need not be first.
            var cart = LegacyMultiRowCart();
            var service = CreateService();

            service.GetSharingScope(cart).Should().Be(CartSharingScope.Organization);
            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeTrue();
            cart.GetEffectiveSharingSetting().Id.Should().Be("active");
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
            // IsAuthorized must not follow GetSharingScope's inference, or any org member reaches every private cart.
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
        public void IsAuthorized_IdsDifferingOnlyByCase_StillMatch()
        {
            // In-memory id comparisons are case-insensitive: the stored casing must not lock a caller out.
            var ownerCart = CartWithScope(CartSharingScope.Private);
            var orgCart = CartWithScope(CartSharingScope.Organization);
            orgCart.OrganizationId = OrgId;

            var service = CreateService();

            service.IsAuthorized(ownerCart, OwnerId.ToUpperInvariant(), null).Should().BeTrue();
            service.IsAuthorized(orgCart, OtherUserId, OrgId.ToUpperInvariant()).Should().BeTrue();
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
            var service = CreateService(WithCustomScope());

            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId]));

            service.GetSharingScope(cart).Should().Be(CustomScope);
            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeTrue();
            service.IsAuthorized(cart, OtherUserId, "another-org").Should().BeFalse();

            service.GetSharingScope(CartWithScope(CartSharingScope.Organization)).Should().Be(CartSharingScope.Organization);
        }

        [Fact]
        public async Task UpdateScopeAsync_TargetedScope_AddsRemovesAndDeduplicatesTargets()
        {
            var service = CreateService(WithCustomScope());
            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId, OtherOrgId], sharingKey: "key-1"));

            var setting = cart.SharingSettings.Should().ContainSingle().Subject;
            setting.Id.Should().Be("key-1");
            setting.Targets.Select(x => x.SharedWithId).Should().BeEquivalentTo(OrgId, OtherOrgId);

            // Re-adding an id in another case is a no-op, a removal drops exactly that id, the key stays.
            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId.ToUpperInvariant(), "org-3"], removeSharedWithIds: [OtherOrgId]));

            cart.SharingSettings.Should().ContainSingle();
            setting.Id.Should().Be("key-1");
            setting.Targets.Select(x => x.SharedWithId).Should().BeEquivalentTo(OrgId, "org-3");
            service.IsAuthorized(cart, OtherUserId, "org-3").Should().BeTrue();
            service.IsAuthorized(cart, OtherUserId, OtherOrgId).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateScopeAsync_LegacySharedWithId_SwitchingRecipientReplacesIt()
        {
            // The released storefront's picker holds ONE recipient and sends it on every save. Sharing with A and
            // then switching to B has to leave the list shared with B alone - accumulating would leave A's access in
            // place while the dialog, which reads back the first target, still displayed A.
            var service = CreateService(WithCustomScope());
            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(legacySharedWithId: OrgId, sharingKey: "key-1"));
            cart.SharingSettings[0].Targets.Select(x => x.SharedWithId).Should().Equal(OrgId);

            // A save that does not touch sharing (a rename) re-sends the same id: nothing moves, in any casing.
            await service.UpdateScopeAsync(cart, CustomContext(legacySharedWithId: OrgId.ToUpperInvariant()));
            cart.SharingSettings[0].Targets.Select(x => x.SharedWithId).Should().Equal(OrgId);

            await service.UpdateScopeAsync(cart, CustomContext(legacySharedWithId: OtherOrgId));

            cart.SharingSettings[0].Targets.Select(x => x.SharedWithId).Should().Equal(OtherOrgId);
            cart.SharingSettings[0].Id.Should().Be("key-1");
            service.IsAuthorized(cart, OtherUserId, OrgId).Should().BeFalse();
            service.IsAuthorized(cart, OtherUserId, OtherOrgId).Should().BeTrue();
        }

        [Fact]
        public async Task UpdateScopeAsync_LegacySharedWithId_MultiTargetList_KeepsTheSetOrRefuses()
        {
            var service = CreateService(WithCustomScope());
            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId, OtherOrgId]));

            // A single-valued client re-sending the id it reads back (the first target) must not collapse the set.
            await service.UpdateScopeAsync(cart, CustomContext(legacySharedWithId: OrgId));
            cart.SharingSettings[0].Targets.Select(x => x.SharedWithId).Should().BeEquivalentTo(OrgId, OtherOrgId);

            // It cannot express "replace these two with one", so the write is refused rather than revoking silently.
            var act = () => service.UpdateScopeAsync(cart, CustomContext(legacySharedWithId: "org-3"));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*addSharedWithIds*");
            cart.SharingSettings[0].Targets.Select(x => x.SharedWithId).Should().BeEquivalentTo(OrgId, OtherOrgId);
        }

        [Fact]
        public async Task UpdateScopeAsync_LegacySharedWithIdWithDeltas_IsOneMoreIdToAdd()
        {
            // A client that speaks deltas gets delta semantics: it can see the set it is changing.
            var service = CreateService(WithCustomScope());
            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId], legacySharedWithId: OtherOrgId));

            cart.SharingSettings[0].Targets.Select(x => x.SharedWithId).Should().BeEquivalentTo(OrgId, OtherOrgId);
        }

        [Fact]
        public async Task UpdateScopeAsync_Message_NullKeepsEmptyClearsValueIsTrimmed()
        {
            var service = CreateService(WithCustomScope());
            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId], message: "  Hello  "));
            cart.SharingSettings[0].Message.Should().Be("Hello");

            await service.UpdateScopeAsync(cart, CustomContext(message: null));
            cart.SharingSettings[0].Message.Should().Be("Hello");

            await service.UpdateScopeAsync(cart, CustomContext(message: ""));
            cart.SharingSettings[0].Message.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_MessageTooLong_Throws()
        {
            var cart = new ShoppingCart();
            var context = CustomContext(addSharedWithIds: [OrgId], message: new string('x', CartModuleConstants.Sharing.MessageMaxLength + 1));

            var act = () => CreateService(WithCustomScope()).UpdateScopeAsync(cart, context);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{CartModuleConstants.Sharing.MessageMaxLength}*");
            cart.SharingSettings.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_MoreTargetsThanOneWriteAllows_Throws()
        {
            // One write persists one row per id, so the add list is bounded the way the communication mutation's
            // organization list is - a rep serving the stated ceiling of ~1000 customers still fits in one call.
            var cart = new ShoppingCart();
            var ids = Enumerable.Range(0, CartModuleConstants.Sharing.MaxTargets + 1).Select(x => $"org-{x}").ToList();

            var act = () => CreateService(WithCustomScope()).UpdateScopeAsync(cart, CustomContext(addSharedWithIds: ids));

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{CartModuleConstants.Sharing.MaxTargets}*");
            cart.SharingSettings.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_IdBothAddedAndRemoved_Throws()
        {
            var cart = new ShoppingCart();
            var context = CustomContext(addSharedWithIds: [OrgId], removeSharedWithIds: [OrgId.ToUpperInvariant()]);

            var act = () => CreateService(WithCustomScope()).UpdateScopeAsync(cart, context);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{OrgId}*");
            cart.SharingSettings.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_ScopeChange_ClearsTargetsAndMessageButKeepsTheKey()
        {
            var service = CreateService(WithCustomScope());
            var cart = new ShoppingCart();

            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId], message: "Hello", sharingKey: "key-1"));
            await service.UpdateScopeAsync(cart, new WishlistScopeContext
            {
                Scope = CartSharingScope.Organization,
                SharingKey = "ignored-key",
                CurrentUserId = OwnerId,
                CurrentOrganizationId = OrgId,
            });

            var setting = cart.SharingSettings.Should().ContainSingle().Subject;
            setting.Id.Should().Be("key-1");
            setting.Scope.Should().Be(CartSharingScope.Organization);
            setting.Targets.Should().BeEmpty();
            setting.Message.Should().BeNull();

            // Back to the targeted scope: nothing resurrects and the key is still the same.
            await service.UpdateScopeAsync(cart, CustomContext());

            setting.Id.Should().Be("key-1");
            setting.Scope.Should().Be(CustomScope);
            setting.Targets.Should().BeEmpty();
            setting.Message.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_BuiltInScope_IgnoresStrayTargetsAndMessage()
        {
            var cart = new ShoppingCart();
            var context = new WishlistScopeContext
            {
                Scope = CartSharingScope.AnyoneAnonymous,
                SharingKey = "key-1",
                AddSharedWithIds = [OrgId],
                Message = "Hello",
                CurrentUserId = OwnerId,
            };

            await CreateService().UpdateScopeAsync(cart, context);

            var setting = cart.SharingSettings.Should().ContainSingle().Subject;
            setting.Targets.Should().BeNullOrEmpty();
            setting.Message.Should().BeNull();
        }

        [Fact]
        public async Task UpdateScopeAsync_LegacyMultiRowCart_KeepsTheActiveRowAndDropsTheRest()
        {
            var cart = LegacyMultiRowCart();
            var context = new WishlistScopeContext { Scope = CartSharingScope.AnyoneAnonymous, SharingKey = "new-key", CurrentUserId = OwnerId };

            await CreateService().UpdateScopeAsync(cart, context);

            var setting = cart.SharingSettings.Should().ContainSingle().Subject;
            setting.Id.Should().Be("active");
            setting.Scope.Should().Be(CartSharingScope.AnyoneAnonymous);
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
        public async Task ResolveTargetsAsync_WithoutAResolvingPolicy_ReturnsIdsOnly()
        {
            var service = CreateService(WithCustomScope());

            var targets = await service.ResolveTargetsAsync(CustomScope, [OrgId, OtherOrgId]);
            targets.Select(x => x.Id).Should().Equal(OrgId, OtherOrgId);
            targets.Should().OnlyContain(x => x.Name == null && x.Subtitle == null && x.ImageUrl == null);

            // A scope without a registered policy (module uninstalled) still shows who the list was shared with.
            (await service.ResolveTargetsAsync(UnknownScope, [OrgId, OtherOrgId])).Select(x => x.Id).Should().Equal(OrgId, OtherOrgId);

            (await service.ResolveTargetsAsync(CustomScope, null)).Should().BeEmpty();
            (await service.ResolveTargetsAsync(CustomScope, [])).Should().BeEmpty();
            (await service.ResolveTargetsAsync(null, [OrgId])).Select(x => x.Id).Should().Equal(OrgId);
        }

        [Fact]
        public async Task ResolveTargetsAsync_PolicyOverride_SuppliesDisplayData()
        {
            var policies = BuiltInPolicies();
            policies.Add(new NamedTargetsScopePolicy(CustomScope));

            // Ids of one scope arrive together: what the batch loader hands the policy for a whole page of lists.
            var targets = await CreateService(policies).ResolveTargetsAsync(CustomScope, [OrgId, OtherOrgId]);

            targets.Select(x => x.Name).Should().Equal($"Name of {OrgId}", $"Name of {OtherOrgId}");
        }

#pragma warning disable VC0015 // EnsureSharingSettings is the legacy writer kept for external callers; its behavior is pinned here.
        [Fact]
        public void EnsureSharingSettings_UnknownScope_Throws()
        {
            var act = () => CreateService().EnsureSharingSettings(new ShoppingCart(), "key-1", UnknownScope, CartSharingAccess.Read, sharedWithId: null);

            act.Should().Throw<InvalidOperationException>().WithMessage($"Unsupported sharing scope '{UnknownScope}'.");
        }

        [Fact]
        public void EnsureSharingSettings_RoutesThroughTheScopePolicyAndAddsTheTarget()
        {
            var policies = BuiltInPolicies();
            policies.Add(new MultiRowScopePolicy(CustomScope));
            var cart = CartWithScope(CustomScope);

            CreateService(policies).EnsureSharingSettings(cart, "key-2", CustomScope, CartSharingAccess.Read, OrgId);

            // MultiRowScopePolicy appends instead of reusing, so the override drives the legacy API too.
            cart.SharingSettings.Should().HaveCount(2);
            cart.SharingSettings.Should().OnlyContain(x => x.Scope == CustomScope);
            cart.SharingSettings[1].Targets.Should().ContainSingle(x => x.SharedWithId == OrgId);
        }
#pragma warning restore VC0015

        [Fact]
        public async Task ApplyAsync_PolicyOverridingEnsureSetting_KeepsItsOwnWritePolicy()
        {
            var policies = BuiltInPolicies();
            policies.Add(new MultiRowScopePolicy(CustomScope));
            var service = CreateService(policies);

            var cart = new ShoppingCart();
            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OrgId]));
            await service.UpdateScopeAsync(cart, CustomContext(addSharedWithIds: [OtherOrgId]));

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

        private static ShoppingCart LegacyMultiRowCart()
        {
            return new ShoppingCart
            {
                CustomerId = OwnerId,
                OrganizationId = OrgId,
                SharingSettings =
                [
                    new CartSharingSetting { Id = "demoted", Scope = CartSharingScope.Private },
                    new CartSharingSetting { Id = "active", Scope = CartSharingScope.Organization },
                ],
            };
        }

        private static WishlistScopeContext CustomContext(
            IList<string> addSharedWithIds = null,
            IList<string> removeSharedWithIds = null,
            string message = null,
            string sharingKey = null,
            string legacySharedWithId = null)
        {
            return new WishlistScopeContext
            {
                Scope = CustomScope,
                SharingKey = sharingKey,
                AddSharedWithIds = addSharedWithIds,
                RemoveSharedWithIds = removeSharedWithIds,
                LegacySharedWithId = legacySharedWithId,
                Message = message,
                CurrentUserId = OwnerId,
            };
        }

        private static List<ICartSharingScopePolicy> BuiltInPolicies() => CartSharingScopeFixtures.BuiltInPolicies();

        private static List<ICartSharingScopePolicy> WithCustomScope()
        {
            var policies = BuiltInPolicies();
            policies.Add(new TestScopePolicy(CustomScope));
            return policies;
        }

        private static CartSharingService CreateService(IList<ICartSharingScopePolicy> policies = null) =>
            CartSharingScopeFixtures.SharingService(policies);

        // A targeted scope on the default write policy: one setting, N targets, one message.
        private sealed class TestScopePolicy(string scope) : CartSharingScopePolicyBase
        {
            public override string Scope => scope;

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
            {
                return IsOwner(cart, currentUserId) || IsSharedWith(cart, currentOrganizationId);
            }

            public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
            {
                var setting = EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read);

                setting.ApplyTargets(context.AddSharedWithIds, context.RemoveSharedWithIds);
                setting.ApplyMessage(context.Message);

                SetOwner(cart, context.CurrentUserId, context.CustomerName, organizationId: null);

                return Task.CompletedTask;
            }

            private bool IsSharedWith(ShoppingCart cart, string organizationId)
            {
                var setting = cart.GetEffectiveSharingSetting();

                return !string.IsNullOrEmpty(organizationId)
                    && setting?.Scope.EqualsIgnoreCase(Scope) == true
                    && setting.Targets?.Any(x => x.SharedWithId.EqualsIgnoreCase(organizationId)) == true;
            }
        }

        // A scope that resolves display data for its targets, the way a module owning the id space would.
        private sealed class NamedTargetsScopePolicy(string scope) : CartSharingScopePolicyBase
        {
            public override string Scope => scope;

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
            {
                return IsOwner(cart, currentUserId);
            }

            public override async Task<IList<WishlistSharingTarget>> ResolveTargetsAsync(IList<string> sharedWithIds)
            {
                var targets = await base.ResolveTargetsAsync(sharedWithIds);

                foreach (var target in targets)
                {
                    target.Name = $"Name of {target.Id}";
                }

                return targets;
            }
        }

        // A scope that overrides the write policy to keep several rows instead of one.
        private sealed class MultiRowScopePolicy(string scope) : CartSharingScopePolicyBase
        {
            public override string Scope => scope;

            public override CartSharingSetting EnsureSetting(ShoppingCart cart, string sharingKey, string access)
            {
                cart.SharingSettings ??= [];

                var setting = new CartSharingSetting
                {
                    Id = sharingKey,
                    ShoppingCartId = cart.Id,
                    Scope = Scope,
                    Access = access,
                };

                cart.SharingSettings.Add(setting);

                return setting;
            }

            public override bool IsAuthorized(ShoppingCart cart, string currentUserId, string currentOrganizationId)
            {
                return IsOwner(cart, currentUserId);
            }

            public override Task ApplyAsync(ShoppingCart cart, WishlistScopeContext context)
            {
                EnsureSetting(cart, context.SharingKey, CartSharingAccess.Read).ApplyTargets(context.AddSharedWithIds, context.RemoveSharedWithIds);

                return Task.CompletedTask;
            }
        }
    }
}
