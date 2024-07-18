using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemSelectedType : InputCartBaseType
    {
        public InputChangeCartItemSelectedType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId", "Line item Id");
            Field<NonNullGraphType<BooleanGraphType>>("selectedForCheckout", "Is item selected for checkout");
        }
    }
}
