using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetSavedForLaterListQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IUserManagerCore userManagerCore)
    : QueryBuilder<GetSavedForLaterListQuery, CartAggregate, CartType>(mediator, authorizationService)
{
    protected override string Name => "getSavedForLater";

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, GetSavedForLaterListQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        await userManagerCore.CheckCurrentUserState(context, allowAnonymous: false);
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, GetSavedForLaterListQuery request, CartAggregate response)
    {
        await base.AfterMediatorSend(context, request, response);

        context.SetExpandedObjectGraph(response);
    }
}
