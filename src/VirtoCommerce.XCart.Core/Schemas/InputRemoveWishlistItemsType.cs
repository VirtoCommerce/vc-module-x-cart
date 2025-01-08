using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveWishlistItemsType : InputObjectGraphType
    {
        public InputRemoveWishlistItemsType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("List ID");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("lineItemIds").Description("Line item IDs to remove");
        }
    }
}
