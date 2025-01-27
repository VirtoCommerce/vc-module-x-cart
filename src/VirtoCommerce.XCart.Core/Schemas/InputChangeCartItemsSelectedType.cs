using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputChangeCartItemsSelectedType : InputCartBaseType
    {
        public InputChangeCartItemsSelectedType()
        {
            Field<ListGraphType<StringGraphType>>("lineItemIds").Description("List of line item Ids");
        }
    }
}
