using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCommentType : InputCartBaseType
    {
        public InputChangeCommentType()
        {
            Field<StringGraphType>("comment",
                "Comment");
        }
    }
}
