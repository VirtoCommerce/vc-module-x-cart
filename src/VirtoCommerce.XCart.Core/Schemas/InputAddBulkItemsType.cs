using GraphQL.Types;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputAddBulkItemsType : InputCartBaseType
    {
        public InputAddBulkItemsType()
        {
            Field<NonNullGraphType<ListGraphType<InputNewBulkItemType>>>("CartItems", "Bulk cart items");
        }
    }
}
