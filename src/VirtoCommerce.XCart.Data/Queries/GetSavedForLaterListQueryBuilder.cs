using System;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Data.Authorization;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetSavedForLaterListQueryBuilder(IAuthorizationService authorizationService)
    : QueryBuilder<GetSavedForLaterListQuery, CartAggregate, CartType>(authorizationService)
{
    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public GetSavedForLaterListQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : this(authorizationService)
    {
    }

    protected override string Name => "getSavedForLater";

    protected override GetSavedForLaterListQuery GetRequest(IResolveFieldContext<object> context)
    {
        var result = base.GetRequest(context);

        result.UserId = context.GetCurrentUserId();
        result.OrganizationId = context.GetCurrentOrganizationId();

        return result;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, GetSavedForLaterListQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, GetSavedForLaterListQuery request, CartAggregate response)
    {
        await base.AfterMediatorSend(context, request, response);

        if (response != null)
        {
            await Authorize(context, response.Cart, new CanAccessCartAuthorizationRequirement());

            context.SetExpandedObjectGraph(response);
        }
    }
}
