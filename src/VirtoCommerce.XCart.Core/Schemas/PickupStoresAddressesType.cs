using GraphQL.Types;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas;

public sealed class PickupStoresAddressesType : ExtendableGraphType<PickupStoresAddressesResponse>
{
    public PickupStoresAddressesType()
    {
        ExtendableField<ListGraphType<PickupAddressType>>(
            "addresses",
            "Pickup Stores Addresses",
            resolve: context => context.Source.Addresses ?? []);
    }
}

public sealed class PickupAddressType : ExtendableGraphType<Address>
{
    public PickupAddressType()
    {
        Field(x => x.Id).Description("Id");
        Field(x => x.City).Description("City");
        Field(x => x.CountryCode).Description("Country code");
        Field(x => x.CountryName, true).Description("Country name");
        Field(x => x.Email, true).Description("Email");
        Field(x => x.Key, true).Description("key");
        Field(x => x.Line1).Description("Line1");
        Field(x => x.Line2, true).Description("Line2");
        Field(x => x.Name, true).Description("Name");
        Field(x => x.Organization, true).Description("Company name");
        Field(x => x.Phone, true).Description("Phone");
        Field(x => x.PostalCode).Description("Postal code");
        Field(x => x.RegionId, true).Description("Region id");
        Field(x => x.RegionName, true).Description("Region name");
        Field(x => x.OuterId, true).Description("Outer id");
        Field(x => x.Description, true).Description("Description");
    }
}
