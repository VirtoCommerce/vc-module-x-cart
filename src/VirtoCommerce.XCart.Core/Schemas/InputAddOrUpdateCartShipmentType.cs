using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Helpers;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddOrUpdateCartShipmentType : InputCartBaseType
    {
        public InputAddOrUpdateCartShipmentType()
        {
            Field("shipment", GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputShipmentType>>()).Description("Shipment");
        }
    }
}
