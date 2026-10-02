using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Authorization
{
    public class CanAccessCartAuthorizationHandlerTests
    {
        private const string CallerId = "caller-user";
        private const string OwnerId = "owner-user";
        private const string OrgId = "org-1";

        [Fact]
        public async Task CloneOfASharedList_UserIdOfAnotherUser_IsRefused()
        {
            // cloneWishlist names the owner of the clone in userId. The check that it is the caller used to sit below
            // the shared-cart branch, which returns first - so a clone of a shared list, the only kind worth cloning,
            // could be planted in someone else's account (VCST-6125).
            var authorized = await AuthorizeAsync(new CloneWishlistCommand { UserId = OwnerId });

            authorized.Should().BeFalse();
        }

        [Fact]
        public async Task CloneOfASharedList_OwnUserId_IsAllowed()
        {
            // The control: the same caller, on the same list, naming themselves - and ids compare ignoring case.
            var authorized = await AuthorizeAsync(new CloneWishlistCommand { UserId = CallerId.ToUpperInvariant() });

            authorized.Should().BeTrue();
        }

        [Fact]
        public async Task WriteToASharedList_NoUserId_IsStillAllowed()
        {
            // Mutations that do not name an owner keep working off the sharing scope alone.
            var authorized = await AuthorizeAsync(new ChangeWishlistCommand());

            authorized.Should().BeTrue();
        }

        // An organization-scoped list owned by someone else: every member of the organization may write it.
        private static async Task<bool> AuthorizeAsync(WishlistCommand command)
        {
            command.WishlistUserContext = new WishlistUserContext
            {
                CurrentUserId = CallerId,
                CurrentOrganizationId = OrgId,
                UserId = command.UserId,
                RequestedAccess = CartSharingAccess.Write,
                Scope = CartSharingScope.Organization,
                Cart = new ShoppingCart
                {
                    CustomerId = OwnerId,
                    OrganizationId = OrgId,
                    SharingSettings = [new CartSharingSetting { Scope = CartSharingScope.Organization }],
                },
            };

            var handler = new CanAccessCartAuthorizationHandler(
                () => null,
                Mock.Of<IShoppingCartService>(),
                CartSharingScopeFixtures.SharingService());

            var requirement = new CanAccessCartAuthorizationRequirement();
            var user = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test"));
            var context = new AuthorizationHandlerContext([requirement], user, command);

            await handler.HandleAsync(context);

            return context.HasSucceeded;
        }
    }
}
