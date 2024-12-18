using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputMoveWishlistItemType : ExtendableInputGraphType
    {
        public InputMoveWishlistItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "Source List ID");
            Field<NonNullGraphType<StringGraphType>>("destinationListId", description: "Destination List ID");
            Field<NonNullGraphType<StringGraphType>>("lineItemId", "Line item ID to move");
        }
    }
}
