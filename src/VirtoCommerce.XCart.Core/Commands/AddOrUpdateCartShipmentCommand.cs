using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Commands
{
    public class AddOrUpdateCartShipmentCommand : CartCommand
    {
        public ExpCartShipment Shipment { get; set; }
    }
}
