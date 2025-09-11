using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.FileExperienceApi.Core.Extensions;
using VirtoCommerce.FileExperienceApi.Core.Models;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using CartType = VirtoCommerce.CartModule.Core.ModuleConstants.CartType;

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
        private readonly IShoppingCartService _shoppingCartService;

        public CanAccessCartAuthorizationHandler(Func<UserManager<ApplicationUser>> userManagerFactory, IShoppingCartService shoppingCartService)
        {
            _userManagerFactory = userManagerFactory;
            _shoppingCartService = shoppingCartService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessCartAuthorizationRequirement requirement)
        {
            var authorized = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            if (!authorized)
            {
                var resource = context.Resource;

                if (resource is File file)
                {
                    authorized = file.OwnerIsEmpty();

                    if (!authorized && file.OwnerTypeIs<ShoppingCart>())
                    {
                        var cart = await _shoppingCartService.GetByIdAsync(file.OwnerEntityId);

                        if (cart != null)
                        {
                            resource = cart;
                        }
                    }
                }

                switch (resource)
                {
                    case string userId when context.User.Identity.IsAuthenticated:
                        authorized = userId == GetUserId(context);
                        break;
                    case string userId when !context.User.Identity.IsAuthenticated:
                        using (var userManager = _userManagerFactory())
                        {
                            var userById = await userManager.FindByIdAsync(userId);
                            authorized = userById == null;
                        }
                        break;
                    case ShoppingCart cart when context.User.Identity.IsAuthenticated:
                        authorized = cart.CustomerId == GetUserId(context);
                        break;
                    case ShoppingCart cart when !context.User.Identity.IsAuthenticated:
                        authorized = cart.IsAnonymous;
                        break;
                    case IEnumerable<ShoppingCart> carts:
                        var user = GetUserId(context);
                        authorized = carts.All(x => x.CustomerId == user);
                        break;
                    case SearchCartQuery searchQuery:
                        var currentUserId = GetUserId(context);
                        if (searchQuery.UserId != null)
                        {
                            authorized = searchQuery.UserId == currentUserId;
                        }
                        else
                        {
                            searchQuery.UserId = currentUserId;
                            authorized = searchQuery.UserId != null;
                        }
                        break;
                    case WishlistUserContext wishlistUserContext:
                        authorized = CheckWishlistUserContext(wishlistUserContext);
                        break;
                    case WishlistCommand { WishlistUserContext: not null } wishlistCommand:
                        authorized = CheckWishlistUserContext(wishlistCommand.WishlistUserContext);
                        break;
                }
            }

            if (authorized)
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
                if (context.Cart.Type == CartType.SavedForLater)
                {
                    result = context.Cart.CustomerId == context.CurrentUserId || (context.Cart.OrganizationId != null && context.Cart.OrganizationId == context.CurrentOrganizationId);
                }
                else
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
            }

            if (result && !string.IsNullOrEmpty(context.UserId))
            {
                result = context.UserId == context.CurrentUserId;
            }

            return result;
        }
    }
}
