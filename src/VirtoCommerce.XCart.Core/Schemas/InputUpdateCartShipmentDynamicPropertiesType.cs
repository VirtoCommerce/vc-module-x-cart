using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartShipmentDynamicPropertiesType : InputCartBaseType
    {
        public InputUpdateCartShipmentDynamicPropertiesType()
        {
            Field<NonNullGraphType<StringGraphType>>("shipmentId",
                "Shipment Id");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties",
                "Dynamic properties");
        }
    }
}
