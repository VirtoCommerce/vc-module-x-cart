using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Models
{
    public class ShipmentContextCartMap
    {
        public CartAggregate CartAggregate { get; set; }

        public Shipment Shipment { get; set; }
    }
}
