using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveWishlistType : InputObjectGraphType
    {
        public InputRemoveWishlistType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("List ID");
        }
    }
}
