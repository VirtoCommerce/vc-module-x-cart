using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Validators
{
    public class ShipmentValidationContext
    {
        public IEnumerable<ShippingRate> AvailShippingRates { get; set; }
        public Shipment Shipment { get; set; }
    }
}
