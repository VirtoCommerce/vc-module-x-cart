using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPickupStoresAddressesQueryHandler(IPickupLocationsSearchService service) : IQueryHandler<GetPickupLocationsQuery, PickupLocationsResponse>
{
    public async Task<PickupLocationsResponse> Handle(GetPickupLocationsQuery request, CancellationToken cancellationToken)
    {
        var searchCriteria = new PickupLocationsSearchCriteria
        {
            StoreId = request.StoreId,
            Skip = request.Skip,
            Take = request.Take
        };
        var result = await service.SearchAsync(searchCriteria);
        return new PickupLocationsResponse
        {
            Addresses = result.Results,
            TotalCount = result.TotalCount
        };
    }
}
