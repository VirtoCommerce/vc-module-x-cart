using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputCartShipmentItemType : InputObjectGraphType
    {
        public InputCartShipmentItemType()
        {
            Field<NonNullGraphType<IntGraphType>>("quantity")
                .Description("Quantity");
            Field<NonNullGraphType<StringGraphType>>("lineItemId")
                .Description("Line item ID");
        }
    }
}
