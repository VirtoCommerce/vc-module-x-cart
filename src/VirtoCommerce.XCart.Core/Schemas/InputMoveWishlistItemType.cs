using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputMoveWishlistItemType : InputObjectGraphType
    {
        public InputMoveWishlistItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("Source List ID");
            Field<NonNullGraphType<StringGraphType>>("destinationListId").Description("Destination List ID");
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line item ID to move");
        }
    }
}
