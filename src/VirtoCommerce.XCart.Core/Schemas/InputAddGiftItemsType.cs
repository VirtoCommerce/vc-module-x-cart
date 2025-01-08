using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddGiftItemsType : InputCartBaseType
    {
        public InputAddGiftItemsType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("Ids")
                .Description("IDs of gift rewards to add to the cart");
        }
    }
}
