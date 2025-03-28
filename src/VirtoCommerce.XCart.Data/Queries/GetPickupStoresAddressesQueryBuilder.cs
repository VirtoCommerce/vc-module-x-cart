using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPickupStoresAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService) :
    SearchQueryBuilder<GetPickupLocationsQuery, PickupLocationsResponse, PickupLocation, PickupLocationsType>(mediator, authorizationService)
{
    protected override string Name => "getPickupInStoreAddresses";
}
