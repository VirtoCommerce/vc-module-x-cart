using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputAddConfigurationItemType : InputCartBaseType
{
    public InputAddConfigurationItemType()
    {
        Field<NonNullGraphType<StringGraphType>>("lineItemId")
            .Description("Line item ID");

        Field<NonNullGraphType<ConfigurationSectionInput>>("configurationSection")
            .Description("Configuration section to add");
    }
}
