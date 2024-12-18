using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddWishlistItemType : ExtendableInputGraphType
    {
        public InputAddWishlistItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("listId", description: "Wish list id");
            Field<NonNullGraphType<StringGraphType>>("productId", description: "Product id to add");
            Field<IntGraphType>("quantity", description: "Product quantity to add");
        }
    }
}
