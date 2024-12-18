using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveWishlistItemType : ExtendableInputGraphType
    {
        public InputRemoveWishlistItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "List ID");
            Field<StringGraphType>("lineItemId", "Line item ID to remove");
            Field<StringGraphType>("productId", "Line item product ID to remove");
        }
    }
}
