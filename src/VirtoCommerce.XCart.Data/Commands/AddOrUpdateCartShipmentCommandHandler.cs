using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddOrUpdateCartShipmentCommandHandler : CartCommandHandler<AddOrUpdateCartShipmentCommand>
    {
        private readonly ICartAvailMethodsService _cartAvailMethodService;

        public AddOrUpdateCartShipmentCommandHandler(ICartAggregateRepository cartAggregateRepository, ICartAvailMethodsService cartAvailMethodService)
            : base(cartAggregateRepository)
        {
            _cartAvailMethodService = cartAvailMethodService;
        }

        public override async Task<CartAggregate> Handle(AddOrUpdateCartShipmentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var previousShipmentCode = cartAggregate.Cart.Shipments.FirstOrDefault()?.ShipmentMethodCode;

            var shipmentId = request.Shipment.Id?.Value;
            var shipment = cartAggregate.Cart.Shipments.FirstOrDefault(s => shipmentId != null && s.Id == shipmentId);
            shipment = request.Shipment.MapTo(shipment);

            var newShipmentCode = cartAggregate.Cart.Shipments.FirstOrDefault()?.ShipmentMethodCode;
            var shippingChanged = previousShipmentCode != newShipmentCode &&
                                  (previousShipmentCode == "BuyOnlinePickupInStore" ||
                                   newShipmentCode == "BuyOnlinePickupInStore");

            if (shippingChanged)
            {
                cartAggregate.Cart.Shipments.Clear();
            }

            await cartAggregate.AddShipmentAsync(shipment, await _cartAvailMethodService.GetAvailableShippingRatesAsync(cartAggregate));

            if (!request.Shipment.DynamicProperties.IsNullOrEmpty())
            {
                await cartAggregate.UpdateCartShipmentDynamicProperties(shipment, request.Shipment.DynamicProperties);
            }

            cartAggregate = await SaveCartAsync(cartAggregate);
            return await GetCartById(cartAggregate.Cart.Id, request.CultureName);
        }
    }
}
