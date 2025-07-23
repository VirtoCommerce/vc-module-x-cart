using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCart.Data.Authorization;

namespace VirtoCommerce.XCart.Data.Commands;

public class MoveFromSavedForLaterItemsCommandBuilder(IMediator mediator, IAuthorizationService authorizationService, ICartAggregateRepository cartRepository)
    : CommandBuilder<MoveFromSavedForLaterItemsCommand, CartAggregateWithList, InputSaveForLaterType, CartWithListType>(mediator, authorizationService)
{
    protected override string Name => "moveFromSavedForLater";

    protected override MoveFromSavedForLaterItemsCommand GetRequest(IResolveFieldContext<object> context)
    {
        var result = base.GetRequest(context);

        result.UserId = context.GetCurrentUserId();
        result.OrganizationId = context.GetCurrentOrganizationId();

        return result;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, MoveFromSavedForLaterItemsCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        var cart = await cartRepository.GetCartByIdAsync(request.CartId);
        await Authorize(context, cart.Cart, new CanAccessCartAuthorizationRequirement());
        request.Cart = cart;
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, MoveFromSavedForLaterItemsCommand request, CartAggregateWithList response)
    {
        await base.AfterMediatorSend(context, request, response);

        context.SetExpandedObjectGraph(response.Cart);
        context.SetExpandedObjectGraph(response.List);
    }
}
