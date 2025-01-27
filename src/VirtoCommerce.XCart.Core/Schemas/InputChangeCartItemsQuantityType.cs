using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemsQuantityType : InputCartBaseType
    {
        public InputChangeCartItemsQuantityType()
        {
            Field<NonNullGraphType<ListGraphType<InputCartItemQuantityType>>>("cartItems").Description("Cart items");
        }
    }
}
