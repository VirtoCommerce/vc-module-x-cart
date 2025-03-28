using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Queries;

public class GetPickupLocationsQuery : SearchQuery<PickupLocationsResponse>
{
    public string StoreId { get; set; }
}
