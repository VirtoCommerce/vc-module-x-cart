using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemSelectedType : InputCartBaseType
    {
        public InputChangeCartItemSelectedType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line item Id");
            Field<NonNullGraphType<BooleanGraphType>>("selectedForCheckout").Description("Is item selected for checkout");
        }
    }
}
