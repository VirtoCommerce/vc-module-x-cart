using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRenameWishlistType : ExtendableInputGraphType
    {
        public InputRenameWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "List ID");
            Field<StringGraphType>("listName", description: "New List name");
        }
    }
}
