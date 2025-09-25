using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CartModule.Core.Model;
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
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Authorization;

namespace VirtoCommerce.XCart.Data.Commands;

public class CreateCartFromWishlistCommandBuilder : CommandBuilder<CreateCartFromWishlistCommand, CartAggregate, InputCreateCartFromWishlistType, CartType>
{
    protected override string Name => "createCartFromWishlist";

    private readonly IMemberResolver _memberResolver;
    private readonly IShoppingCartService _cartService;
    private readonly ICartSharingService _cartSharingService;

    public CreateCartFromWishlistCommandBuilder(
        IShoppingCartService cartService,
        IMemberResolver memberResolver,
        IMediator mediator,
        IAuthorizationService authorizationService,
        ICartSharingService cartSharingService)
        : base(mediator, authorizationService)
    {
        _cartService = cartService;
        _memberResolver = memberResolver;
        _cartSharingService = cartSharingService;
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

        var wishlistUserContext = await InitializeWishlistUserContext(request);

        await Authorize(context, wishlistUserContext, new CanAccessCartAuthorizationRequirement());

        request.WishlistUserContext = wishlistUserContext;
    }

    protected override Task AfterMediatorSend(IResolveFieldContext<object> context, CreateCartFromWishlistCommand request, CartAggregate response)
    {
        context.SetExpandedObjectGraph(response.Cart);
        context.UserContext["storeId"] = response.Cart?.StoreId;

        return base.AfterMediatorSend(context, request, response);
    }

    private async Task<WishlistUserContext> InitializeWishlistUserContext(CreateCartFromWishlistCommand request)
    {
        var wishlistUserContext = new WishlistUserContext
        {
            UserId = request.UserId,
            CurrentUserId = request.UserId,
            CurrentOrganizationId = request.OrganizationId,
            CurrentContact = await _memberResolver.ResolveMemberByIdAsync(request.UserId) as Contact,
            RequestedAccess = CartSharingAccess.Write,
        };

        if (!string.IsNullOrEmpty(request.ListId))
        {
            wishlistUserContext.Cart = await _cartService.GetByIdAsync(request.ListId);
        }

        InitializeWishlistUserContextScope(wishlistUserContext);

        return wishlistUserContext;
    }

    private void InitializeWishlistUserContextScope(WishlistUserContext context)
    {
        context.Scope = _cartSharingService.GetSharingScope(context.Cart);
    }
}
