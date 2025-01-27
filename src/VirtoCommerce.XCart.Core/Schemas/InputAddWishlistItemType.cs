using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddWishlistItemType : InputObjectGraphType
    {
        public InputAddWishlistItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId").Description("Wish list id");
            Field<NonNullGraphType<StringGraphType>>("productId").Description("Product id to add");
            Field<IntGraphType>("quantity").Description("Product quantity to add");
        }
    }
}
