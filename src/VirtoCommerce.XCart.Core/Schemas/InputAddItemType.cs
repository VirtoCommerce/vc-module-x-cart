using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddItemType : InputCartBaseType
    {
        public InputAddItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("productId")
                .Description("Product ID");
            Field<NonNullGraphType<IntGraphType>>("quantity")
                .Description("Quantity");
            Field<DecimalGraphType>("price")
                .Description("Price");
            Field<StringGraphType>("comment")
                .Description("Comment");

            Field<ListGraphType<InputDynamicPropertyValueType>>("dynamicProperties");

            // Configurable product support
            Field<ListGraphType<ConfigurationSectionInput>>("configurationSections");
        }
    }
}
