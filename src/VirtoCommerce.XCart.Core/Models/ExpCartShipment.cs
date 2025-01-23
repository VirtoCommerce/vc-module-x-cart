using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XCart.Core.Models
{
    public class ExpCartShipment
    {
        public Optional<string> Id { get; set; }
        public Optional<string> FulfillmentCenterId { get; set; }
        public Optional<decimal?> Length { get; set; }
        public Optional<decimal?> Height { get; set; }
        public Optional<string> MeasureUnit { get; set; }
        public Optional<string> ShipmentMethodOption { get; set; }
        public Optional<string> ShipmentMethodCode { get; set; }
        public Optional<decimal?> VolumetricWeight { get; set; }
        public Optional<decimal?> Weight { get; set; }
        public Optional<string> WeightUnit { get; set; }
        public Optional<decimal?> Width { get; set; }
        public Optional<string> Currency { get; set; }
        public Optional<decimal> Price { get; set; }
        public Optional<string> Comment { get; set; }
        public Optional<string> VendorId { get; set; }
        public Optional<ExpCartAddress> DeliveryAddress { get; set; }

        public IList<DynamicPropertyValue> DynamicProperties { get; set; }

        public virtual Shipment MapTo(Shipment shipment)
        {
            if (shipment == null)
            {
                shipment = AbstractTypeFactory<Shipment>.TryCreateInstance();
            }

            Optional.SetValue(Id, x => shipment.Id = x);
            Optional.SetValue(FulfillmentCenterId, x => shipment.FulfillmentCenterId = x);
            Optional.SetValue(Length, x => shipment.Length = x);
            Optional.SetValue(Height, x => shipment.Height = x);
            Optional.SetValue(MeasureUnit, x => shipment.MeasureUnit = x);
            Optional.SetValue(ShipmentMethodOption, x => shipment.ShipmentMethodOption = x);
            Optional.SetValue(ShipmentMethodCode, x => shipment.ShipmentMethodCode = x);
            Optional.SetValue(VolumetricWeight, x => shipment.VolumetricWeight = x);
            Optional.SetValue(Weight, x => shipment.Weight = x);
            Optional.SetValue(WeightUnit, x => shipment.WeightUnit = x);
            Optional.SetValue(Width, x => shipment.Width = x);
            Optional.SetValue(Currency, x => shipment.Currency = x);
            Optional.SetValue(Price, x => shipment.Price = x);
            Optional.SetValue(Comment, x => shipment.Comment = x);
            Optional.SetValue(VendorId, x => shipment.VendorId = x);
            Optional.SetValue(DeliveryAddress, x => shipment.DeliveryAddress = x?.MapTo(shipment.DeliveryAddress));

            return shipment;
        }
    }
}
