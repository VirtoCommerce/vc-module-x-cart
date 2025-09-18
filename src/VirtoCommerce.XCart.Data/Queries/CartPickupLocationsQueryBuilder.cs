using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class CartPickupLocationsQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
    : SearchQueryBuilder<CartPickupLocationsQuery, ProductPickupLocationSearchResult, ProductPickupLocation, ProductPickupLocationType>(mediator, authorizationService)
{
    protected override string Name => "cartPickupLocations";
}
