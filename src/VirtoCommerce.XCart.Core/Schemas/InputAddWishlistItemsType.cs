using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddWishlistItemsType : ExtendableInputGraphType
    {
        public InputAddWishlistItemsType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputNewWishlistItemType>>>>("listItems", "List items");
        }
    }
}
