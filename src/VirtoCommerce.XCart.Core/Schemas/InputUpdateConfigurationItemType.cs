using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputUpdateConfigurationItemType : InputCartBaseType
{
    public InputUpdateConfigurationItemType()
    {
        Field<NonNullGraphType<StringGraphType>>("lineItemId")
            .Description("Line item ID");

        Field<NonNullGraphType<ConfigurationSectionInput>>("configurationSection")
            .Description("Configuration section with updated quantity");
    }
}
