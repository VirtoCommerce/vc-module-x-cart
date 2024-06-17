using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemCommentType : InputCartBaseType
    {
        public InputChangeCartItemCommentType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId",
                "Line item Id");
            Field<NonNullGraphType<StringGraphType>>("comment",
                "Comment");
        }
    }
}
