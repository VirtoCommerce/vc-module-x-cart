using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartQuantityType : InputCartBaseType
    {
        public InputUpdateCartQuantityType()
        {
            Name = "InputUpdateCartQuantity";

            Field<ListGraphType<InputUpdateCartQuantityItemType>>("items");
        }
    }
}
