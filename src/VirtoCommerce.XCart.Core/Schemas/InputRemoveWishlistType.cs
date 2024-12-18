using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveWishlistType : ExtendableInputGraphType
    {
        public InputRemoveWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "List ID");
        }
    }
}
