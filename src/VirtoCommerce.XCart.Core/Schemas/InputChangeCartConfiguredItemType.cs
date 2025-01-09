using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartConfiguredItemType : InputCartBaseType
    {
        public InputChangeCartConfiguredItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line item Id");
            Field<IntGraphType>("quantity").Description("Quantity");
            Field<ListGraphType<ConfigurationSectionInput>>("configurationSections").Description("Configuration sections");
        }
    }
}
