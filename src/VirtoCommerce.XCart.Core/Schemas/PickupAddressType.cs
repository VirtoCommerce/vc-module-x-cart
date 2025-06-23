using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;
using IntGraphType = GraphQL.Types.IntGraphType;

namespace VirtoCommerce.XCart.Core.Schemas;

public class PickupAddressType : ExtendableGraphType<PickupLocationAddress>
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

        Field<IntGraphType>(nameof(PickupLocationAddress.AddressType))
            .Description("Address type")
            .Resolve(context => (int)context.Source.AddressType);
    }
}
