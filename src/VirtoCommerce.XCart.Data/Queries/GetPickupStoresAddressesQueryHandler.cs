using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.XCart.Data.Queries;

public class GetPickupStoresAddressesQueryHandler(IPickupService service) : IQueryHandler<GetPickupStoresAddressesQuery, PickupStoresAddressesResponse>
{
    public async Task<PickupStoresAddressesResponse> Handle(GetPickupStoresAddressesQuery request, CancellationToken cancellationToken)
    {
        var result = await service.GetAddresses();
        return new PickupStoresAddressesResponse
        {
            Addresses = result
        };
    }
}
