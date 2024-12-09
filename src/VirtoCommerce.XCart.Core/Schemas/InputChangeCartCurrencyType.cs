using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartCurrencyType : InputCartBaseType
    {
        public InputChangeCartCurrencyType()
        {
            Field<NonNullGraphType<StringGraphType>>("newCurrencyCode", "Second cart currency");
        }
    }
}
