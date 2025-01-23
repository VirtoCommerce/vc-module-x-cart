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

            Field<DateTimeGraphType>("createdDate", "Create date. Optional, to manually control line item position in the cart if required. ISO-8601 format, for example: 2025-01-23T11:46:11Z");
        }
    }
}
