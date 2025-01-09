using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartPaymentDynamicPropertiesType : InputCartBaseType
    {
        public InputUpdateCartPaymentDynamicPropertiesType()
        {
            Field<NonNullGraphType<StringGraphType>>("paymentId")
                .Description("Payment Id");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties")
                .Description("Dynamic properties");
        }
    }
}
