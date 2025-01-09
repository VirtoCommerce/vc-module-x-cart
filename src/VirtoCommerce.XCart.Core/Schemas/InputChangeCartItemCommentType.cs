using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemCommentType : InputCartBaseType
    {
        public InputChangeCartItemCommentType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId")
                .Description("Line item Id");
            Field<NonNullGraphType<StringGraphType>>("comment")
                .Description("Comment");
        }
    }
}
