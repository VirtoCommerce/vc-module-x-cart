using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Data.Authorization
{
    public class CanAccessCartAuthorizationRequirement : PermissionAuthorizationRequirement
    {
        public CanAccessCartAuthorizationRequirement() : base("CanAccessCart")
        {
        }
    }

    public class CanAccessCartAuthorizationHandler : PermissionAuthorizationHandlerBase<CanAccessCartAuthorizationRequirement>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

        public CanAccessCartAuthorizationHandler(Func<UserManager<ApplicationUser>> userManagerFactory)
        {
            _userManagerFactory = userManagerFactory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessCartAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            if (!result)
            {
                switch (context.Resource)
                {
                    case string userId when context.User.Identity.IsAuthenticated:
                        result = userId == GetUserId(context);
                        break;
                    case string userId when !context.User.Identity.IsAuthenticated:
                        using (var userManager = _userManagerFactory())
                        {
                            var userById = await userManager.FindByIdAsync(userId);
                            result = userById == null;
                        }
                        break;
                    case ShoppingCart cart when context.User.Identity.IsAuthenticated:
                        result = cart.CustomerId == GetUserId(context);
                        break;
                    case ShoppingCart cart when !context.User.Identity.IsAuthenticated:
                        result = cart.IsAnonymous;
                        break;
                    case IEnumerable<ShoppingCart> carts:
                        var user = GetUserId(context);
                        result = carts.All(x => x.CustomerId == user);
                        break;
                    case SearchCartQuery searchQuery:
                        var currentUserId = GetUserId(context);
                        if (searchQuery.UserId != null)
                        {
                            result = searchQuery.UserId == currentUserId;
                        }
                        else
                        {
                            searchQuery.UserId = currentUserId;
                            result = searchQuery.UserId != null;
                        }
                        break;
                    case WishlistUserContext wishlistUserContext:
                        result = CheckWishlistUserContext(wishlistUserContext);
                        break;
                    case WishlistCommand { WishlistUserContext: not null } wishlistCommand:
                        result = CheckWishlistUserContext(wishlistCommand.WishlistUserContext);
                        break;
                }
            }

            if (result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        private static string GetUserId(AuthorizationHandlerContext context)
        {
            return context.User.GetUserId();
        }

        private static bool CheckWishlistUserContext(WishlistUserContext context)
        {
            var result = true;
            if (context.Cart != null)
            {
                if (context.Cart.OrganizationId != null)
                {
                    result = context.Cart.OrganizationId == context.CurrentOrganizationId;
                }
                else
                {
                    result = context.Cart.CustomerId == context.CurrentUserId;
                }
            }

            if (result && !string.IsNullOrEmpty(context.UserId))
            {
                result = context.UserId == context.CurrentUserId;
            }

            return result;
        }
    }
}
