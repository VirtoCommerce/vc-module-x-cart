using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartConfiguredItemType : InputCartBaseType
    {
        public InputChangeCartConfiguredItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId", "Line item Id");
            Field<IntGraphType>("quantity", "Quantity");
            Field<ListGraphType<ConfigurationSectionInput>>("configurationSections", "Configuration sections");
        }
    }
}
