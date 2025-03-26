using System.Collections.Generic;
using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public class PickupLocationsResponse
{
    public IEnumerable<PickupLocation> Addresses { get; set; }
    public int TotalCount { get; set; }
}
