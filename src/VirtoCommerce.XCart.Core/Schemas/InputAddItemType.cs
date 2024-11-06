using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddItemType : InputCartBaseType
    {
        public InputAddItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("productId",
                "Product ID");
            Field<NonNullGraphType<IntGraphType>>("quantity",
                "Quantity");
            Field<DecimalGraphType>("price",
                "Price");
            Field<StringGraphType>("comment",
                "Comment");

            Field<ListGraphType<InputDynamicPropertyValueType>>("dynamicProperties");

            // Configurable product support
            Field<ListGraphType<ConfigurationSectionInput>>("configurationSections");
        }
    }
}
