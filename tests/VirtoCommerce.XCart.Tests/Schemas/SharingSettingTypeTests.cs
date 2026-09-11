using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.DataLoader;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Schemas
{
    // Who a list is shared with is the owner's information: targets and the deprecated sharedWithId resolve for the
    // owner only, so a targeted reader never learns the other recipients. The recipients themselves are resolved
    // through a request-scoped batch loader, so a page of lists costs one call per scope rather than one per list.
    public class SharingSettingTypeTests
    {
        private const string OwnerId = "owner-user";

        // GetUserId reads the claim types the platform registers at startup; nothing else in this suite sets them.
        static SharingSettingTypeTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];
        }

        [Theory]
        [InlineData(OwnerId)]
        [InlineData("OWNER-USER")]
        public async Task Owner_SeesTargetsAndSharedWithId(string userId)
        {
            var harness = new Harness();
            var context = Context(Setting(), userId);

            var targets = await harness.ResolveTargetsAsync(context);
            var sharedWithId = await harness.ResolveAsync("SharedWithId", context);

            targets.Select(x => x.Id).Should().Equal("org-1", "org-2");
            sharedWithId.Should().Be("org-1");
        }

        [Theory]
        [InlineData("other-user")]
        [InlineData(null)]
        public async Task NonOwnerOrAnonymous_GetsNoRecipients(string userId)
        {
            var harness = new Harness();
            var context = Context(Setting(), userId);

            (await harness.ResolveTargetsAsync(context)).Should().BeEmpty();
            (await harness.ResolveAsync("SharedWithId", context)).Should().BeNull();
        }

        [Fact]
        public async Task Targets_OfSeveralLists_ResolveInOneCallPerScope()
        {
            // A page of wishlists must not resolve its recipients one list at a time: at up to a thousand
            // recipients per list that fan-out is the whole cost. Ids are deduplicated across the request too.
            var harness = new Harness();

            // Both fields resolve before either result is awaited, exactly as the executor resolves a page.
            var first = harness.PendingTargets(Context(Setting("org-1", "org-2"), OwnerId));
            var second = harness.PendingTargets(Context(Setting("org-2", "org-3"), OwnerId));

            (await first.GetResultAsync()).Select(x => x.Id).Should().Equal("org-1", "org-2");
            (await second.GetResultAsync()).Select(x => x.Id).Should().Equal("org-2", "org-3");

            harness.Calls.Should().ContainSingle();
            harness.Calls[0].Scope.Should().Be("Customer");
            harness.Calls[0].Ids.Should().BeEquivalentTo(["org-1", "org-2", "org-3"]);
        }

        private static CartSharingSetting Setting(params string[] sharedWithIds)
        {
            var ids = sharedWithIds.Length > 0 ? sharedWithIds : ["org-1", "org-2"];

            return new CartSharingSetting
            {
                Id = "key-1",
                CreatedBy = OwnerId,
                Scope = "Customer",
                Targets = [.. ids.Select(x => new CartSharingSettingTarget { SharedWithId = x })],
            };
        }

        private static IResolveFieldContext<CartSharingSetting> Context(CartSharingSetting setting, string userId)
        {
            var identity = new ClaimsIdentity(userId == null ? [] : [new Claim(ClaimTypes.NameIdentifier, userId)]);
            var mock = new Mock<IResolveFieldContext<CartSharingSetting>>();

            // GraphQL.NET's resolver adapter reads the non-generic IResolveFieldContext.Source, so both views need the setting.
            mock.SetupGet(x => x.Source).Returns(setting);
            mock.As<IResolveFieldContext>().SetupGet(x => x.Source).Returns(setting);
            mock.SetupGet(x => x.User).Returns(new ClaimsPrincipal(identity));

            return mock.Object;
        }

        // One DataLoaderContext stands in for one GraphQL request: everything resolved through it batches together.
        private sealed class Harness
        {
            private readonly DataLoaderContextAccessor _accessor = new() { Context = new DataLoaderContext() };
            private readonly SharingSettingType _type;

            public Harness()
            {
                var service = new Mock<ICartSharingService>();

                service
                    .Setup(x => x.ResolveTargetsAsync(It.IsAny<string>(), It.IsAny<IList<string>>()))
                    .Callback<string, IList<string>>((scope, ids) => Calls.Add((scope, ids)))
                    .ReturnsAsync((string _, IList<string> ids) =>
                        (IList<WishlistSharingTarget>)[.. ids.Select(id => new WishlistSharingTarget { Id = id })]);

                _type = new SharingSettingType(service.Object, _accessor);
            }

            public List<(string Scope, IList<string> Ids)> Calls { get; } = [];

            public async Task<object> ResolveAsync(string field, IResolveFieldContext<CartSharingSetting> context)
            {
                return await _type.Fields.Find(field).Resolver.ResolveAsync(context);
            }

            // The pending result: awaiting it is what dispatches the loader, so several must be collected first.
            public IDataLoaderResult<WishlistSharingTarget[]> PendingTargets(IResolveFieldContext<CartSharingSetting> context)
            {
                return (IDataLoaderResult<WishlistSharingTarget[]>)ResolveAsync("Targets", context).GetAwaiter().GetResult();
            }

            public async Task<IList<WishlistSharingTarget>> ResolveTargetsAsync(IResolveFieldContext<CartSharingSetting> context)
            {
                return await PendingTargets(context).GetResultAsync();
            }
        }
    }
}
