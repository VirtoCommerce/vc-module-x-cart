using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class CartPickupLocationsQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
    : SearchQueryBuilder<CartPickupLocationsQuery, CartPickupLocationSearchResult, CartPickupLocation, CartPickupLocationType>(mediator, authorizationService)
{
    protected override string Name => "cartPickupLocations";
}
