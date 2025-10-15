using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using ModuleConstants = VirtoCommerce.ShippingModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Commands
{
    public class AddOrUpdateCartShipmentCommandHandler : CartCommandHandler<AddOrUpdateCartShipmentCommand>
    {
        private readonly ICartAvailMethodsService _cartAvailMethodService;
        private readonly ICustomerPreferenceService _customerPreferenceService;
        private readonly IPickupLocationService _pickupLocationService;

        public AddOrUpdateCartShipmentCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartAvailMethodsService cartAvailMethodService,
            IPickupLocationService pickupLocationService,
            ICustomerPreferenceService customerPreferenceService)
            : base(cartAggregateRepository)
        {
            _cartAvailMethodService = cartAvailMethodService;
            _customerPreferenceService = customerPreferenceService;
            _pickupLocationService = pickupLocationService;
        }

        public override async Task<CartAggregate> Handle(AddOrUpdateCartShipmentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var shipmentId = request.Shipment.Id?.Value;
            var shipment = cartAggregate.Cart.Shipments.FirstOrDefault(s => shipmentId != null && s.Id == shipmentId);
            var previousShipmentCode = shipment?.ShipmentMethodCode;
            shipment = request.Shipment.MapTo(shipment);

            ClearAddressInfo(request, shipment, previousShipmentCode);

            if (!cartAggregate.Cart.IsAnonymous)
            {
                var preferenceKey = GeneratePreferenceKey(request, shipment, cartAggregate);

                if (request.Shipment.DeliveryAddress?.Value != null || request.Shipment.PickupLocationId?.Value != null)
                {
                    await SaveAddressToPreferencesAsync(request.UserId, preferenceKey, request.Shipment.DeliveryAddress?.Value, request.Shipment.PickupLocationId?.Value, cancellationToken);
                }

                await LoadAddressFromPreferencesAsync(request.UserId, preferenceKey, shipment, cartAggregate, cancellationToken);
            }

            await SetPickupLocationAddressAsync(shipment, cartAggregate, cancellationToken);

            await cartAggregate.AddShipmentAsync(shipment, await _cartAvailMethodService.GetAvailableShippingRatesAsync(cartAggregate));

            if (!request.Shipment.DynamicProperties.IsNullOrEmpty())
            {
                await cartAggregate.UpdateCartShipmentDynamicProperties(shipment, request.Shipment.DynamicProperties);
            }

            cartAggregate = await SaveCartAsync(cartAggregate);
            return await GetCartById(cartAggregate.Cart.Id, request.CultureName);
        }

        protected virtual async Task SetPickupLocationAddressAsync(Shipment shipment, CartAggregate cartAggregate, CancellationToken cancellationToken = default)
        {
            if (shipment.PickupLocationId != null && shipment.ShipmentMethodCode == ModuleConstants.BuyOnlinePickupInStoreShipmentCode)
            {
                var pickupLocation = await _pickupLocationService.GetByIdAsync(shipment.PickupLocationId);
                if (pickupLocation != null)
                {
                    shipment.DeliveryAddress = ConvertFromPickupLocationAddress(pickupLocation.Address);
                }
            }
        }

        protected virtual IList<string> GeneratePreferenceKey(AddOrUpdateCartShipmentCommand request, Shipment shipment, CartAggregate cartAggregate)
        {
            var result = new List<string> { "CartShipmentLastAddress" };

            if (!request.OrganizationId.IsNullOrEmpty())
            {
                result.Add(request.OrganizationId);
            }

            result.Add(request.Shipment.ShipmentMethodCode?.Value.EmptyToNull() ?? shipment.ShipmentMethodCode ?? ModuleConstants.FixedRateShipmentCode);

            return result;
        }

        protected virtual async Task LoadAddressFromPreferencesAsync(string userId, IList<string> preferenceKey, Shipment shipment, CartAggregate cartAggregate, CancellationToken cancellationToken = default)
        {
            var savedValue = await _customerPreferenceService.GetValue(userId, preferenceKey);

            if (savedValue != null)
            {
                if (shipment.ShipmentMethodCode == ModuleConstants.BuyOnlinePickupInStoreShipmentCode)
                {
                    shipment.PickupLocationId = savedValue;
                }
                else
                {
                    var address = JsonConvert.DeserializeObject<ExpCartAddress>(savedValue);
                    shipment.DeliveryAddress = AbstractTypeFactory<Address>.TryCreateInstance();
                    address.MapTo(shipment.DeliveryAddress);
                }
            }
            else
            {
                shipment.DeliveryAddress = null;
            }
        }

        protected virtual async Task SaveAddressToPreferencesAsync(string userId, IList<string> preferenceKey, ExpCartAddress address, string pickupLocationId, CancellationToken cancellationToken = default)
        {
            if (address != null || pickupLocationId != null)
            {
                var value = pickupLocationId ?? JsonConvert.SerializeObject(address);
                await _customerPreferenceService.SaveValue(userId, preferenceKey, value);
            }
        }

        protected static void ClearAddressInfo(AddOrUpdateCartShipmentCommand request, Shipment shipment, string previousShipmentCode)
        {
            if (shipment.ShipmentMethodCode != previousShipmentCode || request.Shipment.DeliveryAddress is { IsSpecified: true, Value: null })
            {
                shipment.DeliveryAddress = null;
            }

            if (shipment.ShipmentMethodCode == ModuleConstants.BuyOnlinePickupInStoreShipmentCode)
            {
                shipment.DeliveryAddress = null;
            }
            else
            {
                shipment.PickupLocationId = null;
            }
        }

        protected static Address ConvertFromPickupLocationAddress(PickupLocationAddress address)
        {
            var result = AbstractTypeFactory<Address>.TryCreateInstance();

            result.AddressType = address.AddressType;
            result.Key = address.Key;
            result.Name = address.Name;
            result.Organization = address.Organization;
            result.CountryCode = address.CountryCode;
            result.CountryName = address.CountryName;
            result.City = address.City;
            result.PostalCode = address.PostalCode;
            result.Zip = address.Zip;
            result.Line1 = address.Line1;
            result.Line2 = address.Line2;
            result.RegionId = address.RegionId;
            result.RegionName = address.RegionName;
            result.FirstName = address.FirstName;
            result.MiddleName = address.MiddleName;
            result.LastName = address.LastName;
            result.Phone = address.Phone;
            result.Email = address.Email;
            result.OuterId = address.OuterId;
            result.IsDefault = address.IsDefault;
            result.Description = address.Description;

            return result;
        }
    }
}
