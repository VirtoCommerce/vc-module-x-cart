using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddItemsType : InputCartBaseType
    {
        public InputAddItemsType()
        {
            Field<NonNullGraphType<ListGraphType<InputNewCartItemType>>>("CartItems",
                "Cart items");
        }
    }
}
