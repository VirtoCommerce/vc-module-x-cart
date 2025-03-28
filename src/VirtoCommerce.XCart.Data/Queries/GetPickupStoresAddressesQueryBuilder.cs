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
    QueryBuilder<GetPickupStoresAddressesQuery, PickupStoresAddressesResponse, PickupStoresAddressesType>(mediator, authorizationService)
{
    protected override string Name { get; } = "getPickupInStoreAddresses";

    protected override Task<PickupStoresAddressesResponse> GetResponseAsync(IResolveFieldContext<object> context, GetPickupStoresAddressesQuery request)
    {
        return base.GetResponseAsync(context, request);
    }
}
