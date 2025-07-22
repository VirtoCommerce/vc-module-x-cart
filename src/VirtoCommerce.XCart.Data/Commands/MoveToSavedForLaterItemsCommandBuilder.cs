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
using VirtoCommerce.XCart.Data.Authorization;

namespace VirtoCommerce.XCart.Data.Commands;

public class MoveToSavedForLaterItemsCommandBuilder(IMediator mediator, IAuthorizationService authorizationService)
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

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, MoveToSavedForLaterItemsCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, MoveToSavedForLaterItemsCommand request, CartAggregateWithList response)
    {
        await base.AfterMediatorSend(context, request, response);

        await Authorize(context, response.Cart.Cart, new CanAccessCartAuthorizationRequirement());
        await Authorize(context, response.List.Cart, new CanAccessCartAuthorizationRequirement());

        context.SetExpandedObjectGraph(response.Cart);
        context.SetExpandedObjectGraph(response.List);
    }
}
