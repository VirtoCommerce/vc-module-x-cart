using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas;

public class PickupLocationType : ExtendableGraphType<PickupLocation>
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
        Field(x => x.GeoLocation, nullable: true).Description("GeoLocation");
        ExtendableField<PickupAddressType>("address", "Address", resolve: context => context.Source.Address);
    }
}
