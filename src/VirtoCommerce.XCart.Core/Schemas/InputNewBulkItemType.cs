using GraphQL.Types;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.XCart.Core.Schemas
{
    public class InputNewBulkItemType : InputObjectGraphType<NewBulkCartItem>
    {
        public InputNewBulkItemType()
        {
            Field(x => x.ProductSku, nullable: false).Description("Product SKU");
            Field(x => x.Quantity, nullable: true).Description("Product quantity");
        }
    }
}
