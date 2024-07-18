using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddWishlistItemsType : InputObjectGraphType
    {
        public InputAddWishlistItemsType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputNewWishlistItemType>>>>("listItems", "List items");
        }
    }
}
