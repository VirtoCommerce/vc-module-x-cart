using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputSavedPaymentMethodIdType : InputObjectGraphType
{
    public InputSavedPaymentMethodIdType()
    {
        Name = "InputSavedPaymentMethodId";
        Field<NonNullGraphType<StringGraphType>>("id");
    }
}
