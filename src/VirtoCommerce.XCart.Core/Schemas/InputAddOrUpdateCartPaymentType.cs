using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddOrUpdateCartPaymentType : InputCartBaseType
    {
        public InputAddOrUpdateCartPaymentType()
        {
            Field<NonNullGraphType<InputPaymentType>>("payment",
                "Payment");
        }
    }
}
