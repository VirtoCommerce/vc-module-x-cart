using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputRemoveConfigurationItemType : InputCartBaseType
{
    public InputRemoveConfigurationItemType()
    {
        Field<NonNullGraphType<StringGraphType>>("lineItemId")
            .Description("Line item ID");

        Field<NonNullGraphType<ConfigurationSectionInput>>("configurationSection")
            .Description("Configuration section to remove");
    }
}
