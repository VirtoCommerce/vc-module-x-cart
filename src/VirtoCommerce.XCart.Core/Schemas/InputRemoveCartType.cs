using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveCartType : InputObjectGraphType
    {
        public InputRemoveCartType()
        {
            Field<NonNullGraphType<StringGraphType>>("cartId").Description("Cart Id");
            Field<NonNullGraphType<StringGraphType>>("userId").Description("User Id");
        }
    }
}
