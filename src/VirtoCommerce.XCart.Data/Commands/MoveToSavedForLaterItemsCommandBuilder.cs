using System;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XCart.Data.Schemas;

namespace VirtoCommerce.XCart.Data.Commands;

public class MoveToSavedForLaterItemsCommandBuilder(IMediator mediator, IAuthorizationService authorizationService, ICartAggregateRepository cartRepository, IDistributedLockService distributedLockService)
    : CommandBuilder<MoveToSavedForLaterItemsCommand, CartAggregateWithList, InputSaveForLaterType, CartWithListType>(mediator, authorizationService)
{
    protected override string Name => "moveToSavedForLater";

    protected override MoveToSavedForLaterItemsCommand GetRequest(IResolveFieldContext<object> context)
    {
        var result = base.GetRequest(context);

        result.UserId = context.GetCurrentUserId();
        result.OrganizationId = context.GetCurrentOrganizationId();

        return result;
    }

    protected override Task<CartAggregateWithList> GetResponseAsync(IResolveFieldContext<object> context, MoveToSavedForLaterItemsCommand request)
    {
        return distributedLockService.ExecuteAsync($"{PurchaseSchema.CartPrefix}:{request.UserId}", () => base.GetResponseAsync(context, request));
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, MoveToSavedForLaterItemsCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        var cart = await cartRepository.GetCartByIdAsync(request.CartId);

        if (cart == null)
        {
            throw new OperationCanceledException("Cart not found");
        }

        await Authorize(context, cart.Cart, new CanAccessCartAuthorizationRequirement());

        request.Cart = cart;
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, MoveToSavedForLaterItemsCommand request, CartAggregateWithList response)
    {
        await base.AfterMediatorSend(context, request, response);

        context.SetExpandedObjectGraph(response.Cart);
        context.SetExpandedObjectGraph(response.List);
    }
}
