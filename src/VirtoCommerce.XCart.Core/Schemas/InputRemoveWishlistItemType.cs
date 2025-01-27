using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveWishlistItemType : InputObjectGraphType
    {
        public InputRemoveWishlistItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("List ID");
            Field<StringGraphType>("lineItemId").Description("Line item ID to remove");
            Field<StringGraphType>("productId").Description("Line item product ID to remove");
        }
    }
}
