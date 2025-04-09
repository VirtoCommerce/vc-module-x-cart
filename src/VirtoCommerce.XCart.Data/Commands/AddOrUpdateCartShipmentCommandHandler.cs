using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
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

            Log.Logger.Information("Change shipping info: {0}, {1}, {2}, {3}",
                request.UserId, previousShipmentCode,
                newShipmentCode, shippingChanged);

            if (shippingChanged)
            {
                Log.Logger.Information("Set address to null: {0}", request.UserId);
                shipment.DeliveryAddress = null;
            }

            Log.Logger.Information("add shipment: {0}, {1}",
                request.UserId, shipment.DeliveryAddress == null ? "null" : shipment.DeliveryAddress);
            await cartAggregate.AddShipmentAsync(shipment, await _cartAvailMethodService.GetAvailableShippingRatesAsync(cartAggregate));

            Log.Logger.Information("dynamic properties: {0}, {1}",
                request.UserId, shipment.DeliveryAddress == null ? "null" : shipment.DeliveryAddress);
            if (!request.Shipment.DynamicProperties.IsNullOrEmpty())
            {
                Log.Logger.Information("update dynamic properties: {0}, {1}",
                    request.UserId, shipment.DeliveryAddress == null ? "null" : shipment.DeliveryAddress);
                await cartAggregate.UpdateCartShipmentDynamicProperties(shipment, request.Shipment.DynamicProperties);
            }

            Log.Logger.Information("save cart async: {0}, {1}",
                request.UserId, shipment.DeliveryAddress == null ? "null" : shipment.DeliveryAddress);
            cartAggregate = await SaveCartAsync(cartAggregate);
            Log.Logger.Information("get cart by id: {0}, {1}",
                request.UserId, shipment.DeliveryAddress == null ? "null" : shipment.DeliveryAddress);
            var result = await GetCartById(cartAggregate.Cart.Id, request.CultureName);
            Log.Logger.Information("return cart: {0}, {1}",
                request.UserId, shipment.DeliveryAddress);
            return result;
        }
    }
}
