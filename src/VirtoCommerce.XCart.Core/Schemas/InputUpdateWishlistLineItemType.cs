using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateWishlistLineItemType : InputObjectGraphType
    {
        public InputUpdateWishlistLineItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId", description: "Line Item Id to update");
            Field<NonNullGraphType<IntGraphType>>("quantity", description: "Product quantity to add");
        }
    }
}
