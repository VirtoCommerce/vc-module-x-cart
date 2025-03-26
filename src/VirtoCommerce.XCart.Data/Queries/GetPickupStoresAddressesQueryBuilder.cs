using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPickupStoresAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService) :
    QueryBuilder<GetPickupLocationsQuery, PickupLocationsResponse, PickupStoresAddressesType>(mediator, authorizationService)
{
    protected override string Name { get; } = "getPickupInStoreAddresses";

    protected override Task<PickupLocationsResponse> GetResponseAsync(IResolveFieldContext<object> context, GetPickupLocationsQuery request)
    {
        return base.GetResponseAsync(context, request);
    }
}
