using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputAddConfigurationItemsType : InputCartBaseType
{
    public InputAddConfigurationItemsType()
    {
        Field<NonNullGraphType<StringGraphType>>("lineItemId")
            .Description("Line item ID");

        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ConfigurationSectionInput>>>>("configurationSections")
            .Description("List of configuration sections to add");
    }
}
