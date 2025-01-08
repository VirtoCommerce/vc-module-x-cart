using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartItemDynamicPropertiesType : InputCartBaseType
    {
        public InputUpdateCartItemDynamicPropertiesType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId")
                .Description("Line item Id");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties")
                .Description("Dynamic properties");
        }
    }
}
