using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddWishlistBulkItemType : ExtendableInputGraphType
    {
        public InputAddWishlistBulkItemType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("listIds", description: "Wish list ids");
            Field<NonNullGraphType<StringGraphType>>("productId", description: "Product id to add");
            Field<IntGraphType>("quantity", description: "Product quantity to add");
        }
    }
}
