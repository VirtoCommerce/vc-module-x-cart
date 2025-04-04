using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class PickupLocationsQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
    : SearchQueryBuilder<PickupLocationsQuery, PickupLocationSearchResult, PickupLocation, PickupLocationType>(mediator, authorizationService)
{
    protected override string Name => "pickupLocations";
}
