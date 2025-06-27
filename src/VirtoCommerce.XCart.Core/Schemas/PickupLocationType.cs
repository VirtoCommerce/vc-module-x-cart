using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas;

public sealed class PickupLocationType : ExtendableGraphType<PickupLocation>
{
    public PickupLocationType()
    {
        Field(x => x.Id).Description("Id");
        Field(x => x.IsActive, false).Description("IsActive");
        Field(x => x.Name, false).Description("Name");
        Field(x => x.Description, nullable: true).Description("Description");
        Field(x => x.ContactEmail, nullable: true).Description("ContactEmail");
        Field(x => x.ContactPhone, nullable: true).Description("ContactPhone");
        Field(x => x.WorkingHours, nullable: true).Description("WorkingHours");
        Field(x => x.ReadyForPickup, nullable: true).Description("Days until ready for pickup");
        Field(x => x.PickupDeadline, nullable: true).Description("Pickup duration in days");
        Field(x => x.GeoLocation, nullable: true).Description("GeoLocation");
        Field(x => x.Address, typeof(PickupAddressType)).Description("Address");
    }
}
