using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models;

public class CartPickupLocation
{
    public PickupLocation PickupLocation { get; set; }

    public string AvailabilityType { get; set; }

    public string Note { get; set; }
}
