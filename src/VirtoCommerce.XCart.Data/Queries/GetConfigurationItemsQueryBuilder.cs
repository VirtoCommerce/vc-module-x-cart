using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Data.Authorization;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetConfigurationItemsQueryBuilder : QueryBuilder<GetConfigurationItemsQuery, ConfigurationItemsResponse, ConfigurationItemsResponseType>
{
    private readonly IUserManagerCore _userManagerCore;

    public GetConfigurationItemsQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IUserManagerCore userManagerCore)
        : base(mediator, authorizationService)
    {
        _userManagerCore = userManagerCore;
    }

    protected override string Name => "configurationItems";

    protected override async Task<ConfigurationItemsResponse> GetResponseAsync(IResolveFieldContext<object> context, GetConfigurationItemsQuery request)
    {
        var response = await base.GetResponseAsync(context, request);

        var cartAuthorizationRequirement = new CanAccessCartAuthorizationRequirement();

        if (response is not null && response.CartAggregate is not null)
        {
            await Authorize(context, response.CartAggregate.Cart, cartAuthorizationRequirement);
        }

        return response;
    }

    protected override Task AfterMediatorSend(IResolveFieldContext<object> context, GetConfigurationItemsQuery request, ConfigurationItemsResponse response)
    {
        if (response.CartAggregate is not null)
        {
            context.SetExpandedObjectGraph(response.CartAggregate);
        }

        return Task.CompletedTask;
    }

    protected override async Task Authorize(IResolveFieldContext context, object resource, IAuthorizationRequirement requirement)
    {
        await _userManagerCore.CheckCurrentUserState(context, allowAnonymous: true);

        await base.Authorize(context, resource, requirement);
    }
}
