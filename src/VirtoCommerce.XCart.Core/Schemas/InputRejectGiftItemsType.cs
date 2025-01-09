using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRejectGiftItemsType : InputCartBaseType
    {
        public InputRejectGiftItemsType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("Ids")
                .Description("Ids of gift lineItems to reject from cart");
        }
    }
}
