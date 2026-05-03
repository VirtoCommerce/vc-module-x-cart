using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartConfigurationItemsSelectedType : InputCartBaseType
    {
        public InputChangeCartConfigurationItemsSelectedType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line item Id");
            Field<ListGraphType<NonNullGraphType<ConfigurationSectionKeyInput>>>("configurationSections").Description("Configuration sections that identify the configuration items to toggle");
        }
    }
}
