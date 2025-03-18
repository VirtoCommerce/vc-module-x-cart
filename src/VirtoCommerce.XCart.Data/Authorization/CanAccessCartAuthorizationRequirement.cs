using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
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
        private readonly IShoppingCartSearchService _shoppingCartSearchService;

        public CanAccessCartAuthorizationHandler(Func<UserManager<ApplicationUser>> userManagerFactory, IShoppingCartSearchService shoppingCartSearchService)
        {
            _userManagerFactory = userManagerFactory;
            _shoppingCartSearchService = shoppingCartSearchService;
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

                    if (!authorized && file.OwnerEntityType.EqualsInvariant(typeof(ConfigurationItem).FullName))
                    {
                        var searchCriteria = AbstractTypeFactory<ShoppingCartSearchCriteria>.TryCreateInstance();
                        searchCriteria.ConfigurationItemIds = [file.OwnerEntityId];
                        var cartSearchResult = await _shoppingCartSearchService.SearchAsync(searchCriteria);

                        if (cartSearchResult.TotalCount > 0)
                        {
                            resource = cartSearchResult.Results.First();
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
