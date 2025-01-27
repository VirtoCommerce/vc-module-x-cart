using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddWishlistBulkItemType : InputObjectGraphType
    {
        public InputAddWishlistBulkItemType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("listIds").Description("Wish list ids");
            Field<NonNullGraphType<StringGraphType>>("productId").Description("Product id to add");
            Field<IntGraphType>("quantity").Description("Product quantity to add");
        }
    }
}
