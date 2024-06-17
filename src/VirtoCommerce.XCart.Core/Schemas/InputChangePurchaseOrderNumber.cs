using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangePurchaseOrderNumber : InputCartBaseType
    {
        public InputChangePurchaseOrderNumber()
        {
            Field<StringGraphType>("purchaseOrderNumber", "Purchase Order Number");
        }
    }
}
