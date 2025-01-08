using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartShipmentDynamicPropertiesType : InputCartBaseType
    {
        public InputUpdateCartShipmentDynamicPropertiesType()
        {
            Field<NonNullGraphType<StringGraphType>>("shipmentId")
                .Description("Shipment Id");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties")
                .Description("Dynamic properties");
        }
    }
}
