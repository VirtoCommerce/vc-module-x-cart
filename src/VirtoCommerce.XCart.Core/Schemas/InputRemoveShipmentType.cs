using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveShipmentType : InputCartBaseType
    {
        public InputRemoveShipmentType()
        {
            Field<StringGraphType>("shipmentId",
                "Shipment Id");
        }
    }
}
