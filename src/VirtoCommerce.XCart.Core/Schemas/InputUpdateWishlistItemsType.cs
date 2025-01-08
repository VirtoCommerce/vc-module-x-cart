using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateWishlistItemsType : InputObjectGraphType
    {
        public InputUpdateWishlistItemsType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("Wish list id");
            Field<NonNullGraphType<ListGraphType<InputUpdateWishlistLineItemType>>>("items").Description("Bulk wishlist items");
        }
    }
}
