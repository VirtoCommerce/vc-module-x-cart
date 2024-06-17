using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddOrUpdateCartShipmentType : InputCartBaseType
    {
        public InputAddOrUpdateCartShipmentType()
        {
            Field<NonNullGraphType<InputShipmentType>>("shipment",
                "Shipment");
        }
    }
}
