using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetPickupLocationsQuery : SearchQuery<PickupLocationsResponse>, ISearchQuery
{
    public string StoreId { get; set; }
}
