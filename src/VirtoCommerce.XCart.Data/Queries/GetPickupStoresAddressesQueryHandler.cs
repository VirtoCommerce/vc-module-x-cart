using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
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
        var searchCriteria = AbstractTypeFactory<PickupLocationsSearchCriteria>.TryCreateInstance();

        searchCriteria.Keyword = request.Keyword;
        searchCriteria.StoreId = request.StoreId;
        searchCriteria.Skip = request.Skip;
        searchCriteria.Take = request.Take;

        var result = await service.SearchAsync(searchCriteria);

        var response = AbstractTypeFactory<PickupLocationsResponse>.TryCreateInstance();

        response.Addresses = result.Results;
        response.TotalCount = result.TotalCount;

        return response;

    }
}
