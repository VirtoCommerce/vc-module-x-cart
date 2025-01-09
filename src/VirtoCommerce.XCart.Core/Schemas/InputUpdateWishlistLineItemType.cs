using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateWishlistLineItemType : InputObjectGraphType
    {
        public InputUpdateWishlistLineItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line Item Id to update");
            Field<NonNullGraphType<IntGraphType>>("quantity").Description("Product quantity to add");
        }
    }
}
