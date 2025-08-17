using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Data.Authorization;
using static VirtoCommerce.XCart.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateCartFromWishlistCommandBuilder : CommandBuilder<CreateCartFromWishlistCommand, CartAggregate, InputCreateCartFromWishlistCommand, CartType>
{
    protected override string Name => "createCartFromWishlistCommand";

    private readonly IMemberResolver _memberResolver;
    private readonly IShoppingCartService _cartService;

    public CreateCartFromWishlistCommandBuilder(
        IShoppingCartService cartService,
        IMemberResolver memberResolver,
        IMediator mediator,
        IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
        _cartService = cartService;
        _memberResolver = memberResolver;
    }

    protected override CreateCartFromWishlistCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);

        request.UserId = context.GetCurrentUserId();
        request.OrganizationId = context.GetCurrentOrganizationId();

        return request;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, CreateCartFromWishlistCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        var wishlistUserContext = await InitializeWishlistUserContext(context, request);

        await Authorize(context, wishlistUserContext, new CanAccessCartAuthorizationRequirement());

        request.WishlistUserContext = wishlistUserContext;
    }

    protected override Task AfterMediatorSend(IResolveFieldContext<object> context, CreateCartFromWishlistCommand request, CartAggregate response)
    {
        context.SetExpandedObjectGraph(response.Cart);
        context.UserContext["storeId"] = response.Cart?.StoreId;

        return base.AfterMediatorSend(context, request, response);
    }

    private async Task<WishlistUserContext> InitializeWishlistUserContext(IResolveFieldContext context, CreateCartFromWishlistCommand request)
    {
        var wishlistUserContext = new WishlistUserContext
        {
            UserId = request.UserId,
            CurrentUserId = request.UserId,
            CurrentOrganizationId = request.OrganizationId,
            CurrentContact = await _memberResolver.ResolveMemberByIdAsync(request.UserId) as Contact,
        };

        if (!string.IsNullOrEmpty(request.ListId))
        {
            wishlistUserContext.Cart = await _cartService.GetByIdAsync(request.ListId);
        }

        InitializeWishlistUserContextScope(wishlistUserContext);

        return wishlistUserContext;
    }

    private static void InitializeWishlistUserContextScope(WishlistUserContext context)
    {
        var scope = PrivateScope;

        if (context.Cart is not null)
        {
            scope = string.IsNullOrEmpty(context.Cart.OrganizationId)
                ? PrivateScope
                : OrganizationScope;
        }

        context.Scope = scope;
    }
}
