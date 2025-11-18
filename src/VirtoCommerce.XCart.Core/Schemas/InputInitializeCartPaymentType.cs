using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputInitializeCartPaymentType : InputObjectGraphType
{
    public InputInitializeCartPaymentType()
    {
        Field<StringGraphType>("cartId");
        Field<NonNullGraphType<StringGraphType>>("paymentId").Description("Payment Id");
    }
}
