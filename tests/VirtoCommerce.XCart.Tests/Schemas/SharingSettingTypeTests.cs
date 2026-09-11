using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Schemas
{
    // Who a list is shared with is the owner's information: targets and the deprecated sharedWithId resolve for the
    // owner only, so a targeted reader never learns the other recipients.
    public class SharingSettingTypeTests
    {
        private const string OwnerId = "owner-user";

        private static readonly SharingSettingType Type = new(CartSharingScopeFixtures.SharingService());

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
            var context = Context(Setting(), userId);

            var targets = (IList<WishlistSharingTarget>)await Resolve("Targets", context);
            var sharedWithId = await Resolve("SharedWithId", context);

            targets.Select(x => x.Id).Should().Equal("org-1", "org-2");
            sharedWithId.Should().Be("org-1");
        }

        [Theory]
        [InlineData("other-user")]
        [InlineData(null)]
        public async Task NonOwnerOrAnonymous_GetsNoRecipients(string userId)
        {
            var context = Context(Setting(), userId);

            ((IList<WishlistSharingTarget>)await Resolve("Targets", context)).Should().BeEmpty();
            (await Resolve("SharedWithId", context)).Should().BeNull();
        }

        // Scope without a registered policy: the service falls back to ids-only targets, so the test stays deterministic.
        private static CartSharingSetting Setting()
        {
            return new CartSharingSetting
            {
                Id = "key-1",
                CreatedBy = OwnerId,
                Scope = "Customer",
                Targets = [new CartSharingSettingTarget { SharedWithId = "org-1" }, new CartSharingSettingTarget { SharedWithId = "org-2" }],
            };
        }

        private static async Task<object> Resolve(string field, IResolveFieldContext<CartSharingSetting> context)
        {
            return await Type.Fields.Find(field).Resolver.ResolveAsync(context);
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
    }
}
