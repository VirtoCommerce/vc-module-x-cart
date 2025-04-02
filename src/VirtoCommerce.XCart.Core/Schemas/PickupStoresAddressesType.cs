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
        Field(x => x.GeoLocation, nullable: true).Description("GeoLocation");
        Field(x => x.Address, typeof(PickupAddressType)).Description("Address");
    }
}

public sealed class PickupAddressType : ExtendableGraphType<PickupLocationAddress>
{
    public PickupAddressType()
    {
        Field(x => x.Id).Description("Id");
        Field(x => x.Key, true).Description("Key");
        Field(x => x.Name, nullable: true).Description("Name");
        Field(x => x.Organization, nullable: true).Description("Company name");
        Field(x => x.CountryCode, nullable: true).Description("Country code");
        Field(x => x.CountryName, nullable: true).Description("Country name");
        Field(x => x.City, nullable: true).Description("City");
        Field(x => x.PostalCode, nullable: true).Description("Postal code");
        Field(x => x.Line1, nullable: true).Description("Line1");
        Field(x => x.Line2, nullable: true).Description("Line2");
        Field(x => x.RegionId, nullable: true).Description("Region id");
        Field(x => x.RegionName, nullable: true).Description("Region name");
        Field(x => x.Phone, nullable: true).Description("Phone");
        Field(x => x.Email, nullable: true).Description("Email");
        Field(x => x.OuterId, nullable: true).Description("Outer id");
        Field(x => x.Description, nullable: true).Description("Description");
    }
}
