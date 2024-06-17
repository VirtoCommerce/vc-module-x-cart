using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputUpdateCartDynamicPropertiesType : InputCartBaseType
    {
        public InputUpdateCartDynamicPropertiesType()
        {
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>("dynamicProperties",
                "Dynamic properties");
        }
    }
}
