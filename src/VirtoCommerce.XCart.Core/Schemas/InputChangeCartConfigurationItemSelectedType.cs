using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartConfigurationItemSelectedType : InputCartBaseType
    {
        public InputChangeCartConfigurationItemSelectedType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line item Id");
            Field<NonNullGraphType<ConfigurationSectionKeyInput>>("configurationSection").Description("Configuration section that identifies the configuration item to toggle");
            Field<NonNullGraphType<BooleanGraphType>>("selectedForCheckout").Description("Is configuration item selected for checkout");
        }
    }
}
