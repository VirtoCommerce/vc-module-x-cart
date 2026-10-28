using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas;

public class InputAddSavedPaymentMethodType : InputObjectGraphType
{
    public InputAddSavedPaymentMethodType()
    {
        Name = "InputAddSavedPaymentMethod";
        Field<NonNullGraphType<StringGraphType>>("storeId");
        Field<NonNullGraphType<StringGraphType>>("paymentMethodCode");
        Field<ListGraphType<InputKeyValueType>>("parameters").Description("What the payment provider's card form posted: tokens, nonces, billing address.");
        Field<BooleanGraphType>("setDefault");
    }
}
