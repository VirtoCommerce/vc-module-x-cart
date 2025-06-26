using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddOrUpdateCartShipmentCommandHandler : CartCommandHandler<AddOrUpdateCartShipmentCommand>
    {
        private readonly ICartAvailMethodsService _cartAvailMethodService;
        private readonly ICustomerPreferenceService _customerPreferenceService;

        public AddOrUpdateCartShipmentCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartAvailMethodsService cartAvailMethodService,
            ICustomerPreferenceService customerPreferenceService)
            : base(cartAggregateRepository)
        {
            _cartAvailMethodService = cartAvailMethodService;
            _customerPreferenceService = customerPreferenceService;
        }

        public override async Task<CartAggregate> Handle(AddOrUpdateCartShipmentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var shipmentId = request.Shipment.Id?.Value;
            var shipment = cartAggregate.Cart.Shipments.FirstOrDefault(s => shipmentId != null && s.Id == shipmentId);
            shipment = request.Shipment.MapTo(shipment);

            var preferenceKey = GeneratePreferenceKey(request, shipment);

            if (preferenceKey.Length != 0)
            {
                if (request.Shipment.DeliveryAddress?.Value == null)
                {
                    await LoadAddress(request.UserId, preferenceKey, shipment);
                }
                else
                {
                    await SaveAddress(request.UserId, preferenceKey, request.Shipment.DeliveryAddress?.Value);
                }
            }

            await cartAggregate.AddShipmentAsync(shipment, await _cartAvailMethodService.GetAvailableShippingRatesAsync(cartAggregate));

            if (!request.Shipment.DynamicProperties.IsNullOrEmpty())
            {
                await cartAggregate.UpdateCartShipmentDynamicProperties(shipment, request.Shipment.DynamicProperties);
            }

            cartAggregate = await SaveCartAsync(cartAggregate);
            return await GetCartById(cartAggregate.Cart.Id, request.CultureName);
        }

        private string[] GeneratePreferenceKey(AddOrUpdateCartShipmentCommand request, Shipment shipment)
        {
            if (request.UserId == Xapi.Core.ModuleConstants.AnonymousUser.UserName)
            {
                return [];
            }

            var result = new List<string>
                {
                    request.OrganizationId,
                    request.Shipment.ShipmentMethodCode?.Value ?? shipment.ShipmentMethodCode ?? "FixedRate"
                }
                .Where(x => !x.IsNullOrEmpty())
                .ToList();

            if (result.Count == 0)
            {
                return [];
            }

            return ["CartShipmentLastAddress", .. result];
        }

        private async Task LoadAddress(string userId, IList<string> key, Shipment shipment)
        {
            var savedAddress = await _customerPreferenceService.GetValue(userId, key);

            if (savedAddress != null)
            {
                var address = JsonConvert.DeserializeObject<ExpCartAddress>(savedAddress);
                shipment.DeliveryAddress = AbstractTypeFactory<Address>.TryCreateInstance();
                address.MapTo(shipment.DeliveryAddress);
            }
            else
            {
                shipment.DeliveryAddress = null;
            }
        }

        private async Task SaveAddress(string userId, IList<string> key, ExpCartAddress address)
        {
            if (address != null)
            {
                var value = JsonConvert.SerializeObject(address);
                await _customerPreferenceService.SaveValue(userId, key, value);
            }
        }
    }
}
