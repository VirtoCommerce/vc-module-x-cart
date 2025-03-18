using System.Collections.Generic;
using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public class PickupStoresAddressesResponse
{
    public IEnumerable<Address> Addresses { get; set; }
}
