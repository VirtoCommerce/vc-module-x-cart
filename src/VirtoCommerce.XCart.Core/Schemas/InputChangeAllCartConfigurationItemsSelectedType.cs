using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeAllCartConfigurationItemsSelectedType : InputCartBaseType
    {
        public InputChangeAllCartConfigurationItemsSelectedType()
        {
            Field<NonNullGraphType<StringGraphType>>("lineItemId").Description("Line item Id");
        }
    }
}
