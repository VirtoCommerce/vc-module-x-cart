using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRenameWishlistType : InputObjectGraphType
    {
        public InputRenameWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "List ID");
            Field<StringGraphType>("listName", description: "New List name");
        }
    }
}
