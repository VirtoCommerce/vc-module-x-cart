using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveWishlistItemsType : ExtendableInputGraphType
    {
        public InputRemoveWishlistItemsType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "List ID");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("lineItemIds", "Line item IDs to remove");
        }
    }
}
