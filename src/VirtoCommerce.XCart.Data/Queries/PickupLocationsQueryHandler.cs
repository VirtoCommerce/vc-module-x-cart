using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Data.Queries;

public class PickupLocationsQueryHandler(IPickupLocationSearchService service) : IQueryHandler<PickupLocationsQuery, PickupLocationSearchResult>
{
    public async Task<PickupLocationSearchResult> Handle(PickupLocationsQuery request, CancellationToken cancellationToken)
    {
        var searchCriteria = AbstractTypeFactory<PickupLocationSearchCriteria>.TryCreateInstance();

        searchCriteria.IsActive = true;
        searchCriteria.Keyword = request.Keyword;
        searchCriteria.StoreId = request.StoreId;
        searchCriteria.Skip = request.Skip;
        searchCriteria.Take = request.Take;

        return await service.SearchAsync(searchCriteria);
    }
}
