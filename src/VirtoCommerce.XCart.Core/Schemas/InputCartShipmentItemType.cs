using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputCartShipmentItemType : ExtendableInputGraphType
    {
        public InputCartShipmentItemType()
        {
            Field<NonNullGraphType<IntGraphType>>("quantity",
                "Quantity");
            Field<NonNullGraphType<StringGraphType>>("lineItemId",
                "Line item ID");
        }
    }
}
