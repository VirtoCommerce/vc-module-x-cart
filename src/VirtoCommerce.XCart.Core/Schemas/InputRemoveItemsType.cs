using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputRemoveItemsType : InputCartBaseType
    {
        public InputRemoveItemsType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("lineItemIds")
                .Description("Array of line item Id");
        }
    }
}
