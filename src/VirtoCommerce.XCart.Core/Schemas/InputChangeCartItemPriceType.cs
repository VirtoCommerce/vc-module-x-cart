using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemPriceType : InputCartBaseType
    {
        public InputChangeCartItemPriceType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId")
                .Description("Line item Id");
            Field<NonNullGraphType<DecimalGraphType>>("price")
                .Description("Price");
        }
    }
}
