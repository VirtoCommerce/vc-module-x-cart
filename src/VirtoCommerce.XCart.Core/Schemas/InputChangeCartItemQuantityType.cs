using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemQuantityType : InputCartBaseType
    {
        public InputChangeCartItemQuantityType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId")
                .Description("Line item Id");
            Field<NonNullGraphType<IntGraphType>>("quantity")
                .Description("Quantity");
        }
    }
}
