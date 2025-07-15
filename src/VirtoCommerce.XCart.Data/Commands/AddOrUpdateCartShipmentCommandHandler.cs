using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model.Search;
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
        private readonly IPickupLocationSearchService _pickupLocationSearchService;

        public AddOrUpdateCartShipmentCommandHandler(
            ICartAggregateRepository cartAggregateRepository,
            ICartAvailMethodsService cartAvailMethodService,
            IPickupLocationSearchService pickupLocationSearchService,
            ICustomerPreferenceService customerPreferenceService)
            : base(cartAggregateRepository)
        {
            _cartAvailMethodService = cartAvailMethodService;
            _customerPreferenceService = customerPreferenceService;
            _pickupLocationSearchService = pickupLocationSearchService;
        }

        public override async Task<CartAggregate> Handle(AddOrUpdateCartShipmentCommand request, CancellationToken cancellationToken)
        {
            var cartAggregate = await GetOrCreateCartFromCommandAsync(request);

            var shipmentId = request.Shipment.Id?.Value;
            var shipment = cartAggregate.Cart.Shipments.FirstOrDefault(s => shipmentId != null && s.Id == shipmentId);
            shipment = request.Shipment.MapTo(shipment);

            ClearAddressInfo(request, shipment);

            if (!cartAggregate.Cart.IsAnonymous)
            {
                var preferenceKey = GeneratePreferenceKey(request, shipment);

                if (request.Shipment.DeliveryAddress?.Value != null || request.Shipment.PickupLocationId?.Value != null)
                {
                    await SaveAddress(request.UserId, preferenceKey, request.Shipment.DeliveryAddress?.Value, request.Shipment.PickupLocationId?.Value);
                }
                await LoadAddress(request.UserId, preferenceKey, shipment);
            }

            await ProvidePickupLocationAddress(shipment);

            await cartAggregate.AddShipmentAsync(shipment, await _cartAvailMethodService.GetAvailableShippingRatesAsync(cartAggregate));

            if (!request.Shipment.DynamicProperties.IsNullOrEmpty())
            {
                await cartAggregate.UpdateCartShipmentDynamicProperties(shipment, request.Shipment.DynamicProperties);
            }

            cartAggregate = await SaveCartAsync(cartAggregate);
            return await GetCartById(cartAggregate.Cart.Id, request.CultureName);
        }

        private async Task ProvidePickupLocationAddress(Shipment shipment)
        {
            if (shipment.PickupLocationId != null && shipment.ShipmentMethodCode == ModuleConstants.BuyOnlinePickupInStoreShipmentCode)
            {
                var criteria = AbstractTypeFactory<PickupLocationSearchCriteria>.TryCreateInstance();
                criteria.ObjectIds = [shipment.PickupLocationId];
                criteria.Take = 1;
                var pickupLocation = (await _pickupLocationSearchService.SearchAsync(criteria)).Results.FirstOrDefault();
                if (pickupLocation != null)
                {
                    shipment.DeliveryAddress = MapPickupLocationAddressTo(pickupLocation.Address);
                }
            }
        }

        private static Address MapPickupLocationAddressTo(PickupLocationAddress address)
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

        private static void ClearAddressInfo(AddOrUpdateCartShipmentCommand request, Shipment shipment)
        {
            if (request.Shipment.DeliveryAddress?.Value == null)
            {
                // don't save previous address when new address is not provided
                shipment.DeliveryAddress = null;
            }

            if (shipment.ShipmentMethodCode == ModuleConstants.BuyOnlinePickupInStoreShipmentCode)
            {
                // Pickup location is used instead of delivery address
                shipment.PickupLocationId = request.Shipment.PickupLocationId?.Value;
                shipment.DeliveryAddress = null;
            }
            else
            {
                shipment.PickupLocationId = null;
            }
        }

        private string[] GeneratePreferenceKey(AddOrUpdateCartShipmentCommand request, Shipment shipment)
        {
            var result = new List<string>
                {
                    request.OrganizationId,
                    request.Shipment.ShipmentMethodCode?.Value ?? shipment.ShipmentMethodCode ?? ModuleConstants.FixedRateShipmentCode
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
            var savedValue = await _customerPreferenceService.GetValue(userId, key);

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

        private async Task SaveAddress(string userId, IList<string> key, ExpCartAddress address, string pickupLocationId)
        {
            if (address != null)
            {
                var value = pickupLocationId ?? JsonConvert.SerializeObject(address);
                await _customerPreferenceService.SaveValue(userId, key, value);
            }
        }
    }
}
