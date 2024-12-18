using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateWishlistItemsType : ExtendableInputGraphType
    {
        public InputUpdateWishlistItemsType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "Wish list id");
            Field<NonNullGraphType<ListGraphType<InputUpdateWishlistLineItemType>>>("items", "Bulk wishlist items");
        }
    }
}
