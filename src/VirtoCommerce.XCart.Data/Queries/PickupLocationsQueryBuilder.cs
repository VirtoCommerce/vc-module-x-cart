using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.XCart.Data.Queries;

public class PickupLocationsQueryBuilder(IAuthorizationService authorizationService)
    : SearchQueryBuilder<PickupLocationsQuery, PickupLocationSearchResult, PickupLocation, PickupLocationType>(authorizationService)
{
    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public PickupLocationsQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : this(authorizationService)
    {
    }

    protected override string Name => "pickupLocations";
}
