using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRenameWishlistType : InputObjectGraphType
    {
        public InputRenameWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("List ID");
            Field<StringGraphType>("listName").Description("New List name");
        }
    }
}
