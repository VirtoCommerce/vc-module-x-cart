using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputCartItemQuantityType : InputObjectGraphType<CartItemQuantity>
    {
        public InputCartItemQuantityType()
        {
            Field(x => x.LineItemId, nullable: false).Description("Line item Id");
            Field(x => x.Quantity, nullable: false).Description("New quantity");
        }
    }
}
