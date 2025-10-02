using GraphQL.Types;
using VirtoCommerce.XCart.Core.Commands;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartQuantityItemType : InputObjectGraphType<UpdateCartQuantityItem>
    {
        public InputUpdateCartQuantityItemType()
        {
            Name = "InputUpdateCartQuantityItem";

            Field<NonNullGraphType<StringGraphType>>("productId").Description("Product ID");
            Field<NonNullGraphType<IntGraphType>>("quantity").Description("Quantity");
        }
    }
}
