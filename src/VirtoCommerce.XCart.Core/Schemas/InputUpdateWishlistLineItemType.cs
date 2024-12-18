using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateWishlistLineItemType : ExtendableInputGraphType
    {
        public InputUpdateWishlistLineItemType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId", description: "Line Item Id to update");
            Field<NonNullGraphType<IntGraphType>>("quantity", description: "Product quantity to add");
        }
    }
}
